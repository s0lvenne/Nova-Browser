#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace NovaBrowser;

public partial class App : Application
{
    public static CoreWebView2Environment? PreWarmedEnv { get; private set; }
    public static Task<CoreWebView2Environment?>? EnvTask { get; private set; }

    private async void App_Startup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += (s, args) =>
        {
            args.Handled = true;
        };
        if (!IsWebView2Installed())
        {
            InstallWebView2Async();
            return;
        }
        await LaunchAsync();
    }

    static async Task LaunchAsync()
    {
        var splash = new SplashWindow();
        splash.Show();
        try
        {
            splash.SetStatus("Инициализация стартовой страницы...");
            global::NovaBrowser.MainWindow.ExtractNovaHome();

            splash.SetStatus("Подготовка движка браузера...");
            EnvTask = CreateEnvAsync();
            var env = await EnvTask;
            PreWarmedEnv = env;

            splash.SetStatus("Загрузка интерфейса браузера...");
            var ready = new TaskCompletionSource();
            global::NovaBrowser.MainWindow.HtmlHomeReadyOnce = () => ready.TrySetResult();

            var main = new global::NovaBrowser.MainWindow();
            main.Opacity = 0;
            main.Show();

            splash.SetStatus("Подготовка стартовой страницы...");
            await Task.WhenAny(ready.Task, Task.Delay(4500));

            main.RevealWindow();
        }
        catch
        {
            try { new global::NovaBrowser.MainWindow().Show(); } catch { Current.Shutdown(); }
        }
        finally
        {
            try { splash.Close(); } catch { }
        }
    }

    public static string BuildWebView2Arguments(global::NovaBrowser.Models.BrowserSettings settings)
    {
        var gpuFlags = (settings?.GpuAcceleration ?? true)
            ? "--enable-gpu-rasterization --enable-zero-copy --enable-oop-rasterization "
            : "--disable-gpu ";

        global::NovaBrowser.Helpers.NovaProxyBridge.EnsureStarted();
        var bridgePort = global::NovaBrowser.Helpers.NovaProxyBridge.Port;

        if (settings != null && settings.ProxyEnabled && !string.IsNullOrWhiteSpace(settings.ProxyHost))
        {
            global::NovaBrowser.Helpers.NovaProxyBridge.SetUpstream(
                settings.ProxyType,
                settings.ProxyHost,
                settings.ProxyPort > 0 ? settings.ProxyPort : 8080,
                settings.ProxyUsername,
                settings.ProxyPassword);
        }
        else
        {
            global::NovaBrowser.Helpers.NovaProxyBridge.SetUpstream(null, null, 0);
        }

        var proxyFlags = $"--proxy-server=http://127.0.0.1:{bridgePort} " +
                         "--proxy-bypass-list=<-loopback> " +
                         "--force-webrtc-ip-handling-policy --webrtc-ip-handling-policy=disable_non_proxied_udp ";

        return gpuFlags +
            proxyFlags +
            "--autoplay-policy=no-user-gesture-required " +
            "--enable-features=HighEfficiencyModeAvailable,MemorySaverModeAvailable,HeuristicMemorySaver " +
            "--renderer-process-limit=4 " +
            "--process-per-site " +
            "--js-flags=\"--max_old_space_size=256\" " +
            "--disable-features=Translate,MediaRouter,CalculateNativeWinOcclusion,InterestFeedContentSuggestions " +
            "--disable-breakpad --disable-domain-reliability --disable-hang-monitor --disable-speech-api " +
            "--no-first-run --no-default-browser-check --disable-sync";
    }

    static async Task<CoreWebView2Environment?> CreateEnvAsync()
    {
        try
        {
            var dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovaBrowser", "UserData");

            var settings = global::NovaBrowser.Models.BrowserSettings.Load();
            var optString = BuildWebView2Arguments(settings);

            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", optString);

            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: dataFolder,
                options: new CoreWebView2EnvironmentOptions(optString));
            PreWarmedEnv = env;
            return env;
        }
        catch { return null; }
    }

    static bool IsWebView2Installed()
    {
        string[] keys =
        {
            @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
            @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
        };
        foreach (var key in keys)
        {
            try
            {
                using var reg = Registry.LocalMachine.OpenSubKey(key);
                var pv = reg?.GetValue("pv") as string;
                if (!string.IsNullOrEmpty(pv) && pv != "0.0.0.0") return true;
            }
            catch { }
        }
        try
        {
            var perUser = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "EdgeWebView", "Application");
            if (Directory.Exists(perUser) && Directory.GetDirectories(perUser).Length > 0)
                return true;
        }
        catch { }
        return false;
    }

    static void InstallWebView2Async()
    {
        var result = MessageBox.Show(
            "Nova Browser требует Microsoft WebView2 Runtime.\n\n" +
            "Компонент будет загружен и установлен автоматически (~2 МБ).\n" +
            "Продолжить?",
            "Nova Browser", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (result != MessageBoxResult.Yes) { Current.Shutdown(); return; }
        var splash = new SplashWindow();
        splash.Show();
        Task.Run(async () =>
        {
            try
            {
                var url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
                var tempPath = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebview2Setup.exe");
                splash.SetStatus("Загрузка WebView2...");
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                await File.WriteAllBytesAsync(tempPath, await client.GetByteArrayAsync(url));
                splash.SetStatus("Установка...");
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = tempPath,
                        Arguments = "/silent /install",
                        UseShellExecute = true,
                        Verb = "runas"
                    }
                };
                proc.Start();
                proc.WaitForExit();
                try { File.Delete(tempPath); } catch { }
                await Current.Dispatcher.InvokeAsync(async () =>
                {
                    try { splash.Close(); } catch { }
                    if (!IsWebView2Installed())
                    {
                        MessageBox.Show(
                            "Не удалось установить WebView2.\n\n" +
                            "Скачайте вручную: https://developer.microsoft.com/microsoft-edge/webview2/",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Current.Shutdown();
                    }
                    else await LaunchAsync();
                });
            }
            catch (Exception ex)
            {
                Current.Dispatcher.Invoke(() =>
                {
                    try { splash.Close(); } catch { }
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                });
            }
        });
    }
}
