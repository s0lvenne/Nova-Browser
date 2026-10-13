#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace NovaBrowser.Models;

public class BrowserSettings
{
    public string BrowserLanguage { get; set; } = "ru-RU";
    public string SearchEngine { get; set; } = "Nova";
    public string HomepageUrl { get; set; } = "";
    public bool RestoreLastSession { get; set; } = true;
    public bool ConfirmCloseMultipleTabs { get; set; } = false;
    public bool IsFirstRun { get; set; } = true;
    public string CustomWallpaperUrl { get; set; } = "";
    public int WallpaperBlur { get; set; } = 0;
    public string SelectedCurrencies { get; set; } = "USD,EUR";
    public string UserCity { get; set; } = "";
    public bool AutoDetectLocation { get; set; } = true;
    public bool OpenLinksInNewTab { get; set; } = false;

    public bool ProxyEnabled { get; set; } = false;
    public string ProxyType { get; set; } = "Direct";
    public string ProxyHost { get; set; } = "";
    public int ProxyPort { get; set; } = 8080;
    public string ProxyUsername { get; set; } = "";
    public string ProxyPassword { get; set; } = "";
    public string ProxyBypassList { get; set; } = "localhost;127.0.0.1;*.local";
    public string SelectedProxyPresetId { get; set; } = "direct";

    public bool SearchSourceGoogle { get; set; } = true;
    public bool SearchSourceYandex { get; set; } = true;
    public bool SearchSourceWiki { get; set; } = true;
    public bool SearchSourceHabr { get; set; } = true;
    public bool SearchSourceGitHub { get; set; } = true;
    public bool SearchSourceKinopoisk { get; set; } = true;
    public bool SearchSourceYouTube { get; set; } = true;
    public bool SearchSourceVK { get; set; } = true;
    public string SearchSiteFilter { get; set; } = "";

