#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace NovaBrowser.Helpers;

public static class SecurityHelper
{
    private static readonly HashSet<string> Whitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com", "youtu.be", "m.youtube.com", "youtube-nocookie.com",
        "googlevideo.com", "ytimg.com", "yt3.ggpht.com", "yt3.googleusercontent.com",
        "youtube.googleapis.com", "youtubei.googleapis.com",
        "google.com", "www.google.com", "googleapis.com", "gstatic.com",
        "googleusercontent.com", "accounts.google.com",
        "safebrowsing.googleapis.com", "update.googleapis.com",
        "akamaized.net", "akamai.net", "akamaihd.net", "akadns.net",
        "cloudfront.net", "fastly.net", "fastlylb.net", "cloudflare.com",
        "netflix.com", "nflxvideo.net", "nflxso.net",
        "twitch.tv", "twitchsvc.net", "jtvnw.net",
        "vimeo.com", "vimeocdn.com",
        "spotify.com", "open.spotify.com", "scdn.co", "spotifycdn.com", "spotifycdn.net", "pscdn.co", "spclient.wg.spotify.com"
    };

    public static readonly HashSet<string> BlockedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "doubleclick.net", "googleadservices.com", "adservice.google.com", "adservice.google.ru",
        "pagead2.googlesyndication.com", "googlesyndication.com", "googletagservices.com",
        "googleads.g.doubleclick.net", "pubads.g.doubleclick.net", "securepubads.g.doubleclick.net",
        "pagead.googlesyndication.com", "ade.googlesyndication.com", "ad.doubleclick.net",
        "cm.g.doubleclick.net", "stats.g.doubleclick.net", "fls.doubleclick.net", "ad.mo.doubleclick.net",
        "mediavisor.doubleclick.net",
        "an.yandex.ru", "yabs.yandex.ru", "adfox.ru", "adfox.net", "direct.yandex.ru",
        "tgb.yandex.ru", "rtb.yandex.ru", "bs.yandex.ru", "awaps.yandex.net", "mc.yandex.ru",
        "metrika.yandex.ru", "webvisor.org", "ad.mail.ru", "target.my.com", "top-fwz1.mail.ru",
        "adriver.ru", "begun.ru", "smi2.ru", "24smi.net", "relap.io", "click.ru",
        "lentainform.com", "marketgid.com", "mediametrics.ru", "mgid.com", "teasernet.com",
        "popads.net", "adsterra.com", "propellerads.com", "pushwoosh.com", "onesignal.com",
        "amazon-adsystem.com", "adsystem.amazon.com", "assoc-amazon.com",
        "ads.twitter.com", "ads.x.com", "syndication.twitter.com", "ads-twitter.com", "t.co",
        "ads.linkedin.com", "px.ads.linkedin.com", "ads.microsoft.com", "ads.yahoo.com",
        "advertising.com", "guce.advertising.com", "sp.analytics.yahoo.com",
        "ads.tiktok.com", "tracking.tiktok.com", "analytics.tiktok.com", "log.byteoversea.com", "log.tiktokv.com",
        "criteo.com", "criteo.net", "static.criteo.net", "events.criteo.com",
        "rubiconproject.com", "pubmatic.com", "ads.pubmatic.com", "image2.pubmatic.com", "simage2.pubmatic.com",
        "openx.net", "appnexus.com", "adnxs.com", "secure.adnxs.com", "ib.adnxs.com", "cdn.adnxs.com",
        "yieldmanager.com", "adsrvr.org", "adform.net", "smartadserver.com",
        "outbrain.com", "taboola.com", "revcontent.com", "moatads.com", "casalemedia.com",
        "indexexchange.com", "sharethrough.com", "triplelift.com", "sonobi.com", "contextweb.com",
        "lijit.com", "adsafeprotected.com", "ssp.adsafeprotected.com", "theadex.com", "bidswitch.net",
        "bidswitch.com", "33across.com", "360yield.com", "media.net", "a-mo.net", "adagio.io",
        "adalliance.io", "adbrn.com", "adcolony.com", "addthis.com", "addthisedge.com", "adgravity.com",
        "adgrx.com", "adhigh.net", "adkernel.com", "adloox.com", "admanmedia.com", "admantx.com",
        "admedo.com", "admixer.net", "adnow.com", "adocean.pl", "adpone.com", "adreadytrk.com",
        "adrta.com", "adsymptotic.com", "adtelligent.com", "adtiger.de", "adture.com", "adunity.com",
        "adyoulike.com", "aerserv.com", "agkn.com", "amgdgt.com", "adtech.de", "adserver.adtech.de",
        "trafficjunky.net", "trafficjunky.com", "ads.reddit.com", "reddit.tt",
        "hotjar.com", "mouseflow.com", "crazyegg.com", "mixpanel.com", "cdn.mxpnl.com", "api.mixpanel.com",
        "heap.io", "cdn.heapanalytics.com", "heapanalytics.com", "fullstory.com", "loggly.com",
        "newrelic.com", "nr-data.net", "scorecardresearch.com", "quantserve.com", "chartbeat.com",
        "comscore.com", "sentry.io", "bugsnag.com", "rollbar.com", "datadoghq.com", "pingdom.net",
        "statcounter.com", "woopra.com", "inspectlet.com", "kissmetrics.com", "clicky.com",
        "luckyorange.com", "smartlook.com", "pendo.io", "launchdarkly.com", "appsflyer.com",
        "kochava.com", "branch.io", "app-measurement.com", "crashlytics.com", "fabric.io",
        "flurry.com", "segment.io", "segment.com", "api.segment.io", "cdn.segment.com", "api.segment.com",
        "api.amplitude.com", "cdn.amplitude.com", "assets.adobedtm.com", "dpm.demdex.net", "demdex.net",
        "omtrdc.net", "2o7.net", "everesttech.net",
        "connect.facebook.net", "pixel.facebook.com", "an.facebook.com", "analytics.facebook.com",
        "staticxx.facebook.com", "bat.bing.com", "clarity.ms", "c.clarity.ms", "snap.licdn.com",
        "trk.pinterest.com", "log.pinterest.com", "analytics.pinterest.com", "analytics.snap.com",
        "tr.snapchat.com", "p.typekit.net", "use.typekit.net",
        "cdn.cookielaw.org", "geolocation.onetrust.com", "cdn.consent.cookiebot.com", "consentcdn.cookiebot.com",
        "collect.iubenda.com", "cs.iubenda.com", "cdn.privacy-center.org", "privacy-center.org",
        "api.usercentrics.eu", "consents.usercentrics.eu",
        "imasdk.googleapis.com", "video-ad-stats.googlevideo.com",
        "coinhive.com", "coin-hive.com", "jsecoin.com", "crypto-loot.com", "authedmine.com",
        "minero.pw", "webmr.eu", "monerominer.rocks", "coinhave.com", "papoto.com", "tokensmod.com",
        "webmine.cz", "cryptoloot.pro", "minero.cc", "mlnr.pw", "reasedoper.pw", "wasmio.eu",
        "api.ad-maven.com", "anti-ad.net", "telemetry.mozilla.org", "incoming.telemetry.mozilla.org",
        "ssl.google-analytics.com"
    };

    private static readonly string[] DangerousPatterns =
    {
        "javascript:", "data:text/html", "vbscript:", "<script",
        "data:application/", "%00",
        "data:text/javascript", "&#106;avascript"
    };

    public static readonly Dictionary<string, string> SearchEngines =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["Nova"] = "https://nova.example/index.html#/search?q={0}",
        ["Nova Mix"] = "https://nova.example/index.html#/search?q={0}",
        ["Nova Search"] = "https://nova.example/index.html#/search?q={0}",
        ["Google"] = "https://www.google.com/search?q={0}",
        ["Yandex"] = "https://yandex.ru/search/?text={0}",
        ["Яндекс"] = "https://yandex.ru/search/?text={0}",
        ["DuckDuckGo"] = "https://duckduckgo.com/?q={0}",
        ["Bing"] = "https://www.bing.com/search?q={0}",
        ["Brave Search"] = "https://search.brave.com/search?q={0}",
        ["Yahoo"] = "https://search.yahoo.com/search?p={0}",
        ["Baidu"] = "https://www.baidu.com/s?wd={0}",
        ["Mail.ru"] = "https://go.mail.ru/search?q={0}",
        ["Ecosia"] = "https://www.ecosia.org/search?q={0}",
        ["Startpage"] = "https://www.startpage.com/search?q={0}"
    };

    private static bool IsWhitelistedInternal(string host)
    {
        if (Whitelist.Contains(host)) return true;
        foreach (var w in Whitelist)
        {
            if (host.EndsWith("." + w, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static bool IsWhitelisted(string host) => IsWhitelistedInternal(host);

    public static bool IsBlockedHost(string host)
    {
        if (IsWhitelistedInternal(host)) return false;
        if (BlockedDomains.Contains(host)) return true;
        var dot = host.IndexOf('.');
        while (dot >= 0)
        {
            var suffix = host[(dot + 1)..];
            if (BlockedDomains.Contains(suffix)) return true;
            dot = suffix.IndexOf('.');
            if (dot < 0) break;
            host = suffix;
        }
        return false;
    }

    public static bool IsBlockedUrl(string url, string host)
    {
        if (IsWhitelistedInternal(host)) return false;
        if (IsBlockedHost(host)) return true;
        if (string.IsNullOrWhiteSpace(url)) return false;
        var lower = url.ToLowerInvariant();
        if (lower.Contains("/pagead/") || lower.Contains("/googleads/") ||
            lower.Contains("googleadservices.") || lower.Contains("doubleclick.net") ||
            lower.Contains("an.yandex.ru") || lower.Contains("yabs.yandex.ru") ||
            lower.Contains("adfox.ru") || lower.Contains("adfox.net") ||
            lower.Contains("mc.yandex.ru/watch") || lower.Contains("metrika.yandex.ru") ||
            lower.Contains("/ads.js") || lower.Contains("/ad.js") ||
            lower.Contains("/prebid.js") || lower.Contains("criteo.") ||
            lower.Contains("taboola.") || lower.Contains("outbrain.") ||
            lower.Contains("mgid.com") || lower.Contains("adsystem.amazon") ||
            lower.Contains("clarity.ms") || lower.Contains("pixel.facebook.com") ||
            lower.Contains("connect.facebook.net/signals") ||
            lower.Contains("adriver.ru") || lower.Contains("begun.ru") ||
            lower.Contains("smi2.ru") || lower.Contains("24smi.net") ||
            lower.Contains("relap.io") || lower.Contains("marketgid.com") ||
            lower.Contains("teasernet.com") || lower.Contains("propellerads.com"))
        {
            return true;
        }
        return false;
    }

    public static bool IsExfiltrationUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var lower = url.ToLowerInvariant();
        if (lower.Contains("discord.com/api/webhooks/") || lower.Contains("discordapp.com/api/webhooks/")) return true;
        if (lower.Contains("api.telegram.org/bot")) return true;
        return false;
    }

    public static (bool IsSafe, string Reason) ValidateUrl(
        string url,
        IEnumerable<string>? customBlockedDomains = null,
        bool forceHttps = false,
        bool safeBrowsing = true)
    {
        if (string.IsNullOrWhiteSpace(url)) return (false, "Пустой URL");
        if (url.StartsWith("about:") || url.StartsWith("nova://"))
            return (true, "OK");
        var lower = url.ToLowerInvariant();
        foreach (var p in DangerousPatterns)
        {
            if (!string.IsNullOrEmpty(p) && lower.Contains(p))
                return (false, $"Опасный паттерн [{p}]");
        }
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (true, "OK");
        var host = uri.Host.ToLowerInvariant();
        if (IsWhitelistedInternal(host)) return (true, "OK");
        if (IsBlockedHost(host))
            return (false, $"Домен {host} заблокирован");
        if (customBlockedDomains != null)
        {
            foreach (var cd in customBlockedDomains)
            {
                var d = cd.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(d) && (host == d || host.EndsWith("." + d)))
                    return (false, $"Домен {host} в вашем списке");
            }
        }
        if (forceHttps && uri.Scheme == "http")
            return (false, $"HTTPS_UPGRADE:{url.Replace("http://", "https://")}");
        return (true, "OK");
    }

    public static string? TryUpgradeToHttps(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme == "http"
            ? "https://" + url["http://".Length..] : null;

    public static bool IsSecureConnection(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) &&
        (u.Scheme == "https" || u.Scheme == "chrome");

    public static string NormalizeInput(string input, string searchEngine = "Nova")
    {
        if (string.IsNullOrWhiteSpace(input)) return "about:blank";
        input = input.Trim();
        if (Uri.TryCreate(input, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == "http" || parsed.Scheme == "https" ||
             parsed.Scheme == "ftp" || parsed.Scheme == "file" ||
             parsed.Scheme == "nova"))
            return input;
        if (!input.Contains(' ') && input.Contains('.') &&
            !input.StartsWith("http") && input.Length > 3)
            return "https://" + input;
        if (string.Equals(searchEngine, "Nova", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(searchEngine, "Nova Mix", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(searchEngine, "Nova Search", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://nova.example/index.html#/search?q={Uri.EscapeDataString(input)}";
        }
        string template;
        if (SearchEngines.TryGetValue(searchEngine, out var byName))
        {
            template = byName;
        }
        else if (searchEngine.Contains("{0}"))
        {
            template = searchEngine;
        }
        else if (searchEngine.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return searchEngine + Uri.EscapeDataString(input);
        }
        else
        {
            template = SearchEngines["Google"];
        }
        return string.Format(template, Uri.EscapeDataString(input));
    }

    public static (bool IsMalicious, string ThreatName, string Details) ScanForStealers(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return (false, string.Empty, string.Empty);

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        try
        {
            if (ext == ".jar" || ext == ".zip")
            {
                using var fs = File.OpenRead(filePath);
                using var archive = new ZipArchive(fs, ZipArchiveMode.Read, false);

                var suspiciousEntries = new List<string>();
                bool foundWebhook = false;
                bool foundTelegramBot = false;
                bool foundMinecraftGrabber = false;
                bool foundBrowserStealer = false;

                var sensitiveIndicators = new[]
                {
                    "discord.com/api/webhooks",
                    "discordapp.com/api/webhooks",
                    "api.telegram.org/bot",
                    "launcher_accounts.json",
                    "launcher_profiles.json",
                    "user.auth.xboxlive.com",
                    "tdata",
                    "Local Storage/leveldb",
                    "Login Data",
                    "feather",
                    "essential",
                    "powershell -enc",
                    "certutil -urlcache"
                };

                var suspiciousClassNames = new[]
                {
                    "tokengrabber", "stealer", "grabber", "webhook",
                    "sessionstealer", "minecraftstealer", "lumma", "redline",
                    "raccoon", "vidar", "discordtoken", "passwordsave"
                };

                foreach (var entry in archive.Entries)
                {
                    var nameLower = entry.FullName.ToLowerInvariant();
                    foreach (var sClass in suspiciousClassNames)
                    {
                        if (nameLower.Contains(sClass))
                        {
                            suspiciousEntries.Add(entry.FullName);
                            break;
                        }
                    }

                    if (entry.Length > 0 && entry.Length < 2 * 1024 * 1024 &&
                        (nameLower.EndsWith(".class") || nameLower.EndsWith(".json") || nameLower.EndsWith(".txt") || nameLower.EndsWith(".cfg")))
                    {
                        try
                        {
                            using var entryStream = entry.Open();
                            using var ms = new MemoryStream();
                            entryStream.CopyTo(ms);
                            var bytes = ms.ToArray();
                            var textAscii = Encoding.ASCII.GetString(bytes).ToLowerInvariant();

                            if (textAscii.Contains("discord.com/api/webhooks") || textAscii.Contains("discordapp.com/api/webhooks"))
                            {
                                foundWebhook = true;
                                suspiciousEntries.Add(entry.FullName);
                            }
                            if (textAscii.Contains("api.telegram.org/bot"))
                            {
                                foundTelegramBot = true;
                                suspiciousEntries.Add(entry.FullName);
                            }
                            if (textAscii.Contains("launcher_accounts.json") || textAscii.Contains("user.auth.xboxlive.com"))
                            {
                                foundMinecraftGrabber = true;
                                suspiciousEntries.Add(entry.FullName);
                            }
                            if (textAscii.Contains("login data") || textAscii.Contains("local storage/leveldb") || textAscii.Contains("tdata"))
                            {
                                foundBrowserStealer = true;
                                suspiciousEntries.Add(entry.FullName);
                            }
                        }
                        catch { }
                    }
                }

                if (foundWebhook || foundTelegramBot || (foundMinecraftGrabber && foundBrowserStealer) || suspiciousEntries.Count >= 2)
                {
                    var threat = foundWebhook ? "Discord Webhook Stealer" :
                                 foundTelegramBot ? "Telegram C2 Stealer" :
                                 foundMinecraftGrabber ? "Minecraft Session Grabber" : "Троян-стиллер аккаунтов";
                    var det = $"Обнаружено подозрительных модулей: {suspiciousEntries.Count}. Файл содержит механизмы несанкционированного перехвата токенов и учетных записей.";
                    return (true, threat, det);
                }
            }
            else if (ext == ".exe" || ext == ".scr" || ext == ".bat" || ext == ".cmd" || ext == ".ps1" || ext == ".vbs")
            {
                using var fs = File.OpenRead(filePath);
                var buffer = new byte[Math.Min(fs.Length, 4 * 1024 * 1024)];
                var read = fs.Read(buffer, 0, buffer.Length);
                var text = Encoding.ASCII.GetString(buffer, 0, read).ToLowerInvariant();

                bool foundWebhook = text.Contains("discord.com/api/webhooks") || text.Contains("discordapp.com/api/webhooks");
                bool foundTelegram = text.Contains("api.telegram.org/bot");
                bool foundStealerArtifacts = (text.Contains("login data") || text.Contains("tdata")) && (text.Contains("powershell") || text.Contains("certutil"));

                if (foundWebhook || foundTelegram || foundStealerArtifacts)
                {
                    var threat = foundWebhook ? "Discord Webhook Grabber" : foundTelegram ? "Telegram Stealer Payload" : "Data Stealer Script / Executable";
                    var det = "Файл содержит вредоносные сигнатуры скрытой отправки данных авторизации на внешние серверы.";
                    return (true, threat, det);
                }
            }
        }
        catch { }

        return (false, string.Empty, string.Empty);
    }

    public static bool IsStealthDomain(string urlOrHost, string configuredDomains)
    {
        if (string.IsNullOrWhiteSpace(urlOrHost) || string.IsNullOrWhiteSpace(configuredDomains))
            return false;
        string host;
        try
        {
            if (urlOrHost.Contains("://"))
            {
                var s = urlOrHost.IndexOf("://", StringComparison.Ordinal) + 3;
                var e = urlOrHost.IndexOfAny(new[] { '/', ':', '?' }, s);
                host = e < 0 ? urlOrHost[s..] : urlOrHost[s..e];
            }
            else
            {
                var e = urlOrHost.IndexOfAny(new[] { '/', ':', '?' });
                host = e < 0 ? urlOrHost : urlOrHost[..e];
            }
            host = host.Trim().ToLowerInvariant();
        }
        catch { return false; }

        var list = configuredDomains.Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in list)
        {
            var d = raw.Trim().ToLowerInvariant().TrimStart('*').TrimStart('.');
            if (string.IsNullOrEmpty(d)) continue;
            if (host == d || host.EndsWith("." + d) || host.Contains(d))
                return true;
        }
        return false;
    }

    public static string SanitizeTrackingUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        try
        {
            var qIdx = url.IndexOf('?');
            if (qIdx < 0) return url;
            var basePart = url[..qIdx];
            var query = url[(qIdx + 1)..];
            var hashIdx = query.IndexOf('#');
            var hashPart = "";
            if (hashIdx >= 0)
            {
                hashPart = query[hashIdx..];
                query = query[..hashIdx];
            }
            var trackingParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content",
                "fbclid", "gclid", "yclid", "_openstat", "ysclid", "wbraid", "gbraid", "mc_eid"
            };
            var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            var kept = new List<string>();
            bool stripped = false;
            foreach (var p in pairs)
            {
                var eq = p.IndexOf('=');
                var k = eq >= 0 ? p[..eq].Trim() : p.Trim();
                if (trackingParams.Contains(k))
                {
                    stripped = true;
                    continue;
                }
                kept.Add(p);
            }
            if (!stripped) return url;
            var cleanQuery = kept.Count > 0 ? "?" + string.Join("&", kept) : "";
            return basePart + cleanQuery + hashPart;
        }
        catch { return url; }
    }

    public static string GetInjectionScript(
        bool blockCookies = true,
        bool blockTrackers = true,
        bool blockAds = true,
        bool blockFingerprint = true,
        bool blockWebRTC = true,
        bool blockCanvas = false,
        bool blockAutoplay = false,
        bool blockCryptomining = true,
        bool sendDNT = true,
        bool lazyLoadImages = false,
        string customCss = "",
        string savedAccountsJson = "[]",
        string accentColor = "#BEF47B",
        bool enableStealth = true,
        string stealthDomains = "max.gosuslugi.ru, gosuslugi.ru",
        bool stealthBlockGeo = true,
        bool stealthBlockFp = true,
        bool stealthBlockWebRtc = true,
        bool stealthBlockSensors = true)
    {
        var rawList = (stealthDomains ?? "").Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim().ToLowerInvariant().TrimStart('*').TrimStart('.'))
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct();
        var domainsJson = System.Text.Json.JsonSerializer.Serialize(rawList);

        return ScriptTemplate
            .Replace("__BC__", blockCookies ? "true" : "false")
            .Replace("__BT__", blockTrackers ? "true" : "false")
            .Replace("__BA__", blockAds ? "true" : "false")
            .Replace("__BFP__", blockFingerprint ? "true" : "false")
            .Replace("__BRTC__", blockWebRTC ? "true" : "false")
            .Replace("__BCVS__", blockCanvas ? "true" : "false")
            .Replace("__BAP__", blockAutoplay ? "true" : "false")
            .Replace("__BCM__", blockCryptomining ? "true" : "false")
            .Replace("__DNT__", sendDNT ? "true" : "false")
            .Replace("__LAZY__", lazyLoadImages ? "true" : "false")
            .Replace("__CSS__", customCss.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$"))
            .Replace("__SAVED_ACCOUNTS__", savedAccountsJson)
            .Replace("__ACCENT__", !string.IsNullOrEmpty(accentColor) ? accentColor : "#BEF47B")
            .Replace("__ACCENT_GLOW__", "rgba(190,244,123,0.35)")
            .Replace("__ST_ACT__", enableStealth ? "true" : "false")
            .Replace("__ST_DOMS__", domainsJson)
            .Replace("__ST_GEO__", stealthBlockGeo ? "true" : "false")
            .Replace("__ST_FP__", stealthBlockFp ? "true" : "false")
            .Replace("__ST_RTC__", stealthBlockWebRtc ? "true" : "false")
            .Replace("__ST_SENS__", stealthBlockSensors ? "true" : "false");
    }

    private static readonly string ScriptTemplate = @"(function(){
'use strict';
var BC=__BC__,BT=__BT__,BA=__BA__,BFP=__BFP__;
var BRTC=__BRTC__,BCVS=__BCVS__,BAP=__BAP__;
var BCM=__BCM__,DNT=__DNT__,LAZY=__LAZY__,UCSS='__CSS__';
var ST_ACT=__ST_ACT__,ST_DOMS=__ST_DOMS__,ST_GEO=__ST_GEO__,ST_FP=__ST_FP__,ST_RTC=__ST_RTC__,ST_SENS=__ST_SENS__;
var H=location.hostname;
var isStealth=false;
if(ST_ACT&&ST_DOMS&&ST_DOMS.length){
  var hL=H.toLowerCase();
  for(var si=0;si<ST_DOMS.length;si++){
    var sd=ST_DOMS[si];
    if(sd&&(hL===sd||hL.endsWith('.'+sd)||hL.indexOf(sd)!==-1)){
      isStealth=true;
      break;
    }
  }
}
if(isStealth){
  if(ST_GEO){
    try{
      if(navigator.geolocation){
        navigator.geolocation.getCurrentPosition=function(s,e){
          if(e)setTimeout(function(){e({code:1,message:'User denied Geolocation'});},10);
        };
        navigator.geolocation.watchPosition=function(s,e){
          if(e)setTimeout(function(){e({code:1,message:'User denied Geolocation'});},10);
          return 0;
        };
      }
      if(window.Intl&&Intl.DateTimeFormat){
        var _oRes=Intl.DateTimeFormat.prototype.resolvedOptions;
        Intl.DateTimeFormat.prototype.resolvedOptions=function(){
          var o=_oRes.apply(this,arguments);
          o.timeZone='UTC';
          return o;
        };
      }
      Date.prototype.getTimezoneOffset=function(){return 0;};
    }catch(e){}
  }
  if(ST_FP){
    try{
      var _td=HTMLCanvasElement.prototype.toDataURL;
      var _gd=CanvasRenderingContext2D.prototype.getImageData;
      HTMLCanvasElement.prototype.toDataURL=function(){
        var ctx=this.getContext&&this.getContext('2d');
        if(ctx&&this.width>0&&this.height>0){
          try{
            var img=_gd.call(ctx,0,0,1,1);
            img.data[0]^=1;
            ctx.putImageData(img,0,0);
          }catch(ex){}
        }
        return _td.apply(this,arguments);
      };
      CanvasRenderingContext2D.prototype.getImageData=function(){
        var res=_gd.apply(this,arguments);
        if(res&&res.data&&res.data.length>4){
          res.data[0]^=1;
          res.data[res.data.length-2]^=1;
        }
        return res;
      };
    }catch(e){}
    try{
      var glTypes=[window.WebGLRenderingContext,window.WebGL2RenderingContext];
      for(var gi=0;gi<glTypes.length;gi++){
        var Gl=glTypes[gi];
        if(Gl&&Gl.prototype){
          var _origGP=Gl.prototype.getParameter;
          Gl.prototype.getParameter=function(p){
            if(p===0x9245)return 'Google Inc. (Google)';
            if(p===0x9246)return 'ANGLE (Google, Vulkan 1.3.0 (SwiftShader Device (Subzero)), SwiftShader driver)';
            if(p===0x1F00)return 'WebKit';
            if(p===0x1F01)return 'WebKit WebGL';
            return _origGP.apply(this,arguments);
          };
        }
      }
    }catch(e){}
    try{
      if(window.AudioBuffer){
        var _origGCD=AudioBuffer.prototype.getChannelData;
        AudioBuffer.prototype.getChannelData=function(){
          var d=_origGCD.apply(this,arguments);
          if(d&&d.length>0)d[0]+=(Math.random()*0.0000001);
          return d;
        };
      }
      if(window.AnalyserNode){
        var _origGFD=AnalyserNode.prototype.getFloatFrequencyData;
        AnalyserNode.prototype.getFloatFrequencyData=function(a){
          _origGFD.apply(this,arguments);
          if(a&&a.length>0)a[0]+=(Math.random()*0.001);
        };
      }
    }catch(e){}
    try{
      Object.defineProperty(screen,'width',{get:function(){return 1920;}});
      Object.defineProperty(screen,'height',{get:function(){return 1080;}});
      Object.defineProperty(screen,'availWidth',{get:function(){return 1920;}});
      Object.defineProperty(screen,'availHeight',{get:function(){return 1040;}});
      Object.defineProperty(screen,'colorDepth',{get:function(){return 24;}});
      Object.defineProperty(screen,'pixelDepth',{get:function(){return 24;}});
      Object.defineProperty(window,'devicePixelRatio',{get:function(){return 1;}});
      Object.defineProperty(navigator,'hardwareConcurrency',{get:function(){return 4;}});
      Object.defineProperty(navigator,'deviceMemory',{get:function(){return 8;}});
      Object.defineProperty(navigator,'maxTouchPoints',{get:function(){return 0;}});
      Object.defineProperty(navigator,'platform',{get:function(){return 'Win32';}});
    }catch(e){}
  }
  if(ST_RTC){
    try{
      window.RTCPeerConnection=undefined;
      window.webkitRTCPeerConnection=undefined;
      window.RTCDataChannel=undefined;
      window.RTCSessionDescription=undefined;
    }catch(e){}
  }
  if(ST_SENS){
    try{
      if(navigator.getBattery)navigator.getBattery=function(){return Promise.reject(new Error('Battery disabled'));};
      if(navigator.clipboard){
        navigator.clipboard.read=function(){return Promise.reject(new DOMException('Access denied','NotAllowedError'));};
        navigator.clipboard.readText=function(){return Promise.reject(new DOMException('Access denied','NotAllowedError'));};
      }
      if(navigator.mediaDevices){
        navigator.mediaDevices.enumerateDevices=function(){return Promise.resolve([]);};
        navigator.mediaDevices.getUserMedia=function(){return Promise.reject(new DOMException('Permission denied','NotAllowedError'));};
      }
      window.DeviceOrientationEvent=undefined;
      window.DeviceMotionEvent=undefined;
      window.Accelerometer=undefined;
      window.Gyroscope=undefined;
      window.LinearAccelerationSensor=undefined;
      window.AbsoluteOrientationSensor=undefined;
      window.AmbientLightSensor=undefined;
      navigator.bluetooth=undefined;
      navigator.usb=undefined;
      navigator.serial=undefined;
      navigator.hid=undefined;
      if(navigator.sendBeacon)navigator.sendBeacon=function(){return true;};
      if(navigator.serviceWorker)navigator.serviceWorker.register=function(){return Promise.reject(new Error('ServiceWorker disabled'));};
    }catch(e){}
  }
}
var IS_YT=(H.indexOf('youtube')!==-1||H.indexOf('youtu.be')!==-1||H.indexOf('googlevideo')!==-1);
var IS_MEDIA_SITE=(IS_YT||H.indexOf('spotify')!==-1||H.indexOf('scdn')!==-1);

if(LAZY){(function(){
  function applyLazy(){
    var imgs=document.querySelectorAll('img:not([loading])');
    for(var i=0;i<imgs.length;i++){try{imgs[i].setAttribute('loading','lazy');}catch(e){}}
    var iframes=document.querySelectorAll('iframe:not([loading])');
    for(var j=0;j<iframes.length;j++){try{iframes[j].setAttribute('loading','lazy');}catch(e){}}
  }
  if(document.readyState==='loading'){document.addEventListener('DOMContentLoaded',applyLazy);}
  else{applyLazy();}
  var _lazyT=null;
  try{
    new MutationObserver(function(){
      if(!_lazyT){
        _lazyT=setTimeout(function(){_lazyT=null;applyLazy();},1500);
      }
    }).observe(document.documentElement,{childList:true,subtree:true});
  }catch(e){}
})();}

if(IS_YT){(function(){
  if(!document.getElementById('nova-yt-adblock')){
    var _st=document.createElement('style');
    _st.id='nova-yt-adblock';
    _st.textContent=
      '#player-ads,ytd-action-companion-ad-renderer,ytd-banner-promo-renderer,ytd-display-ad-renderer,'+
      'ytd-in-feed-ad-layout-renderer,ytd-promoted-sparkles-web-renderer,ytd-promoted-video-renderer,'+
      'ytd-search-pyv-renderer,ytd-video-masthead-ad-v3-renderer,ytd-primetime-promo-renderer,'+
      'yt-mealbar-promo-renderer,.ytd-promoted-video-renderer,#masthead-ad,#promotedSparklesTextBlock,'+
      '.ytp-ad-overlay-container,.ytp-ad-text-overlay,.ytp-paid-content-overlay,.ytp-ce-element,'+
      '.ytp-ad-persistent-progress-bar-container,.video-ads{display:none!important}'+
      '.ytp-ad-skip-button-container{opacity:1!important;pointer-events:auto!important}';
    (document.head||document.documentElement).appendChild(_st);
  }
  function ytCheck(){
    var btn=document.querySelector('.ytp-skip-ad-button,.ytp-ad-skip-button,.ytp-ad-skip-button-modern');
    if(btn)try{btn.click();}catch(e){}
    var isAd=(document.querySelector('.ad-showing,.ad-interrupting')!==null);
    if(isAd){
      var vid=document.querySelector('.html5-main-video,video.html5-main-video');
      if(vid&&vid.duration&&isFinite(vid.duration)&&vid.duration>0){
        vid.muted=true;
        vid.playbackRate=16;
        vid.currentTime=vid.duration-0.05;
      }
    }
  }
  setInterval(ytCheck,1000);
})();}

if(DNT){try{Object.defineProperty(navigator,'doNotTrack',{get:function(){return'1';},configurable:true});}catch(e){}}
if(BFP){
  try{if(navigator.getBattery)navigator.getBattery=function(){return Promise.resolve({charging:true,chargingTime:0,dischargingTime:Infinity,level:1});};}catch(e){}
  try{Object.defineProperty(navigator,'plugins',{get:function(){return[];},configurable:true});}catch(e){}
}
if(BCVS&&!IS_MEDIA_SITE){
  try{
    var _tdurl=HTMLCanvasElement.prototype.toDataURL;
    var _gid=CanvasRenderingContext2D.prototype.getImageData;
    HTMLCanvasElement.prototype.toDataURL=function(){
      var c=this.getContext&&this.getContext('2d');
      if(c){try{var d=_gid.call(c,0,0,1,1);d.data[0]^=1;c.putImageData(d,0,0);}catch(e){}}
      return _tdurl.apply(this,arguments);
    };
  }catch(e){}
}
if(BRTC){
  try{
    var _OrigRTC=window.RTCPeerConnection||window.webkitRTCPeerConnection;
    if(_OrigRTC){
      var _NoIPRTC=function(c,o){if(c&&c.iceServers)c.iceServers=[];return new _OrigRTC(c,o);};
      _NoIPRTC.prototype=_OrigRTC.prototype;
      try{window.RTCPeerConnection=_NoIPRTC;}catch(e){}
      try{window.webkitRTCPeerConnection=_NoIPRTC;}catch(e){}
    }
  }catch(e){}
}
if(BCM){
  var M=['coinhive','coin-hive','jsecoin','crypto-loot','authedmine','minero.pw','monerominer'];
  if(window.WebSocket){
    var _oWS=window.WebSocket;
    window.WebSocket=function(u,p){
      if(M.some(function(m){return String(u).indexOf(m)!==-1;}))throw new Error('Nova:miner');
      return new _oWS(u,p);
    };
    window.WebSocket.prototype=_oWS.prototype;
  }
}
if(BAP&&!IS_MEDIA_SITE&&window!==window.top){
  try{
    var _oPlay=HTMLMediaElement.prototype.play;
    HTMLMediaElement.prototype.play=function(){
      if(this.muted)return _oPlay.apply(this,arguments);
      return Promise.reject(new DOMException('Nova:autoplay','NotAllowedError'));
    };
  }catch(e){}
}
if(BC){
  var CS=[
    '#onetrust-banner-sdk','#onetrust-consent-sdk','#cookie-law-info-bar',
    '#gdpr-cookie-notice','#cookieConsent','#cookie-notice','#cookie-banner',
    '#cookie-bar','#cmp-container','#sp-cc','#didomi-popup',
    '#qc-cmp2-container','#usercentrics-root','#consent-banner',
    '#trustarc-banner-container',
    '.cc-window','.cc-banner','.cc-overlay','.cc-revoke',
    '.cookie-banner','.cookie-notice','.cookie-consent','.cookie-bar',
    '.cookie-popup','.cookie-wall','.gdpr-banner','.gdpr-consent',
    '.fc-consent-root','.fc-dialog-container','.fc-dialog-overlay',
    '.cmp-popup','.consent-popup','.consent-overlay'
  ];
  try{
    var _cstyle=document.createElement('style');
    _cstyle.id='nova-cookies';
    _cstyle.textContent=CS.join(',')+'{display:none!important;visibility:hidden!important;height:0!important;max-height:0!important;overflow:hidden!important;pointer-events:none!important}';
    (document.head||document.documentElement).appendChild(_cstyle);
  }catch(e){}
  function hcb(){
    try{if(document.body)document.body.style.removeProperty('overflow');}catch(e){}
  }
  [300,1000,3000].forEach(function(t){setTimeout(hcb,t);});
  try{window.__tcfapi=function(){};window.__cmp=function(){};}catch(e){}
}
if(BA){
  try{
    var _s=document.createElement('style');
    _s.id='nova-adblock';
    var SEL=[
      'ins.adsbygoogle','ins[data-ad-slot]','div.adsbygoogle',
      '[id^=google_ads]','[id^=div-gpt-ad]','[id^=ad-slot]',
      '[id^=yandex_rtb]','[class*=yandex-adaptive]','[class*=ya-partner]','[class*=ya-rtb]',
      '.ad-banner','.ad-bar','.ad-billboard','.ad-block','.ad-bottom',
      '.ad-box','.ad-break','.ad-container','.ad-content','.ad-dock',
      '.ad-element','.ad-embed','.ad-frame','.ad-header','.ad-holder',
      '.ad-image','.ad-inline','.ad-insert','.ad-interstitial',
      '.ad-label','.ad-leaderboard','.ad-link','.ad-links',
      '.ad-module','.ad-notice','.ad-overlay','.ad-place',
      '.ad-placeholder','.ad-placement','.ad-rail','.ad-rectangle',
      '.ad-region','.ad-row','.ad-section','.ad-sidebar',
      '.ad-sky','.ad-skyscraper','.ad-slot','.ad-space','.ad-spot',
      '.ad-square','.ad-sticky','.ad-strip','.ad-unit',
      '.ad-widget','.ad-wrap','.ad-wrapper','.ad-zone',
      '.ads-area','.ads-bar','.ads-block','.ads-container',
      '.ads-display','.ads-footer','.ads-header','.ads-holder',
      '.ads-leaderboard','.ads-module','.ads-overlay','.ads-panel',
      '.ads-placeholder','.ads-rail','.ads-section','.ads-sidebar',
      '.ads-slot','.ads-space','.ads-strip','.ads-unit','.ads-widget',
      '.ads-wrapper','.adsense','.adsbygoogle','.adslot','.adspace',
      '.advert','.advert-box','.advert-header','.advert-leaderboard',
      '.advert-top','.advertise','.advertisement','.advertisement-area',
      '.advertisement-bar','.advertisement-block','.advertisements',
      '.advertorial','.adverts','.advertising','.advertising-banner',
      '.advertising-block','.advertising-label',
      '.sponsored','.sponsored-ad','.sponsored-ads',
      '.sponsored-article','.sponsored-badge','.sponsored-block',
      '.sponsored-content','.sponsored-link','.sponsored-links',
      '.sponsored-listing','.sponsored-post','.sponsored-results',
      '.sponsored-section','.promoted','.promoted-content',
      '.promoted-story','.promoted-video',
      '.promo-ad','.promo-banner','.promo-block','.promo-box',
      '.ob-widget','.ob-unit','.ob-container','.outbrain',
      '.tbl-feed-card','.tbl-rec','.taboola','.taboola-ad',
      '[id*=taboola]','[class*=taboola]',
      '[id*=outbrain]','[class*=OUTBRAIN]',
      '.sticky-ad','.sticky-ads','.sticky-banner',
      '.float-ad','.floating-ad','.fixed-ad','.pinned-ad',
      '.video-ads','.video-ad-container','.video-ad-overlay',
      '[data-ad]','[data-ad-id]','[data-ad-slot]','[data-ad-unit]',
      '[data-ad-client]','[data-ad-code]','[data-ad-type]',
      '[data-adunit]','[data-adslot]','[data-google-query-id]',
      '[data-dfp-ad]','[data-gpt-ad]',
      'iframe[src*=doubleclick.net]',
      'iframe[src*=googlesyndication.com]',
      'iframe[src*=googleadservices.com]',
      'iframe[src*=adnxs.com]',
      'iframe[src*=rubiconproject.com]',
      'iframe[src*=pubmatic.com]',
      'iframe[src*=openx.net]',
      'iframe[src*=criteo.net]',
      'iframe[src*=outbrain.com]',
      'iframe[src*=taboola.com]',
      'iframe[src*=moatads.com]',
      'iframe[src*=adsrvr.org]',
      'iframe[src*=advertising.com]',
      'iframe[src*=sharethrough.com]',
      'iframe[src*=an.yandex.ru]'
    ].join(',');
    _s.textContent=SEL+'{display:none!important;visibility:hidden!important;'+
      'opacity:0!important;pointer-events:none!important;'+
      'height:0!important;max-height:0!important;overflow:hidden!important}';
    (document.head||document.documentElement).appendChild(_s);
    function removeAdNodes(){
      try{
        var nodes=document.querySelectorAll(SEL);
        for(var i=0;i<nodes.length;i++){
          try{nodes[i].remove();}catch(e){nodes[i].style.display='none';}
        }
      }catch(e){}
    }
    if(document.readyState==='loading'){document.addEventListener('DOMContentLoaded',removeAdNodes);}
    else{removeAdNodes();}
    try{
      var _adObs=new MutationObserver(function(){removeAdNodes();});
      _adObs.observe(document.documentElement,{childList:true,subtree:true});
    }catch(e){}
  }catch(e){}
  try{
    var _gt={cmd:{push:function(f){try{f();}catch(e){}}},
      defineSlot:function(){return{addService:function(){return this;},setTargeting:function(){return this;},defineSizeMapping:function(){return this;}};},
      pubads:function(){return{enableSingleRequest:function(){},setTargeting:function(){},refresh:function(){},display:function(){},addEventListener:function(){},enableLazyLoad:function(){},collapseEmptyDivs:function(){}};},
      companionAds:function(){return{setRefreshUnfilledSlots:function(){}};},
      enableServices:function(){},display:function(){},
      destroySlots:function(){},sizeMapping:function(){return{addSize:function(){return this;},build:function(){return[];}};}
    };
    if(!window.googletag||!window.googletag._defined)
      try{window.googletag=_gt;window.googletag._defined=true;}catch(e){}
  }catch(e){}
  try{
    if(!window.adsbygoogle||window.adsbygoogle.length===undefined)
      window.adsbygoogle={push:function(){},loaded:true,pauseAdRequests:1};
  }catch(e){}
}
if(BT){
  var TK=[
    'google-analytics.com/collect','google-analytics.com/j/collect',
    '/gtm.js','googletagmanager.com/gtag',
    'doubleclick.net/pagead','doubleclick.net/adj',
    'facebook.net/signals','fbevents.js',
    '/hotjar.com','/mouseflow.com',
    'mixpanel.com/track','heap.io/h/track',
    'fullstory.com/s/fs.js',
    'clarity.ms/collect',
    'mc.yandex.ru/watch','metrika.yandex.ru',
    'scorecardresearch.com/p',
    'bat.bing.com/action',
    'analytics.tiktok.com',
    'sentry.io/api','bugsnag.com/notify'
  ];
  function isTK(u){if(!u)return false;for(var i=0;i<TK.length;i++)if(String(u).indexOf(TK[i])!==-1)return true;return false;}
  var _XO=XMLHttpRequest.prototype.open;
  XMLHttpRequest.prototype.open=function(m,u){if(isTK(u)){this.__nvb=true;return;}_XO.apply(this,arguments);};
  var _XS=XMLHttpRequest.prototype.send;
  XMLHttpRequest.prototype.send=function(){if(this.__nvb)return;_XS.apply(this,arguments);};
  if(typeof window.fetch==='function'){
    var _F=window.fetch;
    window.fetch=function(r,i){
      var u=(typeof r==='string')?r:((r&&r.url)||'');
      if(isTK(u))return Promise.reject(new TypeError('Nova:tracker'));
      return _F.apply(this,arguments);
    };
  }
  if(navigator.sendBeacon){
    var _B=navigator.sendBeacon.bind(navigator);
    navigator.sendBeacon=function(u,d){if(isTK(u))return false;return _B(u,d);};
  }
  window.addEventListener('load',function(){
    var stubs={ga:function(){},_gaq:{push:function(){}},gtag:function(){},
      fbq:function(){},_fbq:function(){},dataLayer:{push:function(){}},
      mixpanel:{track:function(){},identify:function(){},people:{set:function(){}}},
      amplitude:{logEvent:function(){},setUserId:function(){}},
      hj:function(){},ym:function(){}};
    for(var k in stubs)if(!window[k])try{window[k]=stubs[k];}catch(e){}
  });
}
(function(){
  function setupUniversalFormMonitor(){
    document.addEventListener('submit',function(e){
      try{
        var form=e.target;
        if(!form)return;
        var pInput=form.querySelector('input[type=password]');
        if(!pInput||!pInput.value)return;
        var uInput=form.querySelector('input[type=text],input[type=email],input[name*=user],input[name*=login],input[name*=mail],input[id*=user],input[id*=login]');
        var uVal=uInput?uInput.value:'';
        if(window.chrome&&window.chrome.webview){
          window.chrome.webview.postMessage({
            type:'novaPromptSavePassword',
            login:uVal,
            password:pInput.value,
            site:location.hostname
          });
        }
      }catch(err){}
    },true);
  }
  if(document.readyState==='loading'){document.addEventListener('DOMContentLoaded',setupUniversalFormMonitor);}
  else{setupUniversalFormMonitor();}
})();

(function(){
  var SA = __SAVED_ACCOUNTS__;
  if(!SA || !SA.length) return;
  var _dd = null;
  function closeDd(){
    if(_dd && _dd.parentNode) _dd.parentNode.removeChild(_dd);
    _dd = null;
  }
  function openDd(input){
    closeDd();
    var r = input.getBoundingClientRect();
    _dd = document.createElement('div');
    _dd.style.position = 'fixed';
    _dd.style.left = r.left + 'px';
    _dd.style.top = (r.bottom + 4) + 'px';
    _dd.style.zIndex = '2147483647';
    _dd.style.background = '#141722';
    _dd.style.border = '1px solid #BEF47B';
    _dd.style.borderRadius = '8px';
    _dd.style.boxShadow = '0 8px 24px rgba(0,0,0,0.6)';
    _dd.style.padding = '6px';
    _dd.style.minWidth = Math.max(r.width, 220) + 'px';
    _dd.style.fontFamily = 'system-ui, -apple-system, sans-serif';
    _dd.style.fontSize = '12px';
    _dd.style.color = '#FFFFFF';

    var h = document.createElement('div');
    h.style.padding = '4px 8px 6px 8px';
    h.style.fontSize = '10px';
    h.style.textTransform = 'uppercase';
    h.style.letterSpacing = '1px';
    h.style.color = '#8A99AD';
    h.style.borderBottom = '1px solid rgba(255,255,255,0.08)';
    h.style.marginBottom = '4px';
    h.textContent = 'Nova Passwords';
    _dd.appendChild(h);

    for(var i=0; i<SA.length; i++){
      (function(acc){
        var row = document.createElement('div');
        row.style.padding = '6px 8px';
        row.style.cursor = 'pointer';
        row.style.borderRadius = '4px';
        row.style.display = 'flex';
        row.style.alignItems = 'center';
        row.style.justifyContent = 'space-between';
        var spanL = document.createElement('span');
        spanL.style.fontWeight = '600';
        spanL.textContent = acc.login || '';
        row.appendChild(spanL);
        var spanS = document.createElement('span');
        spanS.style.color = '#8A99AD';
        spanS.style.fontSize = '11px';
        spanS.textContent = acc.site || 'Везде';
        row.appendChild(spanS);
        row.onmouseover = function(){ row.style.background = 'rgba(0,212,255,0.15)'; };
        row.onmouseout = function(){ row.style.background = 'transparent'; };
        row.onmousedown = function(e){
          e.preventDefault();
          var form = input.form || input.closest('form') || document;
          var uInp = form.querySelector('input[type=text],input[type=email],input[name*=user],input[name*=login],input[name*=mail]');
          var pInp = form.querySelector('input[type=password]');
          if(uInp && acc.login){
            uInp.value = acc.login;
            uInp.dispatchEvent(new Event('input', { bubbles: true }));
            uInp.dispatchEvent(new Event('change', { bubbles: true }));
          }
          if(pInp && acc.password){
            pInp.value = acc.password;
            pInp.dispatchEvent(new Event('input', { bubbles: true }));
            pInp.dispatchEvent(new Event('change', { bubbles: true }));
          }
          closeDd();
        };
        _dd.appendChild(row);
      })(SA[i]);
    }
    document.body.appendChild(_dd);
  }
  document.addEventListener('focusin', function(e){
    var t = e.target;
    if(t && t.tagName === 'INPUT' && (t.type === 'password' || t.type === 'text' || t.type === 'email')){
      openDd(t);
    }
  });
  document.addEventListener('focusout', function(){ setTimeout(closeDd, 200); });
  window.addEventListener('scroll', closeDd, true);
})();

if(UCSS&&UCSS.length>2){
  try{
    var _cs=document.createElement('style');
    _cs.id='nova-custom-css';
    _cs.textContent=UCSS;
    (document.head||document.documentElement).appendChild(_cs);
  }catch(e){}
}
})();";
}
