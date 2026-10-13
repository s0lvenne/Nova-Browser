#nullable enable
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NovaBrowser.Helpers;

public static class NovaProxyBridge
{
    private static TcpListener? _listener;
    private static bool _running;
    private static readonly object _sync = new();

    private static volatile string? _upProto;
    private static volatile string? _upHost;
    private static volatile int _upPort;
    private static volatile string? _upUser;
    private static volatile string? _upPass;

    public static int Port { get; private set; } = 28990;

    public static void SetUpstream(string? proto, string? host, int port, string? user = null, string? pass = null)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || proto?.Equals("direct", StringComparison.OrdinalIgnoreCase) == true)
            {
                _upProto = null;
                _upHost = null;
                _upPort = 0;
                _upUser = null;
                _upPass = null;
            }
            else
            {
                _upProto = proto?.Trim().ToLowerInvariant() ?? "http";
                _upHost = host.Trim();
                _upPort = port;
                _upUser = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
                _upPass = string.IsNullOrWhiteSpace(pass) ? null : pass.Trim();
            }
        }
    }

    public static void EnsureStarted()
    {
        if (_running) return;
        lock (_sync)
        {
            if (_running) return;
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, Port);
                _listener.Start();
                _running = true;
            }
            catch
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Loopback, 0);
                    _listener.Start();
                    Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                    _running = true;
                }
                catch
                {
                    return;
                }
            }

            Task.Run(ListenLoopAsync);
        }
    }

    private static async Task ListenLoopAsync()
    {
        while (_running && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                client.NoDelay = true;
                _ = Task.Run(() => HandleClientAsync(client));
            }
            catch
            {
                if (!_running) break;
                await Task.Delay(50);
            }
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var clientStream = client.GetStream();
                using var reader = new BufferedStream(clientStream, 16384);

                var reqLine = await ReadLineAsync(reader);
                if (string.IsNullOrWhiteSpace(reqLine)) return;

                var parts = reqLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return;

                var method = parts[0].ToUpperInvariant();
                var target = parts[1];

                var headers = new StringBuilder();
                string? hostHeader = null;
                while (true)
                {
                    var h = await ReadLineAsync(reader);
                    if (string.IsNullOrEmpty(h)) break;
                    if (h.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                    {
                        hostHeader = h.Substring(5).Trim();
                    }
                    if (!h.StartsWith("Proxy-Connection:", StringComparison.OrdinalIgnoreCase))
                    {
                        headers.Append(h).Append("\r\n");
                    }
                }

                string destHost;
                int destPort;

                if (method == "CONNECT")
                {
                    var colon = target.IndexOf(':');
                    if (colon > 0)
                    {
                        destHost = target.Substring(0, colon);
                        int.TryParse(target.Substring(colon + 1), out destPort);
                        if (destPort <= 0) destPort = 443;
                    }
                    else
                    {
                        destHost = target;
                        destPort = 443;
                    }

                    var upHost = _upHost;
                    var upPort = _upPort;
                    var upProto = _upProto;
                    var upUser = _upUser;
                    var upPass = _upPass;

                    TcpClient? server = null;
                    Stream? serverStream = null;

                    try
                    {
                        if (string.IsNullOrEmpty(upHost) || upPort <= 0)
                        {
                            server = new TcpClient();
                            server.NoDelay = true;
                            await server.ConnectAsync(destHost, destPort);
                            serverStream = server.GetStream();
                        }
                        else if (upProto == "socks5")
                        {
                            server = new TcpClient();
                            server.NoDelay = true;
                            await server.ConnectAsync(upHost, upPort);
                            serverStream = server.GetStream();
                            var ok = await DoSocks5HandshakeAsync(serverStream, destHost, destPort, upUser, upPass);
                            if (!ok)
                            {
                                server.Dispose();
                                await Send502Async(clientStream);
                                return;
                            }
                        }
                        else
                        {
                            server = new TcpClient();
                            server.NoDelay = true;
                            await server.ConnectAsync(upHost, upPort);
                            serverStream = server.GetStream();

                            var connectReq = new StringBuilder();
                            connectReq.Append($"CONNECT {destHost}:{destPort} HTTP/1.1\r\nHost: {destHost}:{destPort}\r\n");
                            if (!string.IsNullOrEmpty(upUser))
                            {
                                var cred = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{upUser}:{upPass ?? ""}"));
                                connectReq.Append($"Proxy-Authorization: Basic {cred}\r\n");
                            }
                            connectReq.Append("Proxy-Connection: keep-alive\r\n\r\n");
                            var connectBytes = Encoding.ASCII.GetBytes(connectReq.ToString());
                            await serverStream.WriteAsync(connectBytes, 0, connectBytes.Length);

                            var respLine = await ReadLineAsync(serverStream);
                            if (string.IsNullOrEmpty(respLine) || !respLine.Contains("200"))
                            {
                                server.Dispose();
                                await Send502Async(clientStream);
                                return;
                            }
                            while (true)
                            {
                                var h = await ReadLineAsync(serverStream);
                                if (string.IsNullOrEmpty(h)) break;
                            }
                        }

                        var okResp = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                        await clientStream.WriteAsync(okResp, 0, okResp.Length);

                        var t1 = clientStream.CopyToAsync(serverStream);
                        var t2 = serverStream.CopyToAsync(clientStream);
                        await Task.WhenAny(t1, t2);
                    }
                    catch
                    {
                        try { await Send502Async(clientStream); } catch { }
                    }
                    finally
                    {
                        try { serverStream?.Dispose(); } catch { }
                        try { server?.Dispose(); } catch { }
                    }
                }
                else
                {
                    Uri? uri = null;
                    if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        Uri.TryCreate(target, UriKind.Absolute, out uri);
                    }
                    else if (!string.IsNullOrEmpty(hostHeader))
                    {
                        Uri.TryCreate($"http://{hostHeader}{target}", UriKind.Absolute, out uri);
                    }

                    if (uri == null)
                    {
                        await Send502Async(clientStream);
                        return;
                    }

                    destHost = uri.Host;
                    destPort = uri.Port > 0 ? uri.Port : 80;

                    var upHost = _upHost;
                    var upPort = _upPort;
                    var upProto = _upProto;
                    var upUser = _upUser;
                    var upPass = _upPass;

                    TcpClient? server = null;
                    Stream? serverStream = null;

                    try
                    {
                        if (string.IsNullOrEmpty(upHost) || upPort <= 0)
                        {
                            server = new TcpClient();
                            server.NoDelay = true;
                            await server.ConnectAsync(destHost, destPort);
                            serverStream = server.GetStream();

                            var path = uri.PathAndQuery;
                            var reqText = $"{method} {path} HTTP/1.1\r\nHost: {destHost}\r\nConnection: close\r\n{headers}\r\n";
                            var reqBytes = Encoding.UTF8.GetBytes(reqText);
                            await serverStream.WriteAsync(reqBytes, 0, reqBytes.Length);
                        }
                        else if (upProto == "socks5")
                        {
                            server = new TcpClient();
                            server.NoDelay = true;
                            await server.ConnectAsync(upHost, upPort);
                            serverStream = server.GetStream();

                            var ok = await DoSocks5HandshakeAsync(serverStream, destHost, destPort, upUser, upPass);
                            if (!ok)
                            {
                                server.Dispose();
                                await Send502Async(clientStream);
                                return;
                            }

                            var path = uri.PathAndQuery;
                            var reqText = $"{method} {path} HTTP/1.1\r\nHost: {destHost}\r\nConnection: close\r\n{headers}\r\n";
                            var reqBytes = Encoding.UTF8.GetBytes(reqText);
                            await serverStream.WriteAsync(reqBytes, 0, reqBytes.Length);
                        }
                        else
                        {
                            server = new TcpClient();
                            server.NoDelay = true;
                            await server.ConnectAsync(upHost, upPort);
                            serverStream = server.GetStream();

                            var reqText = new StringBuilder();
                            reqText.Append($"{method} {uri.AbsoluteUri} HTTP/1.1\r\nHost: {destHost}\r\n");
                            if (!string.IsNullOrEmpty(upUser))
                            {
                                var cred = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{upUser}:{upPass ?? ""}"));
                                reqText.Append($"Proxy-Authorization: Basic {cred}\r\n");
                            }
                            reqText.Append($"Connection: close\r\n{headers}\r\n");
                            var reqBytes = Encoding.UTF8.GetBytes(reqText.ToString());
                            await serverStream.WriteAsync(reqBytes, 0, reqBytes.Length);
                        }

                        await serverStream.CopyToAsync(clientStream);
                    }
                    catch
                    {
                        try { await Send502Async(clientStream); } catch { }
                    }
                    finally
                    {
                        try { serverStream?.Dispose(); } catch { }
                        try { server?.Dispose(); } catch { }
                    }
                }
            }
            catch { }
        }
    }

    private static async Task<bool> DoSocks5HandshakeAsync(Stream stream, string destHost, int destPort, string? user, string? pass)
    {
        try
        {
            if (string.IsNullOrEmpty(user))
            {
                await stream.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, 0, 3);
                var buf = new byte[2];
                var read = await ReadExactAsync(stream, buf, 0, 2);
                if (read < 2 || buf[0] != 0x05 || buf[1] != 0x00) return false;
            }
            else
            {
                await stream.WriteAsync(new byte[] { 0x05, 0x01, 0x02 }, 0, 3);
                var buf = new byte[2];
                var read = await ReadExactAsync(stream, buf, 0, 2);
                if (read < 2 || buf[0] != 0x05 || buf[1] != 0x02) return false;

                var uBytes = Encoding.UTF8.GetBytes(user);
                var pBytes = Encoding.UTF8.GetBytes(pass ?? "");
                var authReq = new byte[3 + uBytes.Length + pBytes.Length];
                authReq[0] = 0x01;
                authReq[1] = (byte)uBytes.Length;
                Buffer.BlockCopy(uBytes, 0, authReq, 2, uBytes.Length);
                authReq[2 + uBytes.Length] = (byte)pBytes.Length;
                Buffer.BlockCopy(pBytes, 0, authReq, 3 + uBytes.Length, pBytes.Length);
                await stream.WriteAsync(authReq, 0, authReq.Length);

                var authResp = new byte[2];
                var aRead = await ReadExactAsync(stream, authResp, 0, 2);
                if (aRead < 2 || authResp[1] != 0x00) return false;
            }

            var dBytes = Encoding.ASCII.GetBytes(destHost);
            var req = new byte[7 + dBytes.Length];
            req[0] = 0x05;
            req[1] = 0x01;
            req[2] = 0x00;
            req[3] = 0x03;
            req[4] = (byte)dBytes.Length;
            Buffer.BlockCopy(dBytes, 0, req, 5, dBytes.Length);
            req[5 + dBytes.Length] = (byte)((destPort >> 8) & 0xFF);
            req[6 + dBytes.Length] = (byte)(destPort & 0xFF);

            await stream.WriteAsync(req, 0, req.Length);

            var respHeader = new byte[4];
            var hRead = await ReadExactAsync(stream, respHeader, 0, 4);
            if (hRead < 4 || respHeader[0] != 0x05 || respHeader[1] != 0x00) return false;

            var atyp = respHeader[3];
            int toSkip = atyp switch
            {
                0x01 => 4 + 2,
                0x04 => 16 + 2,
                0x03 => (await ReadByteAsync(stream)) + 2,
                _ => 6
            };

            var skipBuf = new byte[toSkip];
            await ReadExactAsync(stream, skipBuf, 0, toSkip);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<int> ReadByteAsync(Stream s)
    {
        var b = new byte[1];
        var read = await s.ReadAsync(b, 0, 1);
        return read == 1 ? b[0] : 0;
    }

    private static async Task<int> ReadExactAsync(Stream s, byte[] buffer, int offset, int count)
    {
        int total = 0;
        while (total < count)
        {
            var r = await s.ReadAsync(buffer, offset + total, count - total);
            if (r <= 0) break;
            total += r;
        }
        return total;
    }

    private static async Task<string> ReadLineAsync(Stream s)
    {
        var sb = new StringBuilder();
        var buf = new byte[1];
        while (true)
        {
            var r = await s.ReadAsync(buf, 0, 1);
            if (r <= 0) break;
            var ch = (char)buf[0];
            if (ch == '\r') continue;
            if (ch == '\n') break;
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static async Task Send502Async(Stream s)
    {
        var err = Encoding.ASCII.GetBytes("HTTP/1.1 502 Bad Gateway\r\nContent-Type: text/plain\r\nConnection: close\r\n\r\n502 Bad Gateway - Proxy unreachable");
        await s.WriteAsync(err, 0, err.Length);
    }
}