    public string Theme { get; set; } = "Dark";
    public string AccentColor { get; set; } = "#BEF47B";
    public string HtmlAccentColor { get; set; } = "#BEF47B";
    public string HtmlBackgroundColor { get; set; } = "#07090d";
    public string HtmlThemeMode { get; set; } = "Dark";
    public string LogoColor { get; set; } = "";
    public string CustomTitleBarColor { get; set; } = "";
    public string CustomNavBarColor { get; set; } = "";
    public string CustomAddressBarColor { get; set; } = "";
    public string CustomCardColor { get; set; } = "";
    public string CustomPopupColor { get; set; } = "";
    public string CustomBorderColor { get; set; } = "";
    public string CustomTextColor { get; set; } = "";
    public string CustomTextSecondaryColor { get; set; } = "";
    public string CustomGlowColor { get; set; } = "";
    public string CustomSidebarColor { get; set; } = "";
    public string CustomAddressBarGradient { get; set; } = "";
    public string CustomTitleBarGradient { get; set; } = "";
    public string CustomNavBarGradient { get; set; } = "";
    public string CustomCardGradient { get; set; } = "";
    public string CustomWindowGradient { get; set; } = "";
    public string CustomSidebarGradient { get; set; } = "";
    public string NewTabBackground { get; set; } = "";
    public bool ShowBookmarksBar { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;
    public bool ShowMemoryUsage { get; set; } = true;
    public bool ShowClock { get; set; } = false;
    public bool CompactMode { get; set; } = false;
    public bool ShowFavicons { get; set; } = true;
    public int TabRadius { get; set; } = 10;
    public double WindowOpacity { get; set; } = 1.0;
    public bool LargeText { get; set; } = false;
    public bool ShowHomepageWallpaper { get; set; } = false;
    public int WallpaperPresetIndex { get; set; } = -1;
    public string CustomWallpaperPath { get; set; } = "";
    public string LiveWallpaperPath { get; set; } = "";
    public string WallpaperFit { get; set; } = "UniformToFill";
    public int WallpaperOpacity { get; set; } = 60;
    public bool AnimationsEnabled { get; set; } = true;
    public bool ReduceAnimations { get; set; } = false;
    public bool SmoothScrolling { get; set; } = true;
    public int FontSize { get; set; } = 14;
    public string UIFontFamily { get; set; } = "Segoe UI, sans-serif";

    public bool HomeShowSearchBar { get; set; } = true;
    public bool HomeShowQuickLinks { get; set; } = true;
    public string HomeBackgroundStyle { get; set; } = "Blobs";
    public string HomeBackgroundImage { get; set; } = "";
    public double HomeBgImageOpacity { get; set; } = 0.35;
    public List<QuickLinkItem> HomeQuickLinks { get; set; } = new()
    {
        new QuickLinkItem { Title = "YouTube", Url = "https://youtube.com" },
        new QuickLinkItem { Title = "X", Url = "https://x.com" },
        new QuickLinkItem { Title = "GitHub", Url = "https://github.com" },
        new QuickLinkItem { Title = "Reddit", Url = "https://reddit.com" },
        new QuickLinkItem { Title = "Wikipedia", Url = "https://wikipedia.org" },
        new QuickLinkItem { Title = "DuckDuckGo", Url = "https://duckduckgo.com" },
    };

    public bool BlockCookieBanners { get; set; } = true;
    public bool BlockTrackers { get; set; } = true;
    public bool BlockAds { get; set; } = true;
    public bool BlockNotifications { get; set; } = true;
    public bool BlockPopups { get; set; } = true;
    public bool BlockAutoplay { get; set; } = false;
    public bool BlockCryptomining { get; set; } = true;
    public bool BlockFingerprinting { get; set; } = false;
    public bool BlockWebRTC { get; set; } = false;
    public bool BlockCanvas { get; set; } = false;
    public bool BlockMixedContent { get; set; } = true;
    public bool SendDoNotTrack { get; set; } = true;
    public bool ForceHttps { get; set; } = false;
    public bool SafeBrowsingMode { get; set; } = false;
    public string CustomBlockedDomains { get; set; } = "";
    public bool EnableSiteStealthIsolation { get; set; } = true;
    public string StealthIsolationDomains { get; set; } = "max.gosuslugi.ru, gosuslugi.ru";
    public bool StealthBlockGeo { get; set; } = true;
    public bool StealthBlockFingerprint { get; set; } = true;
    public bool StealthBlockWebRTC { get; set; } = true;
    public bool StealthBlockSensorsAndHardware { get; set; } = true;
    public bool StealthStripTrackingHeaders { get; set; } = true;

    public bool JavaScriptEnabled { get; set; } = true;
    public bool LoadImages { get; set; } = true;
    public bool HardwareAcceleration { get; set; } = true;
    public bool ForceDarkMode { get; set; } = false;
    public bool EnablePDFViewer { get; set; } = true;
    public bool EnableAutofill { get; set; } = true;
    public bool EnablePasswordAutofill { get; set; } = true;
    public bool AntiStealerProtection { get; set; } = true;
    public bool AntiLeakProtection { get; set; } = true;
    public bool UniversalPasswordAutofill { get; set; } = true;
    public List<string> InstalledExtensionPaths { get; set; } = new();

    public List<SavedAccountItem> SavedAccounts { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore]
    public List<SavedPasswordItem> SavedPasswords { get; set; } = new();
    public bool EnableSpellCheck2 { get; set; } = false;
    public bool OpenDevToolsOnF12 { get; set; } = true;
    public string UserAgent { get; set; } = "";
    public string CustomCss { get; set; } = "";

    public string DownloadFolder { get; set; } = "";
    public bool AskDownloadLocation { get; set; } = false;
    public bool AutoOpenDownloads { get; set; } = false;
    public bool ShowDownloadBadge { get; set; } = true;

    public int DefaultZoom { get; set; } = 100;
    public bool PreloadHomepage { get; set; } = false;
    public bool ClearOnExit { get; set; } = false;

    public int RamWarningThresholdMB { get; set; } = 1500;
    public bool RamSaverEnabled { get; set; } = true;
    public int RamLimitThresholdMB { get; set; } = 1500;
    public bool GpuAcceleration { get; set; } = true;
    public bool PreloadPages { get; set; } = true;
    public bool EnablePhishingProtection { get; set; } = true;
    public bool LazyLoadImages { get; set; } = false;
    public bool ClearCacheOnStartup { get; set; } = false;

    public List<string> LastSession { get; set; } = new();
    public int LastSessionActiveIndex { get; set; } = 0;
    public List<BookmarkItem> Bookmarks { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public List<HistoryItem> History { get; set; } = new();

    public static string DataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NovaBrowser");
    public static string ExeDir => AppDomain.CurrentDomain.BaseDirectory;
    private static string SettingsPath => Path.Combine(ExeDir, "settings.json");
    private static string FallbackSettingsPath => Path.Combine(DataDir, "settings.json");
    private static string HistoryPath => Path.Combine(DataDir, "history.json");
    private static string PasswordsPath => Path.Combine(DataDir, "passwords.json");
    private static string AccountsPath => Path.Combine(DataDir, "accounts.json");

    public static BrowserSettings Load()
    {
        BrowserSettings s = new();
        try
        {
            string? json = null;
            if (File.Exists(SettingsPath) && File.Exists(FallbackSettingsPath))
            {
                var fi1 = new FileInfo(SettingsPath);
                var fi2 = new FileInfo(FallbackSettingsPath);
                json = fi2.LastWriteTimeUtc > fi1.LastWriteTimeUtc
                    ? File.ReadAllText(FallbackSettingsPath)
                    : File.ReadAllText(SettingsPath);
            }
            else if (File.Exists(SettingsPath))
            {
                json = File.ReadAllText(SettingsPath);
            }
            else if (File.Exists(FallbackSettingsPath))
            {
                json = File.ReadAllText(FallbackSettingsPath);
            }

            if (!string.IsNullOrEmpty(json))
            {
                s = JsonSerializer.Deserialize<BrowserSettings>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new BrowserSettings();
                MigrateHistoryIfNeeded(json, s);
            }
        }
        catch { }

        try
        {
            if (File.Exists(PasswordsPath))
            {
                var pJson = File.ReadAllText(PasswordsPath);
                var pList = JsonSerializer.Deserialize<List<SavedPasswordItem>>(pJson);
                if (pList != null) s.SavedPasswords = pList;
            }
        }
        catch { }

        try
        {
            if (File.Exists(AccountsPath))
            {
                var aJson = File.ReadAllText(AccountsPath);
                var aList = JsonSerializer.Deserialize<List<SavedAccountItem>>(aJson);
                if (aList != null) s.SavedAccounts = aList;
            }
        }
        catch { }

        s.HomeQuickLinks ??= new();
        s.Bookmarks ??= new();
        s.SavedAccounts ??= new();
        s.SavedPasswords ??= new();
        s.InstalledExtensionPaths ??= new();
        s.LastSession ??= new();

        return s;
    }

    private static void MigrateHistoryIfNeeded(string oldJson, BrowserSettings s)
    {
        try
        {
            if (File.Exists(HistoryPath)) return;
            using var doc = JsonDocument.Parse(oldJson);
            if (!doc.RootElement.TryGetProperty("History", out var histEl)) return;
            if (histEl.ValueKind != JsonValueKind.Array || histEl.GetArrayLength() == 0) return;
            Directory.CreateDirectory(DataDir);
            File.WriteAllText(HistoryPath, histEl.GetRawText());
            Task.Run(() => s.Save());
        }
        catch { }
    }

    public void LoadHistoryAsync()
    {
        Task.Run(() =>
        {
            try
            {
                if (!File.Exists(HistoryPath)) return;
                var json = File.ReadAllText(HistoryPath);
                var items = JsonSerializer.Deserialize<List<HistoryItem>>(json);
                if (items != null)
                    System.Windows.Application.Current.Dispatcher.Invoke(
                        () => History = items);
            }
            catch { }
        });
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, _jsonOpts);
            try
            {
                File.WriteAllText(SettingsPath, json);
            }
            catch { }

            try
            {
                Directory.CreateDirectory(DataDir);
                File.WriteAllText(FallbackSettingsPath, json);
            }
            catch { }

            try
            {
                Directory.CreateDirectory(DataDir);
                if (SavedPasswords != null)
                {
                    var pJson = JsonSerializer.Serialize(SavedPasswords, _jsonOpts);
                    File.WriteAllText(PasswordsPath, pJson);
                }
            }
            catch { }

            try
            {
                Directory.CreateDirectory(DataDir);
                if (SavedAccounts != null)
                {
                    var aJson = JsonSerializer.Serialize(SavedAccounts, _jsonOpts);
                    File.WriteAllText(AccountsPath, aJson);
                }
            }
            catch { }
        }
        catch { }
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = await Task.Run(() => JsonSerializer.Serialize(this, _jsonOpts));
            try
            {
                await File.WriteAllTextAsync(SettingsPath, json);
            }
            catch { }

            try
            {
                Directory.CreateDirectory(DataDir);
                await File.WriteAllTextAsync(FallbackSettingsPath, json);
            }
            catch { }

            try
            {
                Directory.CreateDirectory(DataDir);
                if (SavedPasswords != null)
                {
                    var pJson = await Task.Run(() => JsonSerializer.Serialize(SavedPasswords, _jsonOpts));
                    await File.WriteAllTextAsync(PasswordsPath, pJson);
                }
            }
            catch { }

            try
            {
                Directory.CreateDirectory(DataDir);
                if (SavedAccounts != null)
                {
                    var aJson = await Task.Run(() => JsonSerializer.Serialize(SavedAccounts, _jsonOpts));
                    await File.WriteAllTextAsync(AccountsPath, aJson);
                }
            }
            catch { }
        }
        catch { }
    }

    public async Task SaveHistoryAsync()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var items = History.Count > 150 ? History.GetRange(0, 150) : History;
            var json = await Task.Run(() => JsonSerializer.Serialize(items, _jsonOpts));
            await File.WriteAllTextAsync(HistoryPath, json);
        }
        catch { }
    }

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { WriteIndented = false };
}