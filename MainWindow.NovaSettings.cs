#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using NovaBrowser.Models;

namespace NovaBrowser;

public partial class MainWindow : Window
{
    private static bool _isFullResetting = false;
    private Grid? _settingsRoot;
    private ScrollViewer? _settingsScroll;
    private StackPanel? _panelGeneral;
    private StackPanel? _panelAccounts;
    private StackPanel? _panelAppearance;
    private StackPanel? _panelSecurity;
    private StackPanel? _panelProxy;
    private StackPanel? _panelPerformance;
    private StackPanel? _panelMisc;

    private Button? _btnNavGeneral;
    private Button? _btnNavAccounts;
    private Button? _btnNavAppearance;
    private Button? _btnNavSecurity;
    private Button? _btnNavProxy;
    private Button? _btnNavPerformance;
    private Button? _btnNavMisc;

    private Border? _floatingApplyBar;
    private Border? _savedToastBanner;
    private Grid? _unsavedModal;
    private Grid? _fullResetModal;
    private Action? _pendingNavigationAction;

    private bool _isSettingsDirty;
    private bool _suppressDirtyEvents;
    private string _activeSettingsSection = "General";
            private ComboBox? _sWallpaperPreset;
    private TextBox? _sCustomWpPath;
    private Slider? _sWpDimSlider;
    private Border? _accentPreviewSwatch;
    private Border? _bgPreviewSwatch;
    private Border? _colorPickerModal;
    private Border? _gradientModal;
    private TextBlock? _gradTitleText;
    private Border? _gradPreviewBox;
    private Border? _gradTrackBar;
    private Slider? _gradAngleSlider;
    private TextBlock? _gradAngleValueText;
    private StackPanel? _gradStopsChipsPanel;
    private string _gradTargetLabel = "Адресная строка";
    private string _gradTargetResource = "BgInputBrush";
    private double _gradAngle = 90;
    private List<(Color color, double offset)> _gradStops = new();
    private Action<string>? _colorPickerOnApply;
    private Action<string>? _colorPickerOnLive;
    private string _colorPickerInitialHex = "";
    private Slider? _cpSliderR;
    private Slider? _cpSliderG;
    private Slider? _cpSliderB;
    private TextBox? _cpTextR;
    private TextBox? _cpTextG;
    private TextBox? _cpTextB;
    private TextBox? _cpTextHex;
    private Border? _cpOldColorPreview;
    private Border? _cpNewColorPreview;
    private TextBlock? _cpTitleText;

    private Border? _clearDataModal;
    private ComboBox? _comboCleanTimeRange;
    private CheckBox? _chkCleanHistory;
    private CheckBox? _chkCleanCookies;
    private CheckBox? _chkCleanCache;
    private CheckBox? _chkCleanPasswords;
    private CheckBox? _chkCleanAutofill;
    private CheckBox? _chkCleanDownloads;

    private CheckBox? _sAntiStealerJar;
    private CheckBox? _sAntiLeakOutbound;
    private CheckBox? _sUniversalPassAutofill;
    private StackPanel? _extensionsListContainer;
    private readonly List<(Border chip, string hex)> _accentChips = new();
    private readonly List<(Border chip, string hex)> _bgChips = new();
    private bool _navInterceptionHooked;

    private ComboBox? _sLanguage;
    private ComboBox? _sSearchEngine;
    private TextBox? _sHomepage;
    private ComboBox? _sZoom;
    private CheckBox? _sNewTab;
    private CheckBox? _sRestoreSession;
    private CheckBox? _sConfirmCloseTabs;
    private TextBox? _sDownloadFolder;

    private CheckBox? _sSourceGoogle;
    private CheckBox? _sSourceYandex;
    private CheckBox? _sSourceWiki;
    private CheckBox? _sSourceHabr;
    private CheckBox? _sSourceGitHub;
    private CheckBox? _sSourceKinopoisk;
    private CheckBox? _sSourceYouTube;
    private CheckBox? _sSourceVK;
    private TextBox? _sSiteFilter;

    private ComboBox? _sTheme;
    private TextBox? _sAccentColor;
    private TextBox? _sHtmlBgColor;
    private TextBox? _sCustomTitleBarColor;
    private TextBox? _sCustomNavBarColor;
    private TextBox? _sCustomAddressBarColor;
    private TextBox? _sCustomCardColor;
    private TextBox? _sCustomPopupColor;
    private TextBox? _sCustomBorderColor;
    private TextBox? _sCustomTextColor;
    private TextBox? _sCustomTextSecColor;
    private TextBox? _sCustomGlowColor;
    private TextBox? _sCustomSidebarColor;
    private ComboBox? _sTabRadius;
    private CheckBox? _sGlow;
    private CheckBox? _sAnimations;
    private CheckBox? _sBookmarksBar;
    private CheckBox? _sStatusBar;

    private CheckBox? _sBlockAds;
    private CheckBox? _sBlockTrackers;
    private CheckBox? _sBlockCookieBanners;
    private CheckBox? _sForceHttps;
    private CheckBox? _sBlockPopups;
    private CheckBox? _sPhishingProtection;
    private CheckBox? _sBlockFingerprinting;
    private CheckBox? _sSendDNT;
    private CheckBox? _sBlockAutoplay;
    private CheckBox? _sBlockCrypto;
    private CheckBox? _sEnableSiteStealth;
    private TextBox? _sStealthIsolationDomains;
    private CheckBox? _sStealthBlockGeo;
    private CheckBox? _sStealthBlockFingerprint;
    private CheckBox? _sStealthBlockSensors;
    private CheckBox? _sStealthBlockWebRTC;
    private CheckBox? _sStealthStripTracking;

    private CheckBox? _sRamSaver;
    private ComboBox? _sRamLimit;
    private CheckBox? _sGpuAcceleration;
    private CheckBox? _sPreloadPages;


    private CheckBox? _sClearOnExit;
    private CheckBox? _sDevToolsF12;
    private CheckBox? _sAskDownload;
    private CheckBox? _sPasswordAutofill;

    private StackPanel? _accountsListContainer;
    private StackPanel? _passwordsListContainer;
    private StackPanel? _addAccountForm;
    private StackPanel? _addPasswordForm;

    private ComboBox? _newAccService;
    private TextBox? _newAccEmail;
    private TextBox? _newAccPassword;
    private Border? _accountsModal;
    private StackPanel? _accountsModalList;
    private TextBox? _accountsModalSearch;

    private TextBox? _newPassSite;
    private TextBox? _newPassLogin;
    private TextBox? _newPassValue;

    private CheckBox? _sProxyEnabled;
    private TextBlock? _proxyStatusBadge;
    private Border? _proxyStatusBorder;
    private ComboBox? _sProxyType;
    private TextBox? _sProxyHost;
    private TextBox? _sProxyPort;
    private TextBox? _sProxyUser;
    private PasswordBox? _sProxyPass;
    private TextBox? _sProxyBypass;
    private string _selectedProxyPresetId = "direct";
    private StackPanel? _presetsContainer;
    private TextBlock? _proxyScanStatusText;
    private Button? _btnRefreshProxies;
    private readonly List<(string id, Border card, TextBlock pingBadge, Button btnSelect, Button btnTest)> _proxyPresetCards = new();

    private static readonly (string id, string name, string proto, string host, int port, string desc, string category)[] _localClientCandidates = new[]
    {
        ("tor_browser", "Tor Browser SOCKS5 (Порт 9150)", "SOCKS5", "127.0.0.1", 9150, "Официальный клиент Tor Browser (активен на этом ПК).", "Локальный клиент"),
        ("tor_socks", "Tor Expert / System SOCKS5 (Порт 9050)", "SOCKS5", "127.0.0.1", 9050, "Системная служба Tor / Orbot (активна на этом ПК).", "Локальный клиент"),
        ("tor_privoxy", "Privoxy / Tor HTTP Bridge", "HTTP", "127.0.0.1", 8118, "Локальный HTTP прокси-шлюз Tor / Privoxy.", "Локальный клиент"),
        ("clash", "Clash / Mihomo / OpenClash", "SOCKS5", "127.0.0.1", 7890, "Клиент Clash / Mihomo / Verge (активен на этом ПК).", "Локальный клиент"),
        ("xray_socks", "Xray / v2ray / NekoRay SOCKS5", "SOCKS5", "127.0.0.1", 10808, "Клиент v2rayN / sing-box / NekoBox (активен на этом ПК).", "Локальный клиент"),
        ("xray_http", "Xray / v2ray HTTP Inbound", "HTTP", "127.0.0.1", 10809, "Входящий HTTP шлюз v2rayN (активен на этом ПК).", "Локальный клиент"),
        ("shadowsocks", "Shadowsocks Local SOCKS5", "SOCKS5", "127.0.0.1", 1080, "Клиент Shadowsocks-Windows (активен на этом ПК).", "Локальный клиент")
    };

    private static readonly (string id, string name, string proto, string host, int port, string desc, string category)[] _publicCandidatePool = new[]
    {
        ("pub_us_1", "США (Vultr Cloud)", "HTTP", "108.61.29.163", 10001, "Высокоскоростной облачный сервер в США (Vultr).", "Публичный сервер"),
        ("pub_de_1", "Германия (DigitalOcean Cloud)", "HTTP", "159.65.230.46", 8888, "Европейский сервер в Германии (Франкфурт).", "Публичный сервер"),
        ("pub_nl_1", "Нидерланды (Амстердам Cloud)", "HTTP", "195.26.224.135", 80, "Сервер в Амстердаме для европейских сайтов.", "Публичный сервер"),
        ("pub_oracle_1", "Европа (Oracle Cloud)", "HTTP", "140.238.14.161", 8080, "Выделенный узел Oracle Cloud Infrastructure.", "Публичный сервер"),
        ("pub_asia_1", "Сингапур (Alibaba Cloud)", "SOCKS5", "8.215.25.3", 2080, "Азиатский SOCKS5 датацентровый узел Alibaba Cloud.", "Публичный сервер"),
        ("pub_asia_2", "Азиатско-Тихоокеанский узел", "HTTP", "39.109.113.97", 4090, "Азиатский прокси с хорошей пропускной способностью.", "Публичный сервер"),
        ("pub_us_2", "США (Нью-Йорк)", "HTTP", "38.194.246.34", 999, "Американский узел для доступа к сервисам США.", "Публичный сервер"),
        ("pub_us_3", "США (Датацентр)", "HTTP", "45.225.206.248", 999, "Быстрый сервер в США для обхода блокировок.", "Публичный сервер")
    };

    private sealed class SettingsSnapshotData
    {
        public string Language = "ru-RU";
        public string SearchEngine = "Nova";
        public string Homepage = "";
        public int Zoom = 100;
        public bool NewTab = true;
        public bool RestoreSession = true;
        public bool ConfirmCloseTabs = false;
        public string DownloadFolder = "";

        public bool SourceGoogle = true;
        public bool SourceYandex = true;
        public bool SourceWiki = true;
        public bool SourceHabr = true;
        public bool SourceGitHub = true;
        public bool SourceKinopoisk = true;
        public bool SourceYouTube = true;
        public bool SourceVK = true;
        public string SiteFilter = "";

        public bool ProxyEnabled = false;
        public string ProxyType = "Direct";
        public string ProxyHost = "";
        public int ProxyPort = 8080;
        public string ProxyUsername = "";
        public string ProxyPassword = "";
        public string ProxyBypassList = "localhost;127.0.0.1;*.local";
        public string SelectedProxyPresetId = "direct";

        public int WallpaperPreset = 0;
        public string WallpaperCustomPath = "";
        public int WallpaperDimming = 35;

        public string Theme = "Dark";
        public string AccentColor = "#BEF47B";
        public string HtmlBgColor = "#07090d";
        public string CustomTitleBarColor = "";
        public string CustomNavBarColor = "";
        public string CustomAddressBarColor = "";
        public string CustomCardColor = "";
        public string CustomPopupColor = "";
        public string CustomBorderColor = "";
        public string CustomTextColor = "";
        public string CustomTextSecColor = "";
        public string CustomGlowColor = "";
        public string CustomSidebarColor = "";
        public string CustomAddressBarGradient = "";
        public string CustomTitleBarGradient = "";
        public string CustomNavBarGradient = "";
        public string CustomCardGradient = "";
        public string CustomSidebarGradient = "";
        public int TabRadius = 10;
        public bool Glow = true;
        public bool Animations = true;
        public bool BookmarksBar = true;
        public bool StatusBar = true;

        public bool BlockAds = true;
        public bool BlockTrackers = true;
        public bool BlockCookies = true;
        public bool ForceHttps = false;
        public bool BlockPopups = true;
        public bool PhishingProtection = true;
        public bool BlockFingerprinting = false;
        public bool SendDNT = true;
        public bool BlockAutoplay = false;
        public bool BlockCrypto = true;

        public bool RamSaver = true;
        public int RamLimit = 1500;
        public bool GpuAcceleration = true;
        public bool PreloadPages = true;


        public bool ClearOnExit = false;
        public bool DevToolsF12 = true;
        public bool AskDownload = false;
        public bool PasswordAutofill = true;
        public bool AntiStealer = true;
        public bool AntiLeak = true;
        public bool UniversalPassAutofill = true;
        public bool EnableSiteStealth = true;
        public string StealthDomains = "max.gosuslugi.ru, gosuslugi.ru";
        public bool StealthBlockGeo = true;
        public bool StealthBlockFingerprint = true;
        public bool StealthBlockSensors = true;
        public bool StealthBlockWebRTC = true;
        public bool StealthStripTracking = true;

        public bool Matches(SettingsSnapshotData? other)
        {
            if (other == null) return false;
            return Language == other.Language &&
                   SearchEngine == other.SearchEngine &&
                   Homepage == other.Homepage &&
                   Zoom == other.Zoom &&
                   NewTab == other.NewTab &&
                   RestoreSession == other.RestoreSession &&
                   ConfirmCloseTabs == other.ConfirmCloseTabs &&
                   DownloadFolder == other.DownloadFolder &&
                   SourceGoogle == other.SourceGoogle &&
                   SourceYandex == other.SourceYandex &&
                   SourceWiki == other.SourceWiki &&
                   SourceHabr == other.SourceHabr &&
                   SourceGitHub == other.SourceGitHub &&
                   SourceKinopoisk == other.SourceKinopoisk &&
                   SourceYouTube == other.SourceYouTube &&
                   SourceVK == other.SourceVK &&
                   SiteFilter == other.SiteFilter &&
                   WallpaperPreset == other.WallpaperPreset &&
                   WallpaperCustomPath == other.WallpaperCustomPath &&
                   WallpaperDimming == other.WallpaperDimming &&
                   Theme == other.Theme &&
                   AccentColor == other.AccentColor &&
                   HtmlBgColor == other.HtmlBgColor &&
                   TabRadius == other.TabRadius &&
                   Glow == other.Glow &&
                   Animations == other.Animations &&
                   BookmarksBar == other.BookmarksBar &&
                   StatusBar == other.StatusBar &&
                   BlockAds == other.BlockAds &&
                   BlockTrackers == other.BlockTrackers &&
                   BlockCookies == other.BlockCookies &&
                   ForceHttps == other.ForceHttps &&
                   BlockPopups == other.BlockPopups &&
                   PhishingProtection == other.PhishingProtection &&
                   BlockFingerprinting == other.BlockFingerprinting &&
                   SendDNT == other.SendDNT &&
                   BlockAutoplay == other.BlockAutoplay &&
                   BlockCrypto == other.BlockCrypto &&
                   RamSaver == other.RamSaver &&
                   RamLimit == other.RamLimit &&
                   GpuAcceleration == other.GpuAcceleration &&
                   PreloadPages == other.PreloadPages &&

                   ClearOnExit == other.ClearOnExit &&
                   DevToolsF12 == other.DevToolsF12 &&
                   AskDownload == other.AskDownload &&
                   PasswordAutofill == other.PasswordAutofill &&
                   AntiStealer == other.AntiStealer &&
                   AntiLeak == other.AntiLeak &&
                   UniversalPassAutofill == other.UniversalPassAutofill &&
                   EnableSiteStealth == other.EnableSiteStealth &&
                   StealthDomains == other.StealthDomains &&
                   StealthBlockGeo == other.StealthBlockGeo &&
                   StealthBlockFingerprint == other.StealthBlockFingerprint &&
                   StealthBlockSensors == other.StealthBlockSensors &&
                   StealthBlockWebRTC == other.StealthBlockWebRTC &&
                   StealthStripTracking == other.StealthStripTracking &&
                   ProxyEnabled == other.ProxyEnabled &&
                   ProxyType == other.ProxyType &&
                   ProxyHost == other.ProxyHost &&
                   ProxyPort == other.ProxyPort &&
                   ProxyUsername == other.ProxyUsername &&
                   ProxyPassword == other.ProxyPassword &&
                   ProxyBypassList == other.ProxyBypassList &&
                   SelectedProxyPresetId == other.SelectedProxyPresetId &&
                   CustomTitleBarColor == other.CustomTitleBarColor &&
                   CustomNavBarColor == other.CustomNavBarColor &&
                   CustomAddressBarColor == other.CustomAddressBarColor &&
                   CustomCardColor == other.CustomCardColor &&
                   CustomPopupColor == other.CustomPopupColor &&
                   CustomBorderColor == other.CustomBorderColor &&
                   CustomTextColor == other.CustomTextColor &&
                   CustomTextSecColor == other.CustomTextSecColor &&
                   CustomGlowColor == other.CustomGlowColor &&
                   CustomSidebarColor == other.CustomSidebarColor &&
                   CustomAddressBarGradient == other.CustomAddressBarGradient &&
                   CustomTitleBarGradient == other.CustomTitleBarGradient &&
                   CustomNavBarGradient == other.CustomNavBarGradient &&
                   CustomCardGradient == other.CustomCardGradient &&
                   CustomSidebarGradient == other.CustomSidebarGradient;
        }
    }

    private SettingsSnapshotData _savedSnapshot = new();

    private Brush GetThemeBrush(string key, Color fallback)
    {
        try
        {
            if (Application.Current?.TryFindResource(key) is Brush b1) return b1;
            if (TryFindResource(key) is Brush b2) return b2;
        }
        catch { }
        return new SolidColorBrush(fallback);
    }

        private static Color ParseHexColor(string hex, Color fallback)
    {
        try
        {
            if (ColorConverter.ConvertFromString(hex) is Color c)
                return c;
        }
        catch { }
        return fallback;
    }

    private static Brush ParseHexBrush(string hex, Color fallback)
    {
        try
        {
            if (ColorConverter.ConvertFromString(hex) is Color c)
                return new SolidColorBrush(c);
        }
        catch { }
        return new SolidColorBrush(fallback);
    }

    public void OpenSettingsTabSafe()
    {
        try
        {
            var existing = _tabs.FirstOrDefault(t => t.Url == "nova://settings");
            if (existing != null)
            {
                if (existing != _activeTab)
                {
                    if (_activeTab?.Url == "nova://settings" && _isSettingsDirty)
                    {
                        ShowUnsavedModal(() => SwitchToTab(existing));
                        return;
                    }
                    _activeTab = existing;
                    UpdateTabsUI();
                }
            }
            else
            {
                if (_activeTab?.Url == "nova://settings" && _isSettingsDirty)
                {
                    ShowUnsavedModal(() =>
                    {
                        var tab = new BrowserTab { Title = "Настройки", Url = "nova://settings" };
                        _tabs.Add(tab);
                        _activeTab = tab;
                        UpdateTabsUI();
                        OpenSettingsTabSafe();
                    });
                    return;
                }
                var newTab = new BrowserTab { Title = "Настройки", Url = "nova://settings" };
                _tabs.Add(newTab);
                _activeTab = newTab;
                UpdateTabsUI();
            }

            HideAllWebViews();

            if (HomePage != null) HomePage.Visibility = Visibility.Collapsed;
            if (BlockedPage != null) BlockedPage.Visibility = Visibility.Collapsed;
            if (HistoryPage != null) HistoryPage.Visibility = Visibility.Collapsed;
            if (BookmarksPage != null) BookmarksPage.Visibility = Visibility.Collapsed;
            if (WallpaperLayer != null) WallpaperLayer.Visibility = Visibility.Collapsed;

            foreach (var tab in _tabs)
            {
                if (tab.WebViewControl != null && tab != _activeTab)
                {
                    tab.WebViewControl.Visibility = Visibility.Hidden;
                }
            }

            EnsureSettingsUiBuilt();

            if (SettingsPage != null)
            {
                SettingsPage.BeginAnimation(UIElement.OpacityProperty, null);
                SettingsPage.RenderTransform = null;
                SettingsPage.Opacity = 1.0;
                SettingsPage.Visibility = Visibility.Visible;
                Panel.SetZIndex(SettingsPage, 2000);
            }

            _isOnHomePage = false;
            if (AddressBox != null)
            {
                AddressBox.Text = string.Empty;
            }

            LoadSettingsIntoUi();
            _savedSnapshot = CaptureCurrentSnapshot();
            SetDirtyState(false);
        }
        catch { }
    }

    public void EnsureSettingsUiBuilt()
    {
        if (SettingsPage == null) return;

        if (_settingsRoot != null)
        {
            if (!SettingsPage.Children.Contains(_settingsRoot))
            {
                SettingsPage.Children.Clear();
                SettingsPage.ColumnDefinitions.Clear();
                SettingsPage.RowDefinitions.Clear();
                SettingsPage.Children.Add(_settingsRoot);
            }
            _settingsRoot.Visibility = Visibility.Visible;
            _settingsRoot.Opacity = 1.0;
            SettingsPage.BeginAnimation(UIElement.OpacityProperty, null);
            SettingsPage.RenderTransform = null;
            if (SettingsPage.Opacity < 1.0) SettingsPage.Opacity = 1.0;
            if (SettingsPage.Visibility != Visibility.Visible) SettingsPage.Visibility = Visibility.Visible;
            if (Panel.GetZIndex(SettingsPage) < 2000) Panel.SetZIndex(SettingsPage, 2000);
            return;
        }

        try
        {
            SettingsPage.Children.Clear();
            SettingsPage.ColumnDefinitions.Clear();
            SettingsPage.RowDefinitions.Clear();

            var bgDarkBrush = GetThemeBrush("BgDarkBrush", Color.FromRgb(10, 10, 10));
            SettingsPage.Background = bgDarkBrush;

            _settingsRoot = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = bgDarkBrush,
                Visibility = Visibility.Visible,
                Opacity = 1.0
            };
            _settingsRoot.SetResourceReference(Grid.BackgroundProperty, "BgDarkBrush");

            _settingsRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            _settingsRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            SettingsPage.Children.Add(_settingsRoot);

            var sidebar = BuildSettingsSidebar();
            Grid.SetColumn(sidebar, 0);
            _settingsRoot.Children.Add(sidebar);

            var contentContainer = new Grid
            {
                Background = bgDarkBrush,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            contentContainer.SetResourceReference(Grid.BackgroundProperty, "BgDarkBrush");
            Grid.SetColumn(contentContainer, 1);

            _settingsScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(36, 20, 36, 28),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var mainStack = new StackPanel
            {
                MaxWidth = 900,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            try { _panelGeneral = BuildGeneralPanel(); mainStack.Children.Add(_panelGeneral); } catch { }
            try { _panelAccounts = BuildAccountsPanel(); mainStack.Children.Add(_panelAccounts); } catch { }
            try { _panelAppearance = BuildAppearancePanel(); mainStack.Children.Add(_panelAppearance); } catch { }
            try { _panelSecurity = BuildSecurityPanel(); mainStack.Children.Add(_panelSecurity); } catch { }
            try { _panelProxy = BuildProxyPanel(); mainStack.Children.Add(_panelProxy); } catch { }
            try { _panelPerformance = BuildPerformancePanel(); mainStack.Children.Add(_panelPerformance); } catch { }
            try { _panelMisc = BuildMiscPanel(); mainStack.Children.Add(_panelMisc); } catch { }

            _settingsScroll.Content = mainStack;
            contentContainer.Children.Add(_settingsScroll);

            try { _floatingApplyBar = BuildFloatingApplyBar(); contentContainer.Children.Add(_floatingApplyBar); } catch { }
            try { _savedToastBanner = BuildToastBanner(); contentContainer.Children.Add(_savedToastBanner); } catch { }

            _settingsRoot.Children.Add(contentContainer);

            try
            {
                _unsavedModal = BuildUnsavedModal();
                Grid.SetColumnSpan(_unsavedModal, 2);
                Panel.SetZIndex(_unsavedModal, 500);
                _settingsRoot.Children.Add(_unsavedModal);
            }
            catch { }

            try
            {
                _accountsModal = BuildAccountsModal();
                Grid.SetColumnSpan(_accountsModal, 2);
                Panel.SetZIndex(_accountsModal, 600);
                _settingsRoot.Children.Add(_accountsModal);
            }
            catch { }

            try
            {
                _colorPickerModal = BuildColorPickerModal();
                Grid.SetColumnSpan(_colorPickerModal, 2);
                Panel.SetZIndex(_colorPickerModal, 700);
                _settingsRoot.Children.Add(_colorPickerModal);
            }
            catch { }

            try
            {
                _clearDataModal = BuildClearDataModal();
                Grid.SetColumnSpan(_clearDataModal, 2);
                Panel.SetZIndex(_clearDataModal, 750);
                _settingsRoot.Children.Add(_clearDataModal);
            }
            catch { }

            try
            {
                _gradientModal = BuildGradientCustomizerModal();
                Grid.SetColumnSpan(_gradientModal, 2);
                Panel.SetZIndex(_gradientModal, 720);
                _settingsRoot.Children.Add(_gradientModal);
            }
            catch { }

            try
            {
                _fullResetModal = BuildFullResetModal();
                Grid.SetColumnSpan(_fullResetModal, 2);
                Panel.SetZIndex(_fullResetModal, 500);
                _settingsRoot.Children.Add(_fullResetModal);
            }
            catch { }

            SelectSettingsSection("General");
            try
            {
                this.Closing -= OnMainWindowClosing_SaveSettings;
                this.Closing += OnMainWindowClosing_SaveSettings;
            }
            catch { }
            HookNavigationInterception();
            LoadSettingsIntoUi();
            _savedSnapshot = CaptureCurrentSnapshot();
            SetDirtyState(false);

            SettingsPage.BeginAnimation(UIElement.OpacityProperty, null);
            SettingsPage.RenderTransform = null;
            SettingsPage.Opacity = 1.0;
            SettingsPage.Visibility = Visibility.Visible;
            Panel.SetZIndex(SettingsPage, 2000);
        }
        catch { }
    }

    private Border BuildSettingsSidebar()
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(12, 16, 12, 16),
            Background = GetThemeBrush("BgCardBrush", Color.FromRgb(20, 20, 20)),
            BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(34, 34, 34))
        };
        border.SetResourceReference(Border.BackgroundProperty, "BgCardBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

        var stack = new StackPanel();

        var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 6, 8, 20), VerticalAlignment = VerticalAlignment.Center };
        var iconTitle = new TextBlock
        {
            Text = "\uE713",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            Foreground = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123))
        };
        iconTitle.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
        var titleText = new TextBlock
        {
            Text = "Настройки",
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        titleText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        var badge = new Border
        {
            CornerRadius = new CornerRadius(4),
            Background = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
            Padding = new Thickness(6, 1, 6, 2),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        badge.SetResourceReference(Border.BackgroundProperty, "AccentBrush");
        var badgeText = new TextBlock
        {
            Text = "NOVA",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black
        };
        badge.Child = badgeText;
        titleStack.Children.Add(iconTitle);
        titleStack.Children.Add(titleText);
        titleStack.Children.Add(badge);
        stack.Children.Add(titleStack);

        _btnNavGeneral = CreateSidebarButton("Общие", "\uE713", () => SelectSettingsSection("General"));
        _btnNavAccounts = CreateSidebarButton("Аккаунты и привязки", "\uE77B", () => SelectSettingsSection("Accounts"));
        _btnNavAppearance = CreateSidebarButton("Внешний вид", "\uE790", () => SelectSettingsSection("Appearance"));
        _btnNavSecurity = CreateSidebarButton("Безопасность", "\uE72E", () => SelectSettingsSection("Security"));
        _btnNavProxy = CreateSidebarButton("Прокси", "\uE774", () => SelectSettingsSection("Proxy"));
        _btnNavPerformance = CreateSidebarButton("Производительность", "\uE950", () => SelectSettingsSection("Performance"));
        _btnNavMisc = CreateSidebarButton("Прочее", "\uE712", () => SelectSettingsSection("Misc"));

        stack.Children.Add(_btnNavGeneral);
        stack.Children.Add(_btnNavAccounts);
        stack.Children.Add(_btnNavAppearance);
        stack.Children.Add(_btnNavSecurity);
        stack.Children.Add(_btnNavProxy);
        stack.Children.Add(_btnNavPerformance);
        stack.Children.Add(_btnNavMisc);

        var sep = new Separator
        {
            Margin = new Thickness(6, 14, 6, 14),
            Background = GetThemeBrush("BorderBrush", Color.FromRgb(34, 34, 34))
        };
        sep.SetResourceReference(Separator.BackgroundProperty, "BorderBrush");
        stack.Children.Add(sep);

        var btnReset = CreateSidebarActionButton("Сброс настроек", "\uE777", Brushes.LightGray, () => OnResetPendingClicked());
        var btnFullReset = CreateSidebarActionButton("Полный сброс", "\uE74D", new SolidColorBrush(Color.FromRgb(255, 90, 90)), () => ShowFullResetModal());
        var btnClose = CreateSidebarActionButton("<< Закрыть вкладку", "\uE72B", Brushes.LightGray, () => OnCloseSettingsClicked());

        stack.Children.Add(btnReset);
        stack.Children.Add(btnFullReset);
        stack.Children.Add(btnClose);

        border.Child = stack;
        return border;
    }

    private Button CreateSidebarButton(string text, string glyph, Action onClick)
    {
        var btn = new Button
        {
            Margin = new Thickness(0, 2, 0, 2),
            Height = 36,
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Tag = "SettingsInternal"
        };

        var template = new ControlTemplate(typeof(Button));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        template.VisualTree = presenter;
        btn.Template = template;

        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 6, 10, 6),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Width = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(148, 154, 176))
        };
        icon.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(148, 154, 176))
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");

        sp.Children.Add(icon);
        sp.Children.Add(tb);
        border.Child = sp;
        btn.Content = border;

        btn.MouseEnter += (_, _) =>
        {
            if (btn.Tag as string != "ActiveNav")
            {
                border.Background = GetThemeBrush("BgHoverBrush", Color.FromArgb(24, 255, 255, 255));
                if (border.Child is StackPanel sp2 && sp2.Children.Count >= 2)
                {
                    if (sp2.Children[0] is TextBlock ic) ic.Foreground = Brushes.White;
                    if (sp2.Children[1] is TextBlock t) t.Foreground = Brushes.White;
                }
            }
        };
        btn.MouseLeave += (_, _) =>
        {
            if (btn.Tag as string != "ActiveNav")
            {
                border.Background = Brushes.Transparent;
                if (border.Child is StackPanel sp2 && sp2.Children.Count >= 2)
                {
                    if (sp2.Children[0] is TextBlock ic)
                    {
                        ic.Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(148, 154, 176));
                        ic.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
                    }
                    if (sp2.Children[1] is TextBlock t)
                    {
                        t.Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(148, 154, 176));
                        t.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
                    }
                }
            }
        };

        btn.Click += (_, _) => onClick();
        return btn;
    }

    private Button CreateSidebarActionButton(string text, string glyph, Brush fg, Action onClick)
    {
        var btn = new Button
        {
            Margin = new Thickness(0, 3, 0, 3),
            Height = 34,
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Tag = "SettingsInternal"
        };

        var template = new ControlTemplate(typeof(Button));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        template.VisualTree = presenter;
        btn.Template = template;

        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 6, 10, 6),
            Background = Brushes.Transparent
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 13,
            Width = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Foreground = fg
        };
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = fg
        };

        sp.Children.Add(icon);
        sp.Children.Add(tb);
        border.Child = sp;
        btn.Content = border;
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private void SelectSettingsSection(string tag)
    {
        _activeSettingsSection = tag;

        if (_panelGeneral != null) _panelGeneral.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
        if (_panelAccounts != null) _panelAccounts.Visibility = tag == "Accounts" ? Visibility.Visible : Visibility.Collapsed;
        if (_panelAppearance != null) _panelAppearance.Visibility = tag == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        if (_panelSecurity != null) _panelSecurity.Visibility = tag == "Security" ? Visibility.Visible : Visibility.Collapsed;
        if (_panelProxy != null) _panelProxy.Visibility = tag == "Proxy" ? Visibility.Visible : Visibility.Collapsed;
        if (_panelPerformance != null) _panelPerformance.Visibility = tag == "Performance" ? Visibility.Visible : Visibility.Collapsed;
        if (_panelMisc != null) _panelMisc.Visibility = tag == "Misc" ? Visibility.Visible : Visibility.Collapsed;

        HighlightSidebarButton(_btnNavGeneral, tag == "General");
        HighlightSidebarButton(_btnNavAccounts, tag == "Accounts");
        HighlightSidebarButton(_btnNavAppearance, tag == "Appearance");
        HighlightSidebarButton(_btnNavSecurity, tag == "Security");
        HighlightSidebarButton(_btnNavProxy, tag == "Proxy");
        HighlightSidebarButton(_btnNavPerformance, tag == "Performance");
        HighlightSidebarButton(_btnNavMisc, tag == "Misc");

        if (tag == "Proxy")
        {
            _ = RefreshAvailableProxiesAsync();
        }

        try { _settingsScroll?.ScrollToTop(); } catch { }
    }

    private Color GetCurrentAccentColor()
    {
        try
        {
            if (Application.Current?.Resources["AccentColor"] is Color c) return c;
            if (Application.Current?.Resources["AccentBrush"] is SolidColorBrush sb) return sb.Color;
            if (!string.IsNullOrEmpty(_settings?.AccentColor))
                return (Color)ColorConverter.ConvertFromString(_settings.AccentColor);
        }
        catch { }
        return Color.FromRgb(190, 244, 123);
    }

    private void HighlightSidebarButton(Button? btn, bool isActive)
    {
        if (btn == null) return;
        if (btn.Content is Border border)
        {
            if (isActive)
            {
                btn.Tag = "ActiveNav";
                var acc = GetCurrentAccentColor();
                border.Background = new SolidColorBrush(Color.FromArgb(38, acc.R, acc.G, acc.B));
                border.BorderBrush = new SolidColorBrush(acc);
                border.BorderThickness = new Thickness(3, 0, 0, 0);
                if (border.Child is StackPanel sp && sp.Children.Count >= 2)
                {
                    if (sp.Children[0] is TextBlock icon) icon.Foreground = new SolidColorBrush(acc);
                    if (sp.Children[1] is TextBlock tb)
                    {
                        tb.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
                        tb.FontWeight = FontWeights.SemiBold;
                    }
                }
            }
            else
            {
                btn.Tag = "SettingsInternal";
                border.Background = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
                if (border.Child is StackPanel sp && sp.Children.Count >= 2)
                {
                    if (sp.Children[0] is TextBlock icon)
                    {
                        icon.Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(148, 154, 176));
                        icon.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
                    }
                    if (sp.Children[1] is TextBlock tb)
                    {
                        tb.Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(148, 154, 176));
                        tb.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
                        tb.FontWeight = FontWeights.Medium;
                    }
                }
            }
        }
    }

    private UIElement CreateColorConfigRow(string label, TextBox tb, string defaultHex, string resourceKey)
    {
        StyleDarkTextBox(tb);
        var chip = new Border
        {
            Width = 24, Height = 24, CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(60, 68, 96)),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        void UpdateChip(string h)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(h))
                {
                    var col = (Color)ColorConverter.ConvertFromString(h.Trim());
                    chip.Background = new SolidColorBrush(col);
                    if (!string.IsNullOrEmpty(resourceKey))
                    {
                        var b = new SolidColorBrush(col);
                        Application.Current.Resources[resourceKey] = b;
                        if (resourceKey == "TitleBarBrush" && TitleBarGrid != null) TitleBarGrid.Background = b;
                        if (resourceKey == "NavBarBrush" && NavBarGrid != null) NavBarGrid.Background = b;
                        if (resourceKey == "BgInputBrush" && AddressBox != null) AddressBox.Background = b;
                        if (resourceKey == "BorderBrush" && MainBorder != null) MainBorder.BorderBrush = b;
                    }
                }
                else
                {
                    chip.Background = Brushes.Transparent;
                }
            }
            catch { }
        }
        tb.TextChanged += (_, _) => { UpdateChip(tb.Text); OnInputChanged(); };
        UpdateChip(tb.Text);

        var btnPick = new Button
        {
            Content = "Выбрать...",
            Height = 30,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(8, 0, 0, 0)
        };
        StyleDarkButton(btnPick, false);
        btnPick.Click += (_, _) =>
        {
            var cur = !string.IsNullOrWhiteSpace(tb.Text) ? tb.Text : defaultHex;
            OpenRgbColorPicker($"Выбор цвета: {label}", cur, hex =>
            {
                tb.Text = hex;
                UpdateChip(hex);
                OnInputChanged();
            }, liveHex =>
            {
                tb.Text = liveHex;
                UpdateChip(liveHex);
            });
        };

        var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var tbLabel = new TextBlock
        {
            Text = label, FontSize = 13, VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        tbLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        Grid.SetColumn(tbLabel, 0);
        row.Children.Add(tbLabel);

        var controlsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        controlsStack.Children.Add(chip);
        controlsStack.Children.Add(tb);
        controlsStack.Children.Add(btnPick);

        var btnGrad = new Button
        {
            Content = "🌈 Градиент",
            Height = 30,
            Padding = new Thickness(8, 0, 8, 0),
            Margin = new Thickness(6, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        StyleDarkButton(btnGrad, false);
        btnGrad.Click += (_, _) => OpenGradientCustomizer(label, resourceKey);
        controlsStack.Children.Add(btnGrad);
        Grid.SetColumn(controlsStack, 1);
        row.Children.Add(controlsStack);

        return row;
    }

    private Border CreateCard(string titleText, string? subtitleText = null)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(22, 18, 22, 18),
            Margin = new Thickness(0, 0, 0, 16),
            BorderThickness = new Thickness(1),
            Background = GetThemeBrush("BgSurfaceBrush", Color.FromRgb(22, 25, 38)),
            BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(36, 41, 62))
        };
        border.SetResourceReference(Border.BackgroundProperty, "BgSurfaceBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

        var stack = new StackPanel();

        var tbTitle = new TextBlock
        {
            Text = titleText,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, subtitleText == null ? 12 : 4),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        tbTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        stack.Children.Add(tbTitle);

        if (!string.IsNullOrEmpty(subtitleText))
        {
            var tbSub = new TextBlock
            {
                Text = subtitleText,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 14),
                TextWrapping = TextWrapping.Wrap,
                Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(148, 154, 176))
            };
            tbSub.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            stack.Children.Add(tbSub);
        }

        border.Child = stack;
        return border;
    }

    private void StyleDarkTextBox(TextBox tb)
    {
        tb.SetResourceReference(TextBox.BackgroundProperty, "BgInputBrush");
        tb.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
        tb.SetResourceReference(TextBox.BorderBrushProperty, "BorderBrush");
        tb.BorderThickness = new Thickness(1);
        tb.Padding = new Thickness(12, 0, 12, 0);
        tb.Height = 34;
        tb.VerticalContentAlignment = VerticalAlignment.Center;
        tb.SetResourceReference(TextBox.CaretBrushProperty, "AccentBrush");
        tb.SetResourceReference(TextBox.SelectionBrushProperty, "AccentBrush");
        try
        {
            var template = new ControlTemplate(typeof(TextBox));
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.Name = "Bd";
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
            bd.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));

            var sv = new FrameworkElementFactory(typeof(ScrollViewer));
            sv.Name = "PART_ContentHost";
            sv.SetValue(ScrollViewer.PaddingProperty, new TemplateBindingExtension(TextBox.PaddingProperty));
            sv.SetValue(ScrollViewer.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            sv.SetValue(ScrollViewer.VerticalContentAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(sv);

            template.VisualTree = bd;

            var trgFocus = new Trigger { Property = UIElement.IsFocusedProperty, Value = true };
            trgFocus.Setters.Add(new Setter { TargetName = "Bd", Property = Border.BorderBrushProperty, Value = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)) });
            trgFocus.Setters.Add(new Setter { TargetName = "Bd", Property = Border.BackgroundProperty, Value = GetThemeBrush("BgSurfaceBrush", Color.FromRgb(22, 26, 42)) });
            template.Triggers.Add(trgFocus);

            var trgOver = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trgOver.Setters.Add(new Setter { TargetName = "Bd", Property = Border.BorderBrushProperty, Value = GetThemeBrush("BorderBrush", Color.FromRgb(70, 80, 115)) });
            template.Triggers.Add(trgOver);

            tb.Template = template;
        }
        catch { }
    }

    private void StyleDarkComboBox(ComboBox cb)
    {
        cb.Height = 34;
        cb.SetResourceReference(ComboBox.BackgroundProperty, "BgInputBrush");
        cb.SetResourceReference(ComboBox.ForegroundProperty, "TextPrimaryBrush");
        cb.SetResourceReference(ComboBox.BorderBrushProperty, "BorderBrush");
        cb.BorderThickness = new Thickness(1);
        cb.Cursor = Cursors.Hand;
    }

    private void StyleDarkCheckBox(CheckBox chk)
    {
        chk.Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White);
        chk.SetResourceReference(Control.ForegroundProperty, "TextPrimaryBrush");
        chk.FontSize = 13;
        chk.Cursor = Cursors.Hand;
        chk.Margin = new Thickness(0, 3, 0, 3);
        try
        {
            var template = new ControlTemplate(typeof(CheckBox));
            var grid = new FrameworkElementFactory(typeof(Grid));
            grid.SetValue(Grid.BackgroundProperty, Brushes.Transparent);

            var col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col0.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(col0);
            grid.AppendChild(col1);

            var box = new FrameworkElementFactory(typeof(Border));
            box.Name = "Box";
            box.SetValue(Grid.ColumnProperty, 0);
            box.SetValue(Border.WidthProperty, 18.0);
            box.SetValue(Border.HeightProperty, 18.0);
            box.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            box.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
            box.SetValue(Border.BorderBrushProperty, GetThemeBrush("BorderBrush", Color.FromRgb(55, 63, 92)));
            box.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(16, 19, 30)));
            box.SetValue(Border.VerticalAlignmentProperty, VerticalAlignment.Center);

            var checkGlyph = new FrameworkElementFactory(typeof(TextBlock));
            checkGlyph.Name = "CheckMark";
            checkGlyph.SetValue(TextBlock.TextProperty, "");
            checkGlyph.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe MDL2 Assets"));
            checkGlyph.SetValue(TextBlock.FontSizeProperty, 11.0);
            checkGlyph.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            checkGlyph.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
            checkGlyph.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            checkGlyph.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            checkGlyph.SetValue(TextBlock.VisibilityProperty, Visibility.Collapsed);
            box.AppendChild(checkGlyph);
            grid.AppendChild(box);

            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(Grid.ColumnProperty, 1);
            cp.SetValue(ContentPresenter.MarginProperty, new Thickness(10, 0, 0, 0));
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            grid.AppendChild(cp);

            template.VisualTree = grid;

            var trgChecked = new Trigger { Property = CheckBox.IsCheckedProperty, Value = true };
            trgChecked.Setters.Add(new Setter { TargetName = "Box", Property = Border.BackgroundProperty, Value = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)) });
            trgChecked.Setters.Add(new Setter { TargetName = "Box", Property = Border.BorderBrushProperty, Value = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)) });
            trgChecked.Setters.Add(new Setter { TargetName = "CheckMark", Property = UIElement.VisibilityProperty, Value = Visibility.Visible });
            template.Triggers.Add(trgChecked);

            var trgOver = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trgOver.Setters.Add(new Setter { TargetName = "Box", Property = Border.BorderBrushProperty, Value = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)) });
            template.Triggers.Add(trgOver);

            chk.Template = template;
        }
        catch { }
    }

    private void StyleAccentButton(Button btn) => StyleDarkButton(btn, true);
    private void StyleDarkButton(Button btn, bool isPrimary = false)
    {
        btn.Cursor = Cursors.Hand;
        btn.BorderThickness = new Thickness(1);
        if (isPrimary)
        {
            btn.Background = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123));
            btn.Foreground = Brushes.Black;
            btn.BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123));
        }
        else
        {
            btn.Background = GetThemeBrush("BgHoverBrush", Color.FromRgb(34, 39, 61));
            btn.Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White);
            btn.BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(55, 63, 92));
        }
        try
        {
            var template = new ControlTemplate(typeof(Button));
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.Name = "Bd";
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            bd.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            bd.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            template.VisualTree = bd;

            var trgOver = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trgOver.Setters.Add(new Setter { TargetName = "Bd", Property = Border.BorderBrushProperty, Value = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)) });
            if (!isPrimary)
                trgOver.Setters.Add(new Setter { TargetName = "Bd", Property = Border.BackgroundProperty, Value = new SolidColorBrush(Color.FromRgb(42, 48, 77)) });
            template.Triggers.Add(trgOver);

            btn.Template = template;
        }
        catch { }
    }

    private UIElement CreateRowWithHelp(string labelText, FrameworkElement control, string helpText)
    {
        if (control is TextBox tb) StyleDarkTextBox(tb);
        else if (control is ComboBox cb) StyleDarkComboBox(cb);
        else if (control is Button btn) StyleDarkButton(btn);

        var row = new Grid { Margin = new Thickness(0, 5, 0, 7) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var tbLabel = new TextBlock
        {
            Text = labelText,
            FontSize = 13.5,
            FontWeight = FontWeights.Medium,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        tbLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        Grid.SetColumn(tbLabel, 0);
        row.Children.Add(tbLabel);

        control.Margin = new Thickness(12, 0, 0, 0);
        Grid.SetColumn(control, 1);
        row.Children.Add(control);

        return row;
    }

    private UIElement CreateCheckBoxWithHelp(CheckBox chk, string helpText)
    {
        StyleDarkCheckBox(chk);
        chk.Margin = new Thickness(0, 5, 0, 7);
        return chk;
    }

    private Border BuildFloatingApplyBar()
    {
        var border = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 28, 20),
            Padding = new Thickness(18, 12, 18, 12),
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(22, 25, 35)),
            BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
            Visibility = Visibility.Collapsed
        };
        border.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
        try { border.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 20, ShadowDepth = 6, Opacity = 0.55, Color = Colors.Black }; } catch { }

        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var dot = new Ellipse
        {
            Width = 9,
            Height = 9,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Fill = new SolidColorBrush(Color.FromRgb(255, 170, 0))
        };
        sp.Children.Add(dot);

        var txt = new TextBlock
        {
            Text = "Есть несохранённые изменения",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        txt.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        sp.Children.Add(txt);

        var btnRevert = new Button
        {
            Content = "Сбросить",
            Padding = new Thickness(14, 6, 14, 6),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            FontSize = 12,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnRevert, false);
        btnRevert.Click += (_, _) => OnResetPendingClicked();
        sp.Children.Add(btnRevert);

        var btnApply = new Button
        {
            Content = "Применить",
            Padding = new Thickness(18, 6, 18, 6),
            Cursor = Cursors.Hand,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnApply, true);
        btnApply.Click += (_, _) => OnApplySettingsClicked();
        sp.Children.Add(btnApply);

        border.Child = sp;
        return border;
    }

    private Border BuildToastBanner()
    {
        var border = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(22, 12, 22, 12),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(240, 16, 185, 129)),
            Visibility = Visibility.Collapsed
        };
        try { border.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 4, Opacity = 0.45, Color = Colors.Black }; } catch { }

        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        var checkIcon = new TextBlock
        {
            Text = "\uE73E",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var txt = new TextBlock
        {
            Text = "Настройки успешно сохранены и применены",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };

        sp.Children.Add(checkIcon);
        sp.Children.Add(txt);
        border.Child = sp;
        return border;
    }

    private Grid BuildUnsavedModal()
    {
        var grid = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            Visibility = Visibility.Collapsed
        };

        var card = new Border
        {
            Width = 440,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(24),
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(20, 23, 33)),
            BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123))
        };
        card.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
        try { card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.65, Color = Colors.Black }; } catch { }

        var stack = new StackPanel();

        var icon = new TextBlock
        {
            Text = "\uE7BA",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 30,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 12),
            Foreground = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123))
        };
        icon.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
        stack.Children.Add(icon);

        var title = new TextBlock
        {
            Text = "Несохранённые изменения",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        stack.Children.Add(title);

        var desc = new TextBlock
        {
            Text = "Вы изменили настройки браузера, но не сохранили их. Применить изменения перед переходом?",
            FontSize = 13,
            LineHeight = 18,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20),
            Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(170, 170, 170))
        };
        desc.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        stack.Children.Add(desc);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var btnCancel = new Button
        {
            Content = "Отменить",
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            FontSize = 12,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnCancel, false);
        btnCancel.Click += (_, _) =>
        {
            grid.Visibility = Visibility.Collapsed;
            _pendingNavigationAction = null;
        };
        actions.Children.Add(btnCancel);

        var btnDiscard = new Button
        {
            Content = "Не сохранять",
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            FontSize = 12,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnDiscard, false);
        btnDiscard.Click += (_, _) =>
        {
            grid.Visibility = Visibility.Collapsed;
            LoadSettingsIntoUi();
            SetDirtyState(false);
            var act = _pendingNavigationAction;
            _pendingNavigationAction = null;
            act?.Invoke();
        };
        actions.Children.Add(btnDiscard);

        var btnSaveAndGo = new Button
        {
            Content = "Сохранить и перейти",
            Padding = new Thickness(16, 7, 16, 7),
            Cursor = Cursors.Hand,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnSaveAndGo, true);
        btnSaveAndGo.Click += (_, _) =>
        {
            grid.Visibility = Visibility.Collapsed;
            OnApplySettingsClicked();
            var act = _pendingNavigationAction;
            _pendingNavigationAction = null;
            act?.Invoke();
        };
        actions.Children.Add(btnSaveAndGo);

        stack.Children.Add(actions);
        card.Child = stack;
        grid.Children.Add(card);
        return grid;
    }

    private Grid BuildFullResetModal()
    {
        var grid = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            Visibility = Visibility.Collapsed
        };

        var card = new Border
        {
            Width = 440,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(24),
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            Background = new SolidColorBrush(Color.FromRgb(20, 23, 33))
        };
        try { card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.65, Color = Colors.Black }; } catch { }

        var stack = new StackPanel();

        var icon = new TextBlock
        {
            Text = "\uE74D",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 30,
            Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 12)
        };
        stack.Children.Add(icon);

        var title = new TextBlock
        {
            Text = "Полный сброс параметров",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            Margin = new Thickness(0, 0, 0, 8)
        };
        stack.Children.Add(title);

        var desc = new TextBlock
        {
            Text = "Будут сброшены все настройки, история посещений, сохранённые сеансы и пользовательские стили Nova. Браузер вернётся к заводскому состоянию.",
            FontSize = 13,
            LineHeight = 18,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20),
            Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(170, 170, 170))
        };
        desc.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        stack.Children.Add(desc);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var btnCancel = new Button
        {
            Content = "Отмена",
            Padding = new Thickness(16, 7, 16, 7),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            FontSize = 12,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnCancel, false);
        btnCancel.Click += (_, _) => grid.Visibility = Visibility.Collapsed;
        actions.Children.Add(btnCancel);

        var btnConfirm = new Button
        {
            Content = "Сбросить всё",
            Padding = new Thickness(18, 7, 18, 7),
            Cursor = Cursors.Hand,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnConfirm, true);
        btnConfirm.Click += (_, _) =>
        {
            grid.Visibility = Visibility.Collapsed;
            ShowFullResetModal();
        };
        actions.Children.Add(btnConfirm);

        stack.Children.Add(actions);
        card.Child = stack;
        grid.Children.Add(card);
        return grid;
    }

    public void ShowUnsavedModal(Action onProceed)
    {
        _pendingNavigationAction = onProceed;
        if (_unsavedModal != null)
        {
            _unsavedModal.Visibility = Visibility.Visible;
        }
        else
        {
            onProceed();
        }
    }

    public void ShowFullResetModal()
    {
        var confirm = MessageBox.Show(
            this,
            "ВНИМАНИЕ! Вы собираетесь выполнить полный сброс Nova Browser.\n\n" +
            "Будут безвозвратно удалены абсолютно все данные:\n" +
            "• Все параметры конфигурации, цветовые палитры и темы\n" +
            "• Вся история просмотров и список загрузок\n" +
            "• Все сохранённые пароли, аккаунты и автозаполнение\n" +
            "• Все закладки и сохранённые сеансы вкладок\n" +
            "• Все файлы cookie, кэш и данные сайтов (полная очистка профиля)\n\n" +
            "После сброса браузер будет моментально закрыт и запущен заново в абсолютно чистом состоянии.\n\n" +
            "Вы подтверждаете полный сброс?",
            "Предупреждение: Полный сброс Nova Browser",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirm == MessageBoxResult.Yes)
        {
            PerformFullResetAndRelaunch();
        }
    }

    private void HookNavigationInterception()
    {
        if (_navInterceptionHooked) return;
        _navInterceptionHooked = true;

        if (TabsPanel != null)
        {
            TabsPanel.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (_activeTab?.Url == "nova://settings" && _isSettingsDirty)
                {
                    var dep = e.OriginalSource as DependencyObject;
                    while (dep != null && dep != TabsPanel)
                    {
                        if (dep is Border b && b.Tag is BrowserTab targetTab)
                        {
                            if (targetTab != _activeTab)
                            {
                                e.Handled = true;
                                ShowUnsavedModal(() => SwitchToTab(targetTab));
                                return;
                            }
                            break;
                        }
                        if (dep is Button closeBtn && closeBtn.Tag is BrowserTab closeTab)
                        {
                            if (closeTab == _activeTab)
                            {
                                e.Handled = true;
                                ShowUnsavedModal(() => CloseTab(closeTab));
                                return;
                            }
                        }

                        dep = GetParentSafe(dep);
                    }
                }
            };
        }

        PreviewKeyDown += (s, e) =>
        {
            if (_activeTab?.Url == "nova://settings" && _isSettingsDirty)
            {
                if (e.Key == Key.W && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    e.Handled = true;
                    ShowUnsavedModal(() =>
                    {
                        if (_activeTab != null) CloseTab(_activeTab);
                    });
                }
            }
        };
    }

    private StackPanel BuildGeneralPanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Visible };

        var h1 = new TextBlock
        {
            Text = "Общие настройки",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 16),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        h1.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        panel.Children.Add(h1);

        var cardLang = CreateCard("Язык интерфейса", "Выберите язык отображения браузера, меню и встроенных страниц.");
        var stackLang = (StackPanel)cardLang.Child;

        _sLanguage = new ComboBox { Width = 260, HorizontalAlignment = HorizontalAlignment.Left };
        _sLanguage.Items.Add(new ComboBoxItem { Content = "Русский (Россия)", Tag = "ru-RU" });
        _sLanguage.Items.Add(new ComboBoxItem { Content = "English (United States)", Tag = "en-US" });
        _sLanguage.Items.Add(new ComboBoxItem { Content = "Deutsch (Deutschland)", Tag = "de-DE" });
        _sLanguage.Items.Add(new ComboBoxItem { Content = "Français (France)", Tag = "fr-FR" });
        _sLanguage.Items.Add(new ComboBoxItem { Content = "Español (España)", Tag = "es-ES" });
        _sLanguage.Items.Add(new ComboBoxItem { Content = "中文 (简体)", Tag = "zh-CN" });
        _sLanguage.SelectionChanged += (_, _) => OnInputChanged();

        stackLang.Children.Add(CreateRowWithHelp("Язык браузера", _sLanguage,
            "Определяет язык для всех элементов интерфейса, кнопок, диалогов, системных уведомлений и встроенных подсказок Nova."));
        panel.Children.Add(cardLang);

        var cardSearch = CreateCard("Поисковая система", "Выберите основной поисковик для поисковых запросов.");
        var stackSearch = (StackPanel)cardSearch.Child;

        _sSearchEngine = new ComboBox { Width = 260, HorizontalAlignment = HorizontalAlignment.Left };
        _sSearchEngine.Items.Add(new ComboBoxItem { Content = "Nova" });
        _sSearchEngine.Items.Add(new ComboBoxItem { Content = "Google" });
        _sSearchEngine.Items.Add(new ComboBoxItem { Content = "Яндекс" });
        _sSearchEngine.Items.Add(new ComboBoxItem { Content = "DuckDuckGo" });
        _sSearchEngine.Items.Add(new ComboBoxItem { Content = "Bing" });
        _sSearchEngine.Items.Add(new ComboBoxItem { Content = "Brave Search" });
        _sSearchEngine.Items.Add(new ComboBoxItem { Content = "Ecosia" });
        _sSearchEngine.SelectionChanged += (_, _) => OnInputChanged();

        stackSearch.Children.Add(CreateRowWithHelp("Поисковик по умолчанию", _sSearchEngine,
            "Используется при вводе текста или вопроса в адресную строку без префикса протокола."));
        panel.Children.Add(cardSearch);

        var cardHome = CreateCard("Стартовая страница", "Страница, которая загружается при открытии новой вкладки или клике на Домой.");
        var stackHome = (StackPanel)cardHome.Child;

        var homeRow = new Grid();
        homeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        homeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _sHomepage = new TextBox { Margin = new Thickness(0, 0, 8, 0), Height = 34, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_sHomepage);
        _sHomepage.TextChanged += (_, _) => OnInputChanged();
        Grid.SetColumn(_sHomepage, 0);
        homeRow.Children.Add(_sHomepage);

        var btnNovaHome = new Button { Content = "Nova Home", Height = 34, Padding = new Thickness(14, 0, 14, 0), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
        StyleDarkButton(btnNovaHome);
        btnNovaHome.Click += (_, _) => { _sHomepage.Text = ""; OnInputChanged(); };
        Grid.SetColumn(btnNovaHome, 1);
        homeRow.Children.Add(btnNovaHome);

        stackHome.Children.Add(CreateRowWithHelp("Адрес домашней страницы", homeRow,
            "Оставьте пустым для отображения фирменного портала Nova Home с интеллектуальным поиском и виджетами, либо укажите свой веб-адрес."));
        panel.Children.Add(cardHome);

        var cardZoom = CreateCard("Масштабирование страниц", "Установите комфортный базовый размер текста и элементов.");
        var stackZoom = (StackPanel)cardZoom.Child;

        _sZoom = new ComboBox { Width = 140, HorizontalAlignment = HorizontalAlignment.Left };
        _sZoom.Items.Add(new ComboBoxItem { Content = "75%", Tag = 75 });
        _sZoom.Items.Add(new ComboBoxItem { Content = "90%", Tag = 90 });
        _sZoom.Items.Add(new ComboBoxItem { Content = "100%", Tag = 100 });
        _sZoom.Items.Add(new ComboBoxItem { Content = "110%", Tag = 110 });
        _sZoom.Items.Add(new ComboBoxItem { Content = "125%", Tag = 125 });
        _sZoom.Items.Add(new ComboBoxItem { Content = "150%", Tag = 150 });
        _sZoom.SelectionChanged += (_, _) => OnInputChanged();

        stackZoom.Children.Add(CreateRowWithHelp("Масштаб по умолчанию", _sZoom,
            "Применяется ко всем открываемым сайтам при их начальной загрузке."));
        panel.Children.Add(cardZoom);

        var cardTabs = CreateCard("Запуск браузера и сохранение вкладок", "Сохранение открытых страниц при выходе и поведение вкладок.");
        var stackTabs = (StackPanel)cardTabs.Child;

        _sRestoreSession = new CheckBox { Content = "Сохранять вкладки после выхода и восстанавливать их при запуске", Margin = new Thickness(0, 2, 0, 8) };
        _sRestoreSession.Checked += (_, _) => OnInputChanged();
        _sRestoreSession.Unchecked += (_, _) => OnInputChanged();
        stackTabs.Children.Add(CreateCheckBoxWithHelp(_sRestoreSession,
            "При закрытии браузера все открытые вкладки автоматически сохраняются и восстанавливаются при следующем запуске."));

        _sNewTab = new CheckBox { Content = "Открывать внешние ссылки в новой вкладке", Margin = new Thickness(0, 0, 0, 8) };
        _sNewTab.Checked += (_, _) => OnInputChanged();
        _sNewTab.Unchecked += (_, _) => OnInputChanged();
        stackTabs.Children.Add(CreateCheckBoxWithHelp(_sNewTab,
            "При клике на ссылки из закладок или внешних источников открывает отдельную новую вкладку, сохраняя текущую страницу."));

        _sConfirmCloseTabs = new CheckBox { Content = "Предупреждать при закрытии нескольких вкладок", Margin = new Thickness(0, 0, 0, 2) };
        _sConfirmCloseTabs.Checked += (_, _) => OnInputChanged();
        _sConfirmCloseTabs.Unchecked += (_, _) => OnInputChanged();
        stackTabs.Children.Add(CreateCheckBoxWithHelp(_sConfirmCloseTabs,
            "Запрашивает подтверждение при закрытии окна браузера с несколькими открытыми вкладками."));
        panel.Children.Add(cardTabs);

        var cardDownloads = CreateCard("Папка загрузок", "Каталог для сохранения загружаемых файлов.");
        var stackDownloads = (StackPanel)cardDownloads.Child;

        var downRow = new Grid();
        downRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        downRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _sDownloadFolder = new TextBox { IsReadOnly = true, Margin = new Thickness(0, 0, 8, 0), Height = 34, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_sDownloadFolder);
        Grid.SetColumn(_sDownloadFolder, 0);
        downRow.Children.Add(_sDownloadFolder);

        var btnBrowseDown = new Button { Content = "Обзор...", Height = 34, Padding = new Thickness(16, 0, 16, 0), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
        StyleDarkButton(btnBrowseDown);
        btnBrowseDown.Click += (_, _) =>
        {
            var f = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var initial = Directory.Exists(_sDownloadFolder.Text) ? _sDownloadFolder.Text : System.IO.Path.Combine(f, "Downloads");
            _sDownloadFolder.Text = initial;
            OnInputChanged();
        };
        Grid.SetColumn(btnBrowseDown, 1);
        downRow.Children.Add(btnBrowseDown);

        stackDownloads.Children.Add(CreateRowWithHelp("Путь к загрузкам", downRow,
            "Папка на диске, в которую по умолчанию помещаются скачанные изображения, документы и программы."));
        panel.Children.Add(cardDownloads);

        var cardSources = CreateCard("Источники поиска Nova", "С каких сервисов Nova агрегирует результаты при поиске.");
        var stackSources = (StackPanel)cardSources.Child;

        var sourcesGrid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
        sourcesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sourcesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var col1 = new StackPanel();
        var col2 = new StackPanel();

        _sSourceGoogle = new CheckBox { Content = "Google (веб-сайты)", Margin = new Thickness(0, 0, 0, 8) };
        _sSourceYandex = new CheckBox { Content = "Яндекс (русскоязычные сайты)", Margin = new Thickness(0, 0, 0, 8) };
        _sSourceWiki = new CheckBox { Content = "Википедия (энциклопедия)", Margin = new Thickness(0, 0, 0, 8) };
        _sSourceHabr = new CheckBox { Content = "Habr (IT-статьи)", Margin = new Thickness(0, 0, 0, 8) };

        _sSourceGitHub = new CheckBox { Content = "GitHub (код и проекты)", Margin = new Thickness(0, 0, 0, 8) };
        _sSourceKinopoisk = new CheckBox { Content = "Кинопоиск (фильмы и сериалы)", Margin = new Thickness(0, 0, 0, 8) };
        _sSourceYouTube = new CheckBox { Content = "YouTube (видеоматериалы)", Margin = new Thickness(0, 0, 0, 8) };
        _sSourceVK = new CheckBox { Content = "ВКонтакте (сообщества)", Margin = new Thickness(0, 0, 0, 8) };

        foreach (var chk in new[] { _sSourceGoogle, _sSourceYandex, _sSourceWiki, _sSourceHabr, _sSourceGitHub, _sSourceKinopoisk, _sSourceYouTube, _sSourceVK })
        {
            chk.Checked += (_, _) => OnInputChanged();
            chk.Unchecked += (_, _) => OnInputChanged();
        }

        col1.Children.Add(_sSourceGoogle);
        col1.Children.Add(_sSourceYandex);
        col1.Children.Add(_sSourceWiki);
        col1.Children.Add(_sSourceHabr);

        col2.Children.Add(_sSourceGitHub);
        col2.Children.Add(_sSourceKinopoisk);
        col2.Children.Add(_sSourceYouTube);
        col2.Children.Add(_sSourceVK);

        Grid.SetColumn(col1, 0);
        Grid.SetColumn(col2, 1);
        sourcesGrid.Children.Add(col1);
        sourcesGrid.Children.Add(col2);
        stackSources.Children.Add(sourcesGrid);

        var filterLabel = new TextBlock
        {
            Text = "Ограничить поиск доменами (site:)",
            FontSize = 13.5,
            FontWeight = FontWeights.Medium,
            Margin = new Thickness(0, 10, 0, 6),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        filterLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        stackSources.Children.Add(filterLabel);

        _sSiteFilter = new TextBox
        {
            Height = 36,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
            Padding = new Thickness(12, 0, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        StyleDarkTextBox(_sSiteFilter);
        _sSiteFilter.TextChanged += (_, _) => OnInputChanged();
        stackSources.Children.Add(_sSiteFilter);



        panel.Children.Add(cardSources);

        return panel;
    }

    private StackPanel BuildAccountsPanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Collapsed };

        var h1 = new TextBlock
        {
            Text = "Аккаунты и привязки",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        h1.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        panel.Children.Add(h1);

        var sub = new TextBlock
        {
            Text = "Управление привязанными учетными записями и локальным менеджером паролей браузера.",
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 16),
            Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(150, 150, 150))
        };
        sub.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        panel.Children.Add(sub);

        var cardAcc = CreateCard("Привязанные аккаунты", "Быстрый вход и интеграция с экосистемами Google, Яндекс и другими.");
        var stackAcc = (StackPanel)cardAcc.Child;

        var headerAcc = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        headerAcc.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerAcc.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headerBtns = new StackPanel { Orientation = Orientation.Horizontal };
        var btnOpenModal = new Button
        {
            Content = "📋 Окно почт и паролей",
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnOpenModal);
        btnOpenModal.Click += (_, _) => ShowAccountsModal();
        headerBtns.Children.Add(btnOpenModal);

        var btnAddAcc = new Button
        {
            Content = "+ Привязать аккаунт",
            Padding = new Thickness(14, 7, 14, 7),
            Cursor = Cursors.Hand,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnAddAcc);
        btnAddAcc.Click += (_, _) =>
        {
            if (_addAccountForm != null)
                _addAccountForm.Visibility = _addAccountForm.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        };
        headerBtns.Children.Add(btnAddAcc);

        Grid.SetColumn(headerBtns, 1);
        headerAcc.Children.Add(headerBtns);
        stackAcc.Children.Add(headerAcc);

        _addAccountForm = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 0, 0, 14)
        };
        var addFormBorder = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(220, 20, 23, 33)),
            BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123))
        };
        addFormBorder.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");

        var addFormStack = new StackPanel();

        addFormStack.Children.Add(new TextBlock { Text = "Сервис:", FontSize = 11, Margin = new Thickness(0, 0, 0, 3), Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White) });
        _newAccService = new ComboBox { Margin = new Thickness(0, 0, 0, 8), Height = 34 };
        StyleDarkComboBox(_newAccService);
        _newAccService.Items.Add(new ComboBoxItem { Content = "Google", IsSelected = true });
        _newAccService.Items.Add(new ComboBoxItem { Content = "Яндекс" });
        _newAccService.Items.Add(new ComboBoxItem { Content = "ВКонтакте" });
        _newAccService.Items.Add(new ComboBoxItem { Content = "Mail.ru" });
        _newAccService.Items.Add(new ComboBoxItem { Content = "GitHub" });
        _newAccService.Items.Add(new ComboBoxItem { Content = "Другой" });
        addFormStack.Children.Add(_newAccService);

        _newAccEmail = new TextBox { Margin = new Thickness(0, 0, 0, 8), Height = 34, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_newAccEmail);
        addFormStack.Children.Add(new TextBlock { Text = "E-mail:", FontSize = 11, Margin = new Thickness(0, 0, 0, 3), Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White) });
        addFormStack.Children.Add(_newAccEmail);

        _newAccPassword = new TextBox { Margin = new Thickness(0, 0, 0, 12), Height = 34, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_newAccPassword);
        addFormStack.Children.Add(new TextBlock { Text = "Пароль:", FontSize = 11, Margin = new Thickness(0, 0, 0, 3), Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White) });
        addFormStack.Children.Add(_newAccPassword);

        var btnSubmitAcc = new Button
        {
            Content = "Сохранить аккаунт",
            Padding = new Thickness(16, 6, 16, 6),
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right,
            FontWeight = FontWeights.Bold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnSubmitAcc, true);
        btnSubmitAcc.Click += (_, _) =>
        {
            var srv = (_newAccService.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Google";
            var em = _newAccEmail.Text.Trim();
            var pw = _newAccPassword.Text.Trim();
            if (string.IsNullOrEmpty(em)) return;

            _settings.SavedAccounts ??= new();
            _settings.SavedAccounts.Add(new SavedAccountItem
            {
                Service = srv,
                Login = em,
                Email = em,
                Password = pw,
                AddedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            });

            if (!string.IsNullOrEmpty(pw))
            {
                _settings.SavedPasswords ??= new();
                _settings.SavedPasswords.Add(new SavedPasswordItem
                {
                    Site = srv,
                    Login = em,
                    Password = pw,
                    Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                });
            }

            _newAccEmail.Text = "";
            _newAccPassword.Text = "";
            _addAccountForm.Visibility = Visibility.Collapsed;
            OnInputChanged();
            RefreshAccountsList();
            RefreshPasswordsList();
        };
        addFormStack.Children.Add(btnSubmitAcc);

        addFormBorder.Child = addFormStack;
        _addAccountForm.Children.Add(addFormBorder);
        stackAcc.Children.Add(_addAccountForm);

        _accountsListContainer = new StackPanel();
        stackAcc.Children.Add(_accountsListContainer);
        panel.Children.Add(cardAcc);

        var cardPass = CreateCard("Сохранённые пароли", "Локальное безопасное хранилище логинов и паролей сайтов.");
        var stackPass = (StackPanel)cardPass.Child;

        _sPasswordAutofill = new CheckBox { Content = "Предлагать сохранение паролей при входе на сайтах", Margin = new Thickness(0, 0, 0, 8) };
        _sPasswordAutofill.Checked += (_, _) => OnInputChanged();
        _sPasswordAutofill.Unchecked += (_, _) => OnInputChanged();
        stackPass.Children.Add(CreateCheckBoxWithHelp(_sPasswordAutofill,
            "Браузер отслеживает ввод логина и пароля на формах авторизации и предлагает сохранить их в локальное безопасное хранилище."));

        _sUniversalPassAutofill = new CheckBox { Content = "Универсальное автозаполнение паролей на всех сайтах", Margin = new Thickness(0, 0, 0, 12) };
        _sUniversalPassAutofill.Checked += (_, _) => OnInputChanged();
        _sUniversalPassAutofill.Unchecked += (_, _) => OnInputChanged();
        stackPass.Children.Add(CreateCheckBoxWithHelp(_sUniversalPassAutofill,
            "Все сохранённые логины и пароли будут предлагаться для автозаполнения в полях ввода на любых сайтах, а не только на исходном домене."));

        var headerPass = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        headerPass.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerPass.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var btnAddPass = new Button
        {
            Content = "+ Добавить пароль",
            Padding = new Thickness(14, 7, 14, 7),
            Cursor = Cursors.Hand,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnAddPass, true);
        btnAddPass.Click += (_, _) =>
        {
            if (_addPasswordForm != null)
                _addPasswordForm.Visibility = _addPasswordForm.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        };
        Grid.SetColumn(btnAddPass, 1);
        headerPass.Children.Add(btnAddPass);
        stackPass.Children.Add(headerPass);

        _addPasswordForm = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 0, 0, 14)
        };
        var addPassBorder = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(220, 20, 23, 33)),
            BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123))
        };
        addPassBorder.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");

        var addPassStack = new StackPanel();

        _newPassSite = new TextBox { Text = "*", Margin = new Thickness(0, 0, 0, 8), Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        addPassStack.Children.Add(new TextBlock { Text = "Веб-сайт или сервис:", FontSize = 11, Margin = new Thickness(0, 0, 0, 2), Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White) });
        addPassStack.Children.Add(_newPassSite);

        _newPassLogin = new TextBox { Margin = new Thickness(0, 0, 0, 8), Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        addPassStack.Children.Add(new TextBlock { Text = "Логин / E-mail:", FontSize = 11, Margin = new Thickness(0, 0, 0, 2), Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White) });
        addPassStack.Children.Add(_newPassLogin);

        _newPassValue = new TextBox { Margin = new Thickness(0, 0, 0, 12), Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        addPassStack.Children.Add(new TextBlock { Text = "Пароль:", FontSize = 11, Margin = new Thickness(0, 0, 0, 2), Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White) });
        addPassStack.Children.Add(_newPassValue);

        var btnSubmitPass = new Button
        {
            Content = "Сохранить пароль",
            Padding = new Thickness(16, 6, 16, 6),
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right,
            FontWeight = FontWeights.Bold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnSubmitPass, true);
        btnSubmitPass.Click += (_, _) =>
        {
            var st = _newPassSite.Text.Trim();
            var lg = _newPassLogin.Text.Trim();
            var pw = _newPassValue.Text;
            if (string.IsNullOrEmpty(st) || string.IsNullOrEmpty(lg)) return;

            _settings.SavedPasswords ??= new();
            _settings.SavedPasswords.Add(new SavedPasswordItem
            {
                Site = st,
                Login = lg,
                Password = pw,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            });

            _newPassSite.Text = "";
            _newPassLogin.Text = "";
            _newPassValue.Text = "";
            _addPasswordForm.Visibility = Visibility.Collapsed;
            OnInputChanged();
            RefreshPasswordsList();
        };
        addPassStack.Children.Add(btnSubmitPass);

        addPassBorder.Child = addPassStack;
        _addPasswordForm.Children.Add(addPassBorder);
        stackPass.Children.Add(_addPasswordForm);

        _passwordsListContainer = new StackPanel();
        stackPass.Children.Add(_passwordsListContainer);
        panel.Children.Add(cardPass);

        return panel;
    }

    private Border BuildAccountsModal()
    {
        var overlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            Visibility = Visibility.Collapsed
        };

        var card = new Border
        {
            Width = 560,
            MaxHeight = 520,
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = GetThemeBrush("BgCardBrush", Color.FromRgb(24, 26, 32)),
            BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
            BorderThickness = new Thickness(1)
        };
        try { card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.65, Color = Colors.Black }; } catch { }

        var stack = new StackPanel();

        var topRow = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "Сохранённые учётные записи и пароли",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        Grid.SetColumn(title, 0);
        topRow.Children.Add(title);

        var btnClose = new Button
        {
            Content = "✕",
            FontSize = 14,
            Width = 28,
            Height = 28,
            Cursor = Cursors.Hand,
            Tag = "SettingsInternal"
        };
        btnClose.Click += (_, _) => overlay.Visibility = Visibility.Collapsed;
        Grid.SetColumn(btnClose, 1);
        topRow.Children.Add(btnClose);
        stack.Children.Add(topRow);

        _accountsModalSearch = new TextBox
        {
            Margin = new Thickness(0, 0, 0, 12),
            Height = 32,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _accountsModalSearch.TextChanged += (_, _) => PopulateAccountsModalList(_accountsModalSearch.Text.Trim().ToLowerInvariant());
        stack.Children.Add(_accountsModalSearch);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 360
        };
        _accountsModalList = new StackPanel();
        scroll.Content = _accountsModalList;
        stack.Children.Add(scroll);

        card.Child = stack;
        overlay.Child = card;
        return overlay;
    }

    private void ShowAccountsModal()
    {
        if (_accountsModal == null) return;
        if (_accountsModalSearch != null) _accountsModalSearch.Text = "";
        PopulateAccountsModalList("");
        _accountsModal.Visibility = Visibility.Visible;
    }

    private void PopulateAccountsModalList(string filter)
    {
        if (_accountsModalList == null) return;
        _accountsModalList.Children.Clear();

        var accounts = (_settings.SavedAccounts ?? new()).ToList();
        var passwords = (_settings.SavedPasswords ?? new()).ToList();

        if (accounts.Count == 0 && passwords.Count == 0)
        {
            _accountsModalList.Children.Add(new TextBlock
            {
                Text = "Нет сохранённых учётных записей и паролей.",
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 10),
                Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(150, 150, 150))
            });
            return;
        }

        foreach (var acc in accounts)
        {
            var match = string.IsNullOrEmpty(filter)
                     || acc.Service.ToLowerInvariant().Contains(filter)
                     || acc.Login.ToLowerInvariant().Contains(filter)
                     || acc.Email.ToLowerInvariant().Contains(filter);
            if (!match) continue;

            var card = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(40, 44, 52))
            };

            var sp = new StackPanel();
            var head = new TextBlock
            {
                Text = acc.Service,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            sp.Children.Add(head);

            var emRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            emRow.Children.Add(new TextBlock { Text = "E-Mail: ", FontSize = 12, Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(160, 160, 160)) });
            var emVal = new TextBlock { Text = string.IsNullOrEmpty(acc.Email) ? acc.Login : acc.Email, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White), Margin = new Thickness(0, 0, 8, 0) };
            emRow.Children.Add(emVal);

            var btnCopyEm = new Button { Content = "Копировать", FontSize = 10, Padding = new Thickness(6, 2, 6, 2), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
            var emStr = emVal.Text;
            btnCopyEm.Click += (_, _) => { try { Clipboard.SetText(emStr); ShowToast("E-mail скопирован"); } catch { } };
            emRow.Children.Add(btnCopyEm);
            sp.Children.Add(emRow);

            var pwRow = new StackPanel { Orientation = Orientation.Horizontal };
            pwRow.Children.Add(new TextBlock { Text = "Пароль: ", FontSize = 12, Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(160, 160, 160)) });
            var pwVal = new TextBlock { Text = string.IsNullOrEmpty(acc.Password) ? "(не сохранён)" : "••••••••", FontSize = 12, Margin = new Thickness(0, 0, 8, 0), Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White) };
            pwRow.Children.Add(pwVal);

            if (!string.IsNullOrEmpty(acc.Password))
            {
                var curPw = acc.Password;
                var btnTgl = new Button { Content = "Показать", FontSize = 10, Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 6, 0), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
                bool isSh = false;
                btnTgl.Click += (_, _) =>
                {
                    isSh = !isSh;
                    pwVal.Text = isSh ? curPw : "••••••••";
                    btnTgl.Content = isSh ? "Скрыть" : "Показать";
                };
                pwRow.Children.Add(btnTgl);

                var btnCopyPw = new Button { Content = "Копировать", FontSize = 10, Padding = new Thickness(6, 2, 6, 2), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
                btnCopyPw.Click += (_, _) => { try { Clipboard.SetText(curPw); ShowToast("Пароль скопирован"); } catch { } };
                pwRow.Children.Add(btnCopyPw);
            }
            sp.Children.Add(pwRow);

            card.Child = sp;
            _accountsModalList.Children.Add(card);
        }
    }

    private void RefreshAccountsList()
    {
        if (_accountsListContainer == null) return;
        _accountsListContainer.Children.Clear();

        if (_settings.SavedAccounts == null || _settings.SavedAccounts.Count == 0)
        {
            var emptyTb = new TextBlock
            {
                Text = "Нет привязанных учетных записей. Нажмите '+ Привязать аккаунт' для добавления.",
                FontSize = 12,
                Margin = new Thickness(4, 6, 4, 6),
                Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(150, 150, 150))
            };
            emptyTb.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
            _accountsListContainer.Children.Add(emptyTb);
            return;
        }

        foreach (var acc in _settings.SavedAccounts.ToList())
        {
            var row = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 10),
                Background = GetThemeBrush("BgSurfaceBrush", Color.FromRgb(28, 30, 36)),
                BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(40, 44, 52)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel();
            var title = new TextBlock
            {
                Text = acc.Service,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            info.Children.Add(title);

            var emRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            emRow.Children.Add(new TextBlock { Text = "E-Mail: ", FontSize = 12, Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(160, 160, 160)) });
            var emVal = new TextBlock { Text = string.IsNullOrEmpty(acc.Email) ? acc.Login : acc.Email, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White), Margin = new Thickness(0, 0, 8, 0) };
            emRow.Children.Add(emVal);

            var btnCopyEm = new Button { Content = "Копировать", FontSize = 10, Padding = new Thickness(6, 2, 6, 2), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
            var emStr = emVal.Text;
            btnCopyEm.Click += (_, _) => { try { Clipboard.SetText(emStr); ShowToast("E-mail скопирован"); } catch { } };
            emRow.Children.Add(btnCopyEm);
            info.Children.Add(emRow);

            var pwRow = new StackPanel { Orientation = Orientation.Horizontal };
            pwRow.Children.Add(new TextBlock { Text = "Пароль: ", FontSize = 12, Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(160, 160, 160)) });
            var pwVal = new TextBlock { Text = string.IsNullOrEmpty(acc.Password) ? "(не сохранён)" : "••••••••", FontSize = 12, Margin = new Thickness(0, 0, 8, 0), Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White) };
            pwRow.Children.Add(pwVal);

            if (!string.IsNullOrEmpty(acc.Password))
            {
                var curPw = acc.Password;
                var btnTgl = new Button { Content = "Показать", FontSize = 10, Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 6, 0), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
                bool isSh = false;
                btnTgl.Click += (_, _) =>
                {
                    isSh = !isSh;
                    pwVal.Text = isSh ? curPw : "••••••••";
                    btnTgl.Content = isSh ? "Скрыть" : "Показать";
                };
                pwRow.Children.Add(btnTgl);

                var btnCopyPw = new Button { Content = "Копировать", FontSize = 10, Padding = new Thickness(6, 2, 6, 2), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
                btnCopyPw.Click += (_, _) => { try { Clipboard.SetText(curPw); ShowToast("Пароль скопирован"); } catch { } };
                pwRow.Children.Add(btnCopyPw);
            }
            info.Children.Add(pwRow);

            Grid.SetColumn(info, 0);
            grid.Children.Add(info);

            var curAcc = acc;
            var btnDel = new Button
            {
                Content = "Удалить",
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = "SettingsInternal"
            };
            StyleDarkButton(btnDel, false);
            btnDel.Click += (_, _) =>
            {
                _settings.SavedAccounts.Remove(curAcc);
                OnInputChanged();
                RefreshAccountsList();
            };
            Grid.SetColumn(btnDel, 1);
            grid.Children.Add(btnDel);

            row.Child = grid;
            _accountsListContainer.Children.Add(row);
        }
    }

    private void RefreshPasswordsList()
    {
        if (_passwordsListContainer == null) return;
        _passwordsListContainer.Children.Clear();

        if (_settings.SavedPasswords == null || _settings.SavedPasswords.Count == 0)
        {
            var emptyTb = new TextBlock
            {
                Text = "Список сохраненных паролей пуст. Нажмите '+ Добавить пароль' для добавления.",
                FontSize = 12,
                Margin = new Thickness(4, 6, 4, 6),
                Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(150, 150, 150))
            };
            emptyTb.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
            _passwordsListContainer.Children.Add(emptyTb);
            return;
        }

        foreach (var p in _settings.SavedPasswords.ToList())
        {
            var row = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 10),
                Background = GetThemeBrush("BgSurfaceBrush", Color.FromRgb(28, 30, 36)),
                BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(40, 44, 52)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel();
            var title = new TextBlock
            {
                Text = p.Site,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            info.Children.Add(title);

            var emRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            emRow.Children.Add(new TextBlock { Text = "Логин / E-Mail: ", FontSize = 12, Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(160, 160, 160)) });
            var emVal = new TextBlock { Text = p.Login, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White), Margin = new Thickness(0, 0, 8, 0) };
            emRow.Children.Add(emVal);

            var btnCopyEm = new Button { Content = "Копировать", FontSize = 10, Padding = new Thickness(8, 3, 8, 3), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
            StyleDarkButton(btnCopyEm, false);
            var emStr = emVal.Text;
            btnCopyEm.Click += (_, _) => { try { Clipboard.SetText(emStr); ShowToast("Логин скопирован"); } catch { } };
            emRow.Children.Add(btnCopyEm);
            info.Children.Add(emRow);

            var pwRow = new StackPanel { Orientation = Orientation.Horizontal };
            pwRow.Children.Add(new TextBlock { Text = "Пароль: ", FontSize = 12, Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(160, 160, 160)) });
            var pwVal = new TextBlock { Text = "••••••••", FontSize = 12, Margin = new Thickness(0, 0, 8, 0), Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White) };
            pwRow.Children.Add(pwVal);

            var curPw = p.Password;
            var btnTgl = new Button { Content = "Показать", FontSize = 10, Padding = new Thickness(8, 3, 8, 3), Margin = new Thickness(0, 0, 6, 0), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
            StyleDarkButton(btnTgl, false);
            bool isSh = false;
            btnTgl.Click += (_, _) =>
            {
                isSh = !isSh;
                pwVal.Text = isSh ? curPw : "••••••••";
                btnTgl.Content = isSh ? "Скрыть" : "Показать";
            };
            pwRow.Children.Add(btnTgl);

            var btnCopyPw = new Button { Content = "Копировать", FontSize = 10, Padding = new Thickness(8, 3, 8, 3), Cursor = Cursors.Hand, Tag = "SettingsInternal" };
            StyleDarkButton(btnCopyPw, false);
            btnCopyPw.Click += (_, _) => { try { Clipboard.SetText(curPw); ShowToast("Пароль скопирован"); } catch { } };
            pwRow.Children.Add(btnCopyPw);
            info.Children.Add(pwRow);

            Grid.SetColumn(info, 0);
            grid.Children.Add(info);

            var curPass = p;
            var btnDel = new Button
            {
                Content = "Удалить",
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = "SettingsInternal"
            };
            StyleDarkButton(btnDel, false);
            btnDel.Click += (_, _) =>
            {
                _settings.SavedPasswords.Remove(curPass);
                OnInputChanged();
                RefreshPasswordsList();
            };
            Grid.SetColumn(btnDel, 1);
            grid.Children.Add(btnDel);

            row.Child = grid;
            _passwordsListContainer.Children.Add(row);
        }
    }

    private StackPanel BuildAppearancePanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Collapsed };

        var h1 = new TextBlock
        {
            Text = "Внешний вид и темы",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 16),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        h1.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        panel.Children.Add(h1);

        var cardTheme = CreateCard("Тема оформления", "Цветовая схема оконных рамок и элементов управления.");
        var stackTheme = (StackPanel)cardTheme.Child;

        _sTheme = new ComboBox { Width = 260, HorizontalAlignment = HorizontalAlignment.Left };
        _sTheme.Items.Add(new ComboBoxItem { Content = "Nova Dark (кибер-тёмная)", Tag = "Dark" });
        _sTheme.Items.Add(new ComboBoxItem { Content = "Cyberpunk Cyan", Tag = "Cyberpunk" });
        _sTheme.Items.Add(new ComboBoxItem { Content = "Nord", Tag = "Nord" });
        _sTheme.Items.Add(new ComboBoxItem { Content = "Dracula", Tag = "Dracula" });
        _sTheme.Items.Add(new ComboBoxItem { Content = "Deep Violet / Mocha", Tag = "Mocha" });
        _sTheme.Items.Add(new ComboBoxItem { Content = "Light Mode (светлая)", Tag = "Light" });
        _sTheme.SelectionChanged += (_, _) =>
        {
            if (!_suppressDirtyEvents && _sTheme.SelectedItem is ComboBoxItem item && item.Tag is string th)
            {
                _settings.Theme = th;
                ApplyTheme(th);
                var curAcc = _sAccentColor?.Text?.Trim();
                if (string.IsNullOrEmpty(curAcc)) curAcc = _settings.AccentColor;
                ApplyAccentColor(curAcc);
                PushThemeSettingsToAllTabs(curAcc, _sHtmlBgColor?.Text?.Trim());
                UpdateColorChipsVisuals();
                OnInputChanged();
            }
        };

        stackTheme.Children.Add(CreateRowWithHelp("Тема браузера", _sTheme,
            "Изменяет общую цветовую гамму интерфейса, цвет панелей, всплывающих окон и контекстных меню."));
        panel.Children.Add(cardTheme);

        var cardAccent = CreateCard("Акцентный цвет (WPF + HTML)", "Цвет подсветки кнопок, свечения и элементов поиска.");
        var stackAccent = (StackPanel)cardAccent.Child;

        var accentPickerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 10) };
        _accentPreviewSwatch = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 10, 0),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
            Background = ParseHexBrush(_sAccentColor?.Text ?? "#BEF47B", Color.FromRgb(190, 244, 123))
        };
        accentPickerRow.Children.Add(_accentPreviewSwatch);

        var btnPickAccent = new Button
        {
            Content = "🎨 Выбрать цвет в RGB (Палитра)...",
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.SemiBold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnPickAccent, true);
        btnPickAccent.Click += (_, _) =>
        {
            var cur = _sAccentColor?.Text?.Trim() ?? "#BEF47B";
            OpenRgbColorPicker("Акцентный цвет интерфейса", cur, hex =>
            {
                if (_sAccentColor != null)
                {
                    _sAccentColor.Text = hex;
                    UpdateColorChipsVisuals();
                    _settings.AccentColor = hex;
                    _settings.HtmlAccentColor = hex;
                    ApplyAccentColor(hex);
                    PushThemeSettingsToAllTabs(hex, _sHtmlBgColor?.Text?.Trim());
                    OnInputChanged();
                }
            }, liveHex =>
            {
                if (_sAccentColor != null)
                {
                    _sAccentColor.Text = liveHex;
                    UpdateColorChipsVisuals();
                    _settings.AccentColor = liveHex;
                    _settings.HtmlAccentColor = liveHex;
                    ApplyAccentColor(liveHex);
                    PushThemeSettingsToAllTabs(liveHex, _sHtmlBgColor?.Text?.Trim());
                }
            });
        };
        accentPickerRow.Children.Add(btnPickAccent);
        stackAccent.Children.Add(accentPickerRow);

        _sAccentColor = new TextBox { Width = 140, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_sAccentColor);
        _sAccentColor.TextChanged += (_, _) =>
        {
            UpdateColorChipsVisuals();
            var hex = _sAccentColor.Text?.Trim() ?? "";
            if (IsValidHex(hex))
            {
                _settings.AccentColor = hex;
                _settings.HtmlAccentColor = hex;
                ApplyAccentColor(hex);
                PushThemeSettingsToAllTabs(hex, _sHtmlBgColor?.Text?.Trim());
            }
            OnInputChanged();
        };
        stackAccent.Children.Add(CreateRowWithHelp("HEX-код акцентного цвета", _sAccentColor,
            "Цвет неонового свечения, полосы активной вкладки, кнопок и акцентных элементов на страницах Nova Search."));
        panel.Children.Add(cardAccent);

        var cardBg = CreateCard("Фон страницы Nova Home & Search", "Фоновый оттенок стартового экрана.");
        var stackBg = (StackPanel)cardBg.Child;

        var bgPickerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 10) };
        _bgPreviewSwatch = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 10, 0),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
            Background = ParseHexBrush(_sHtmlBgColor?.Text ?? "#07090d", Color.FromRgb(7, 9, 13))
        };
        bgPickerRow.Children.Add(_bgPreviewSwatch);

        var btnPickBg = new Button
        {
            Content = "🎨 Выбрать цвет в RGB (Палитра)...",
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.SemiBold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnPickBg, true);
        btnPickBg.Click += (_, _) =>
        {
            var cur = _sHtmlBgColor?.Text?.Trim() ?? "#07090d";
            OpenRgbColorPicker("Фон страницы Nova Home", cur, hex =>
            {
                if (_sHtmlBgColor != null)
                {
                    _sHtmlBgColor.Text = hex;
                    UpdateColorChipsVisuals();
                    _settings.HtmlBackgroundColor = hex;
                    PushThemeSettingsToAllTabs(_sAccentColor?.Text?.Trim(), hex);
                    OnInputChanged();
                }
            }, liveHex =>
            {
                if (_sHtmlBgColor != null)
                {
                    _sHtmlBgColor.Text = liveHex;
                    UpdateColorChipsVisuals();
                    _settings.HtmlBackgroundColor = liveHex;
                    PushThemeSettingsToAllTabs(_sAccentColor?.Text?.Trim(), liveHex);
                }
            });
        };
        bgPickerRow.Children.Add(btnPickBg);
        stackBg.Children.Add(bgPickerRow);

        _sHtmlBgColor = new TextBox { Width = 140, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_sHtmlBgColor);
        _sHtmlBgColor.TextChanged += (_, _) =>
        {
            UpdateColorChipsVisuals();
            var hex = _sHtmlBgColor.Text?.Trim() ?? "";
            if (IsValidHex(hex))
            {
                _settings.HtmlBackgroundColor = hex;
                PushThemeSettingsToAllTabs(_sAccentColor?.Text?.Trim(), hex);
            }
            OnInputChanged();
        };
        stackBg.Children.Add(CreateRowWithHelp("HEX-код фона HTML-страницы", _sHtmlBgColor,
            "Цвет заливки заднего плана для новой вкладки и поискового интерфейса Nova Search."));
        panel.Children.Add(cardBg);
        var cardColors = CreateCard("Детальная палитра интерфейса", "Полная настройка всех цветов браузера: панели, кнопки, текст и границы.");
        var stackColors = (StackPanel)cardColors.Child;

        var btnToggleColors = new Button
        {
            Content = "▼ Развернуть палитру всех цветов интерфейса",
            Height = 34,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(14, 0, 14, 0)
        };
        StyleDarkButton(btnToggleColors, false);

        var stackExpandableColors = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) };

        btnToggleColors.Click += (_, _) =>
        {
            if (stackExpandableColors.Visibility == Visibility.Visible)
            {
                stackExpandableColors.Visibility = Visibility.Collapsed;
                btnToggleColors.Content = "▼ Развернуть палитру всех цветов интерфейса";
            }
            else
            {
                stackExpandableColors.Visibility = Visibility.Visible;
                btnToggleColors.Content = "▲ Свернуть палитру цветов";
            }
        };
        stackColors.Children.Add(btnToggleColors);
        stackColors.Children.Add(stackExpandableColors);

        _sCustomTitleBarColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _sCustomNavBarColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _sCustomAddressBarColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _sCustomCardColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _sCustomPopupColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _sCustomBorderColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _sCustomTextColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _sCustomTextSecColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _sCustomGlowColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _sCustomSidebarColor = new TextBox { Width = 110, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };

        stackExpandableColors.Children.Add(CreateColorConfigRow("Шапка окна и заголовок", _sCustomTitleBarColor, "#0E101A", "TitleBarBrush"));
        stackExpandableColors.Children.Add(CreateColorConfigRow("Панель навигации", _sCustomNavBarColor, "#141724", "NavBarBrush"));
        stackExpandableColors.Children.Add(CreateColorConfigRow("Адресная строка и поля ввода", _sCustomAddressBarColor, "#10131E", "BgInputBrush"));
        stackExpandableColors.Children.Add(CreateColorConfigRow("Карточки настроек", _sCustomCardColor, "#141724", "BgCardBrush"));
        stackExpandableColors.Children.Add(CreateColorConfigRow("Всплывающие меню и окна", _sCustomPopupColor, "#1A1D2E", "PopupBrush"));
        stackExpandableColors.Children.Add(CreateColorConfigRow("Границы и контуры", _sCustomBorderColor, "#262A3E", "BorderBrush"));
        stackExpandableColors.Children.Add(CreateColorConfigRow("Основной текст", _sCustomTextColor, "#F4F6FC", "TextPrimaryBrush"));
        stackExpandableColors.Children.Add(CreateColorConfigRow("Вторичный текст", _sCustomTextSecColor, "#949AB0", "TextSecondaryBrush"));
        stackExpandableColors.Children.Add(CreateColorConfigRow("Неоновое свечение (Glow)", _sCustomGlowColor, "#BEF47B", "GlowLine"));
        stackExpandableColors.Children.Add(CreateColorConfigRow("Боковая панель навигации", _sCustomSidebarColor, "#1A1E2E", "BgSurfaceBrush"));

        var btnResetColors = new Button
        {
            Content = "Сбросить к цветам темы по умолчанию",
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 10, 0, 4),
            Padding = new Thickness(12, 0, 12, 0)
        };
        StyleDarkButton(btnResetColors, false);
        btnResetColors.Click += (_, _) =>
        {
            _sCustomTitleBarColor.Text = "";
            _sCustomNavBarColor.Text = "";
            _sCustomAddressBarColor.Text = "";
            _sCustomCardColor.Text = "";
            _sCustomPopupColor.Text = "";
            _sCustomBorderColor.Text = "";
            _sCustomTextColor.Text = "";
            _sCustomTextSecColor.Text = "";
            _sCustomGlowColor.Text = "";
            _sCustomSidebarColor.Text = "";
            ApplyTheme(_settings?.Theme ?? "Dark");
            OnInputChanged();
        };
        stackExpandableColors.Children.Add(btnResetColors);

        panel.Children.Add(cardColors);


        var cardRadius = CreateCard("Геометрия интерфейса", "Форма вкладок и скругления.");
        var stackRadius = (StackPanel)cardRadius.Child;

        _sTabRadius = new ComboBox { Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
        _sTabRadius.Items.Add(new ComboBoxItem { Content = "0 px — Прямоугольные", Tag = 0 });
        _sTabRadius.Items.Add(new ComboBoxItem { Content = "6 px — Слегка скругленные", Tag = 6 });
        _sTabRadius.Items.Add(new ComboBoxItem { Content = "10 px — Стандарт Nova", Tag = 10 });
        _sTabRadius.Items.Add(new ComboBoxItem { Content = "14 px — Мягкие скругления", Tag = 14 });
        _sTabRadius.Items.Add(new ComboBoxItem { Content = "18 px — Круглые", Tag = 18 });
        _sTabRadius.SelectionChanged += (_, _) => OnInputChanged();

        stackRadius.Children.Add(CreateRowWithHelp("Скругление углов вкладок", _sTabRadius,
            "Задает радиус верхних углов плашек вкладок на верхней панели навигации."));
        panel.Children.Add(cardRadius);

        var cardEffects = CreateCard("Эффекты и панели", "Включение визуальных эффектов и вспомогательных баров.");
        var stackEffects = (StackPanel)cardEffects.Child;

        _sGlow = new CheckBox { Content = "Неоновое свечение (Glow-эффект на панелях)", Margin = new Thickness(0, 0, 0, 8) };
        _sGlow.Checked += (_, _) => OnInputChanged();
        _sGlow.Unchecked += (_, _) => OnInputChanged();
        stackEffects.Children.Add(CreateCheckBoxWithHelp(_sGlow,
            "Отображает светящуюся линию под адресной строкой и мягкую световую ауру вокруг активных кнопок."));

        _sAnimations = new CheckBox { Content = "Плавные анимации переходов и вкладок", Margin = new Thickness(0, 0, 0, 8) };
        _sAnimations.Checked += (_, _) => OnInputChanged();
        _sAnimations.Unchecked += (_, _) => OnInputChanged();
        stackEffects.Children.Add(CreateCheckBoxWithHelp(_sAnimations,
            "Включает плавные аппаратные переходы при смене страниц, переключении вкладок и открытии меню."));

        _sBookmarksBar = new CheckBox { Content = "Отображать панель закладок", Margin = new Thickness(0, 0, 0, 8) };
        _sBookmarksBar.Checked += (_, _) => OnInputChanged();
        _sBookmarksBar.Unchecked += (_, _) => OnInputChanged();
        stackEffects.Children.Add(CreateCheckBoxWithHelp(_sBookmarksBar,
            "Показывает горизонтальную полосу быстрого доступа к избранным сайтам прямо под навигационной строкой."));

        _sStatusBar = new CheckBox { Content = "Отображать строку состояния (индикатор RAM и защита)", Margin = new Thickness(0, 0, 0, 4) };
        _sStatusBar.Checked += (_, _) => OnInputChanged();
        _sStatusBar.Unchecked += (_, _) => OnInputChanged();
        stackEffects.Children.Add(CreateCheckBoxWithHelp(_sStatusBar,
            "Нижняя полоса в окне браузера с информацией о загрузке, статусе соединения и текущем потреблении оперативной памяти."));

        panel.Children.Add(cardEffects);
                var cardWallpaper = CreateCard("Обои домашней страницы", "Фоновое изображение и эффекты на новой вкладке.");
        var stackWp = (StackPanel)cardWallpaper.Child;

        _sWallpaperPreset = new ComboBox { Width = 280, HorizontalAlignment = HorizontalAlignment.Left };
        _sWallpaperPreset.Items.Add(new ComboBoxItem { Content = "Горное озеро (по умолчанию)", Tag = "wallpaper-default.jpg" });
        _sWallpaperPreset.Items.Add(new ComboBoxItem { Content = "Альпийский закат", Tag = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=1920&q=80" });
        _sWallpaperPreset.Items.Add(new ComboBoxItem { Content = "Неоновый киберпанк", Tag = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=1920&q=80" });
        _sWallpaperPreset.Items.Add(new ComboBoxItem { Content = "Космическая туманность", Tag = "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?w=1920&q=80" });
        _sWallpaperPreset.Items.Add(new ComboBoxItem { Content = "Лес в тумане", Tag = "https://images.unsplash.com/photo-1448375240586-882707db888b?w=1920&q=80" });
        _sWallpaperPreset.Items.Add(new ComboBoxItem { Content = "Минимализм (темный градиент)", Tag = "linear" });
        _sWallpaperPreset.Items.Add(new ComboBoxItem { Content = "Пользовательское изображение (файл / URL)", Tag = "custom" });
        _sWallpaperPreset.SelectionChanged += (_, _) => OnInputChanged();
        stackWp.Children.Add(CreateRowWithHelp("Тема обоев", _sWallpaperPreset, "Выбор готовой панорамы, своего изображения, GIF-анимации или видеофайла (MP4, WebM) для домашней страницы."));

        var customWpRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 10) };
        var btnPickWpFile = new Button
        {
            Content = "🖼 Выбрать файл с диска...",
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnPickWpFile);
        btnPickWpFile.Click += (_, _) =>
        {
            try
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Выберите фоновое изображение",
                    Filter = "Все медиа (*.jpg;*.png;*.webp;*.gif;*.mp4;*.webm;*.mov)|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.mp4;*.webm;*.mov|Изображения и GIF (*.jpg;*.png;*.webp;*.gif)|*.jpg;*.jpeg;*.png;*.webp;*.gif|Видео (*.mp4;*.webm;*.mov)|*.mp4;*.webm;*.mov|Все файлы (*.*)|*.*"
                };
                if (ofd.ShowDialog() == true)
                {
                    if (_sCustomWpPath != null) _sCustomWpPath.Text = ofd.FileName;
                    if (_sWallpaperPreset != null) _sWallpaperPreset.SelectedIndex = 6;
                    OnInputChanged();
                }
            }
            catch { }
        };
        customWpRow.Children.Add(btnPickWpFile);

        _sCustomWpPath = new TextBox { Width = 300, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_sCustomWpPath);
        _sCustomWpPath.TextChanged += (_, _) => OnInputChanged();
        customWpRow.Children.Add(_sCustomWpPath);
        stackWp.Children.Add(CreateRowWithHelp("Файл или ссылка на обои", customWpRow, "Путь к файлу на компьютере или прямая веб-ссылка на картинку."));

        _sWpDimSlider = new Slider { Minimum = 0, Maximum = 80, Value = 35, Width = 260, HorizontalAlignment = HorizontalAlignment.Left };
        _sWpDimSlider.ValueChanged += (_, _) => OnInputChanged();
        stackWp.Children.Add(CreateRowWithHelp("Затемнение фона (%)", _sWpDimSlider, "Регулирует затемнение фонового рисунка для комфортного чтения текста."));

        panel.Children.Add(cardWallpaper);
        return panel;
    }

    private StackPanel BuildSecurityPanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Collapsed };

        var h1 = new TextBlock
        {
            Text = "Безопасность и защита",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 16),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        h1.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        panel.Children.Add(h1);

        var cardBlock = CreateCard("Блокировка рекламы и спама", "Инструменты для чистой работы в сети без баннеров.");
        var stackBlock = (StackPanel)cardBlock.Child;

        _sBlockAds = new CheckBox { Content = "Блокировать назойливую рекламу (AdBlock)", Margin = new Thickness(0, 0, 0, 8) };
        _sBlockAds.Checked += (_, _) => OnInputChanged();
        _sBlockAds.Unchecked += (_, _) => OnInputChanged();
        stackBlock.Children.Add(CreateCheckBoxWithHelp(_sBlockAds,
            "Удаляет рекламные баннеры, всплывающие объявления и автопроигрываемые рекламные ролики на страницах."));

        _sBlockCookieBanners = new CheckBox { Content = "Автоматически скрывать баннеры согласия на Cookie", Margin = new Thickness(0, 0, 0, 8) };
        _sBlockCookieBanners.Checked += (_, _) => OnInputChanged();
        _sBlockCookieBanners.Unchecked += (_, _) => OnInputChanged();
        stackBlock.Children.Add(CreateCheckBoxWithHelp(_sBlockCookieBanners,
            "Блокирует назойливые всплывающие окна с требованием подтвердить сбор файлов Cookie при входе на сайты."));

        _sBlockPopups = new CheckBox { Content = "Блокировать всплывающие всплывающие окна (Pop-ups)", Margin = new Thickness(0, 0, 0, 4) };
        _sBlockPopups.Checked += (_, _) => OnInputChanged();
        _sBlockPopups.Unchecked += (_, _) => OnInputChanged();
        stackBlock.Children.Add(CreateCheckBoxWithHelp(_sBlockPopups,
            "Предотвращает несанкционированное открытие новых вкладок и окон сторонними скриптами."));

        panel.Children.Add(cardBlock);

        var cardPrivacy = CreateCard("Приватность и трекеры", "Защита от межсайтового сбора персональных данных.");
        var stackPrivacy = (StackPanel)cardPrivacy.Child;

        _sBlockTrackers = new CheckBox { Content = "Защита от межсайтового отслеживания и аналитики", Margin = new Thickness(0, 0, 0, 8) };
        _sBlockTrackers.Checked += (_, _) => OnInputChanged();
        _sBlockTrackers.Unchecked += (_, _) => OnInputChanged();
        stackPrivacy.Children.Add(CreateCheckBoxWithHelp(_sBlockTrackers,
            "Запрещает рекламным сетям и аналитическим трекерам отслеживать ваши перемещения по сайтам."));

        _sSendDNT = new CheckBox { Content = "Передавать сайтам сигнал Do Not Track (Не отслеживать)", Margin = new Thickness(0, 0, 0, 8) };
        _sSendDNT.Checked += (_, _) => OnInputChanged();
        _sSendDNT.Unchecked += (_, _) => OnInputChanged();
        stackPrivacy.Children.Add(CreateCheckBoxWithHelp(_sSendDNT,
            "Отправляет стандартный заголовок с запросом запретить сайтам сбор и продажу истории ваших действий."));

        _sBlockFingerprinting = new CheckBox { Content = "Защита цифрового отпечатка (WebRTC & Canvas Masking)", Margin = new Thickness(0, 0, 0, 4) };
        _sBlockFingerprinting.Checked += (_, _) => OnInputChanged();
        _sBlockFingerprinting.Unchecked += (_, _) => OnInputChanged();
        stackPrivacy.Children.Add(CreateCheckBoxWithHelp(_sBlockFingerprinting,
            "Рандомизирует отпечаток Canvas и скрывает локальный IP-адрес через WebRTC для предотвращения фингерпринтинга устройства."));

        panel.Children.Add(cardPrivacy);

        var cardNet = CreateCard("Сетевая безопасность", "Шифрование соединений и защита от угроз.");
        var stackNet = (StackPanel)cardNet.Child;

        _sForceHttps = new CheckBox { Content = "Принудительный безопасный режим (Только HTTPS)", Margin = new Thickness(0, 0, 0, 8) };
        _sForceHttps.Checked += (_, _) => OnInputChanged();
        _sForceHttps.Unchecked += (_, _) => OnInputChanged();
        stackNet.Children.Add(CreateCheckBoxWithHelp(_sForceHttps,
            "Автоматически переводит любые соединения на защищенный протокол HTTPS и блокирует незашифрованный трафик HTTP."));

        _sPhishingProtection = new CheckBox { Content = "Встроенная защита от фишинга и вредоносных сайтов", Margin = new Thickness(0, 0, 0, 8) };
        _sPhishingProtection.Checked += (_, _) => OnInputChanged();
        _sPhishingProtection.Unchecked += (_, _) => OnInputChanged();
        stackNet.Children.Add(CreateCheckBoxWithHelp(_sPhishingProtection,
            "Проверяет доменные имена сайтов по базе известных фишинговых и вредоносных адресов для защиты учетных данных."));

        _sBlockCrypto = new CheckBox { Content = "Блокировать скрытый майнинг криптовалют на сайтах", Margin = new Thickness(0, 0, 0, 8) };
        _sBlockCrypto.Checked += (_, _) => OnInputChanged();
        _sBlockCrypto.Unchecked += (_, _) => OnInputChanged();
        stackNet.Children.Add(CreateCheckBoxWithHelp(_sBlockCrypto,
            "Останавливает вредоносные скрипты, которые пытаются использовать вычислительные мощности вашего процессора."));

        _sBlockAutoplay = new CheckBox { Content = "Блокировать автовоспроизведение медиа со звуком", Margin = new Thickness(0, 0, 0, 8) };
        _sBlockAutoplay.Checked += (_, _) => OnInputChanged();
        _sBlockAutoplay.Unchecked += (_, _) => OnInputChanged();
        stackNet.Children.Add(CreateCheckBoxWithHelp(_sBlockAutoplay,
            "Запрещает видеороликам и аудиодорожкам начинать автоматическое воспроизведение при открытии страницы."));

        _sAntiStealerJar = new CheckBox { Content = "Защита от стиллеров", Margin = new Thickness(0, 0, 0, 8) };
        _sAntiStealerJar.Checked += (_, _) => OnInputChanged();
        _sAntiStealerJar.Unchecked += (_, _) => OnInputChanged();
        stackNet.Children.Add(CreateCheckBoxWithHelp(_sAntiStealerJar,
            "Глубокая проверка архивов .JAR (включая моды Minecraft) и исполняемых файлов на наличие вебхуков Discord, грабберов токенов и стиллеров сессий."));

        _sAntiLeakOutbound = new CheckBox { Content = "Блокировка утечек токенов и сессий (Discord / Telegram Webhooks)", Margin = new Thickness(0, 0, 0, 4) };
        _sAntiLeakOutbound.Checked += (_, _) => OnInputChanged();
        _sAntiLeakOutbound.Unchecked += (_, _) => OnInputChanged();
        stackNet.Children.Add(CreateCheckBoxWithHelp(_sAntiLeakOutbound,
            "Перехватывает и блокирует скрытые сетевые запросы вредоносных скриптов, пытающихся отправить ваши пароли и токены на вебхуки Discord или боты Telegram."));

        panel.Children.Add(cardNet);
        var cardStealth = CreateCard("Сверхзащита и изоляция сайтов (Стелс-режим «Газировка»)", "Полная нейтрализация слежки для указанных сайтов: блокировка геолокации, подмена цифрового отпечатка, изоляция WebRTC, датчиков и заголовков.");
        var stackStealth = (StackPanel)cardStealth.Child;

        _sEnableSiteStealth = new CheckBox { Content = "Включить сверхзащиту и стелс-изоляцию сайтов", Margin = new Thickness(0, 0, 0, 8) };
        _sEnableSiteStealth.Checked += (_, _) => OnInputChanged();
        _sEnableSiteStealth.Unchecked += (_, _) => OnInputChanged();
        stackStealth.Children.Add(CreateCheckBoxWithHelp(_sEnableSiteStealth,
            "Активирует полную маскировку и блокировку для всех сайтов, указанных в списке ниже. Сайт не сможет определить ваше местоположение, устройство или поведение."));

        _sStealthIsolationDomains = new TextBox { Width = 380, Height = 32, HorizontalAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 0, 8) };
        StyleDarkTextBox(_sStealthIsolationDomains);
        _sStealthIsolationDomains.TextChanged += (_, _) => OnInputChanged();
        stackStealth.Children.Add(CreateRowWithHelp("Сайты для изоляции", _sStealthIsolationDomains,
            "Список доменов через запятую или пробел (например: max.gosuslugi.ru, gosuslugi.ru, *.tracker.com). Любые страницы этих сайтов будут полностью изолированы."));

        _sStealthBlockGeo = new CheckBox { Content = "Блокировать геолокацию и часовой пояс (не прочтет, где ты)", Margin = new Thickness(0, 0, 0, 8) };
        _sStealthBlockGeo.Checked += (_, _) => OnInputChanged();
        _sStealthBlockGeo.Unchecked += (_, _) => OnInputChanged();
        stackStealth.Children.Add(CreateCheckBoxWithHelp(_sStealthBlockGeo,
            "Запрещает сайту доступ к GPS-координатам, подменяет часовой пояс на UTC и скрывает ваше реальное географическое положение."));

        _sStealthBlockFingerprint = new CheckBox { Content = "Маскировать Canvas, WebGL, аудио и параметры экрана (не прочтет, что ты)", Margin = new Thickness(0, 0, 0, 8) };
        _sStealthBlockFingerprint.Checked += (_, _) => OnInputChanged();
        _sStealthBlockFingerprint.Unchecked += (_, _) => OnInputChanged();
        stackStealth.Children.Add(CreateCheckBoxWithHelp(_sStealthBlockFingerprint,
            "Рандомизирует отпечатки Canvas и WebGL, нормализует разрешение экрана под 1920x1080 и скрывает реальную видеокарту и процессор."));

        _sStealthBlockSensors = new CheckBox { Content = "Блокировать датчики, батарею, буфер обмена и периферию (не прочтет, как ты)", Margin = new Thickness(0, 0, 0, 8) };
        _sStealthBlockSensors.Checked += (_, _) => OnInputChanged();
        _sStealthBlockSensors.Unchecked += (_, _) => OnInputChanged();
        stackStealth.Children.Add(CreateCheckBoxWithHelp(_sStealthBlockSensors,
            "Блокирует гироскоп, акселерометр, уровень заряда батареи, чтение буфера обмена и доступ к микрофону, камере, Bluetooth и USB."));

        _sStealthBlockWebRTC = new CheckBox { Content = "Полная изоляция WebRTC (защита от утечки реального IP-адреса)", Margin = new Thickness(0, 0, 0, 8) };
        _sStealthBlockWebRTC.Checked += (_, _) => OnInputChanged();
        _sStealthBlockWebRTC.Unchecked += (_, _) => OnInputChanged();
        stackStealth.Children.Add(CreateCheckBoxWithHelp(_sStealthBlockWebRTC,
            "Полностью отключает RTCPeerConnection для изолированных сайтов, исключая раскрытие локального и публичного IP-адресов."));

        _sStealthStripTracking = new CheckBox { Content = "Очищать трекинговые метки URL и заголовок Referer (не передавать источник)", Margin = new Thickness(0, 0, 0, 4) };
        _sStealthStripTracking.Checked += (_, _) => OnInputChanged();
        _sStealthStripTracking.Unchecked += (_, _) => OnInputChanged();
        stackStealth.Children.Add(CreateCheckBoxWithHelp(_sStealthStripTracking,
            "Удаляет метки utm_*, fbclid, yclid, gclid из ссылок при переходе и очищает заголовок Referer."));

        panel.Children.Add(cardStealth);
        return panel;
    }

    private void StyleDarkPasswordBox(PasswordBox pb)
    {
        pb.SetResourceReference(PasswordBox.BackgroundProperty, "BgInputBrush");
        pb.SetResourceReference(PasswordBox.ForegroundProperty, "TextPrimaryBrush");
        pb.SetResourceReference(PasswordBox.BorderBrushProperty, "BorderBrush");
        pb.BorderThickness = new Thickness(1);
        pb.Padding = new Thickness(12, 0, 12, 0);
        pb.Height = 34;
        pb.VerticalContentAlignment = VerticalAlignment.Center;
        pb.SetResourceReference(PasswordBox.CaretBrushProperty, "AccentBrush");
    }

    private static async Task<(bool ok, int latencyMs, string msg)> TestProxyConnectionAsync(string host, int port, string proto = "http")
    {
        if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
            return (false, 0, "Неверный адрес или порт");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var pType = proto.ToLowerInvariant();

        if (host == "127.0.0.1" || host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var connectTask = client.ConnectAsync(host.Trim(), port);
                var timeoutTask = Task.Delay(100);
                var completed = await Task.WhenAny(connectTask, timeoutTask);
                sw.Stop();
                if (completed == connectTask && client.Connected)
                {
                    return (true, Math.Max(1, (int)sw.ElapsedMilliseconds), $"✓ {Math.Max(1, (int)sw.ElapsedMilliseconds)} мс");
                }
                return (false, 0, "✗ Недоступен");
            }
            catch
            {
                return (false, 0, "✗ Недоступен");
            }
        }

        if (pType == "http" || pType == "https")
        {
            try
            {
                var handler = new System.Net.Http.SocketsHttpHandler
                {
                    Proxy = new System.Net.WebProxy($"http://{host.Trim()}:{port}"),
                    UseProxy = true,
                    ConnectTimeout = TimeSpan.FromSeconds(2.5)
                };

                using var http = new System.Net.Http.HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(3)
                };

                var resp = await http.GetAsync("http://cp.cloudflare.com/generate_204");
                sw.Stop();
                if (resp.IsSuccessStatusCode || (int)resp.StatusCode < 400)
                {
                    return (true, (int)sw.ElapsedMilliseconds, $"✓ {sw.ElapsedMilliseconds} мс");
                }
            }
            catch { }
        }

        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync(host.Trim(), port);
            var timeoutTask = Task.Delay(1500);
            var completed = await Task.WhenAny(connectTask, timeoutTask);
            sw.Stop();
            if (completed == connectTask && client.Connected)
            {
                return (true, (int)sw.ElapsedMilliseconds, $"✓ {sw.ElapsedMilliseconds} мс");
            }
            return (false, 0, "✗ Недоступен");
        }
        catch
        {
            return (false, 0, "✗ Ошибка");
        }
    }

    private Border CreatePresetCard((string id, string name, string proto, string host, int port, string desc, string category, int latencyMs) def)
    {
        var itemBorder = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 8),
            BorderThickness = new Thickness(1),
            Background = GetThemeBrush("BgCardBrush", Color.FromRgb(20, 23, 34)),
            BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(36, 41, 62))
        };
        itemBorder.SetResourceReference(Border.BackgroundProperty, "BgCardBrush");
        itemBorder.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

        var rowGrid = new Grid();
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var infoStack = new StackPanel();
        var titleLine = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };

        var tbName = new TextBlock
        {
            Text = def.name,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13.5,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        tbName.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        titleLine.Children.Add(tbName);

        var catBadge = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 1, 6, 2),
            Margin = new Thickness(8, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
        };
        var catText = new TextBlock
        {
            Text = def.category,
            FontSize = 10,
            Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(160, 160, 160))
        };
        catBadge.Child = catText;
        titleLine.Children.Add(catBadge);

        if (def.proto != "Direct")
        {
            var addrBadge = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 1, 6, 2),
                Margin = new Thickness(6, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(40, 190, 244, 123))
            };
            var addrText = new TextBlock
            {
                Text = $"{def.proto} {def.host}:{def.port}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
                FontFamily = new FontFamily("Cascadia Code, Consolas, monospace")
            };
            addrText.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
            addrBadge.Child = addrText;
            titleLine.Children.Add(addrBadge);
        }

        infoStack.Children.Add(titleLine);

        var tbDesc = new TextBlock
        {
            Text = def.desc,
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(148, 154, 176))
        };
        tbDesc.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
        infoStack.Children.Add(tbDesc);

        Grid.SetColumn(infoStack, 0);
        rowGrid.Children.Add(infoStack);

        var pingBadge = new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 10, 0),
            Text = def.proto == "Direct" ? "✓ Доступно" : $"✓ {def.latencyMs} мс",
            Foreground = new SolidColorBrush(Color.FromRgb(190, 244, 123))
        };
        Grid.SetColumn(pingBadge, 1);
        rowGrid.Children.Add(pingBadge);

        var btnStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnSelect = new Button
        {
            Content = "Подключить",
            Height = 28,
            Padding = new Thickness(14, 0, 14, 0),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Cursor = Cursors.Hand
        };
        StyleDarkButton(btnSelect, false);
        var thisDef = def;
        btnSelect.Click += (_, _) =>
        {
            SelectProxyPreset(thisDef.id, thisDef.proto, thisDef.host, thisDef.port);
        };
        btnStack.Children.Add(btnSelect);

        Grid.SetColumn(btnStack, 2);
        rowGrid.Children.Add(btnStack);

        itemBorder.Child = rowGrid;

        _proxyPresetCards.Add((def.id, itemBorder, pingBadge, btnSelect, btnSelect));
        return itemBorder;
    }

    private async Task RefreshAvailableProxiesAsync()
    {
        if (_presetsContainer == null) return;
        if (_btnRefreshProxies != null) _btnRefreshProxies.IsEnabled = false;
        if (_proxyScanStatusText != null)
        {
            _proxyScanStatusText.Text = "⏳ Поиск и проверка доступных узлов...";
            _proxyScanStatusText.Foreground = Brushes.Gray;
        }

        var verifiedList = new List<(string id, string name, string proto, string host, int port, string desc, string category, int latencyMs)>();

        try
        {
            var localTasks = _localClientCandidates.Select(async c =>
            {
                try
                {
                    using var client = new System.Net.Sockets.TcpClient();
                    var connectTask = client.ConnectAsync(c.host, c.port);
                    var timeoutTask = Task.Delay(100);
                    var completed = await Task.WhenAny(connectTask, timeoutTask);
                    if (completed == connectTask && client.Connected)
                    {
                        return (true, c, 1);
                    }
                }
                catch { }
                return (false, c, 0);
            });

            var publicTasks = _publicCandidatePool.Select(async c =>
            {
                var (ok, ms, _) = await TestProxyConnectionAsync(c.host, c.port, c.proto);
                return (ok, c, ms);
            });

            var results = await Task.WhenAll(localTasks.Concat(publicTasks));
            foreach (var (ok, c, ms) in results)
            {
                if (ok)
                {
                    verifiedList.Add((c.id, c.name, c.proto, c.host, c.port, c.desc, c.category, ms));
                }
            }

            if (verifiedList.Count(x => x.category == "Публичный сервер") < 3)
            {
                try
                {
                    using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2.5) };
                    var raw = await http.GetStringAsync("https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=1500&country=all&ssl=all&anonymity=all");
                    var extraCandidates = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(l => l.Contains(':') && !l.StartsWith("#"))
                        .Take(5)
                        .ToList();

                    var extraTasks = extraCandidates.Select(async (ep, idx) =>
                    {
                        var parts = ep.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out var p))
                        {
                            var (ok, ms, _) = await TestProxyConnectionAsync(parts[0], p, "HTTP");
                            if (ok)
                            {
                                return (true, ($"dyn_{idx}", $"Облачный сервер #{idx + 1}", "HTTP", parts[0], p, "Проверенный рабочий прокси-узел.", "Публичный сервер"), ms);
                            }
                        }
                        return (false, ("", "", "", "", 0, "", ""), 0);
                    });

                    var extraResults = await Task.WhenAll(extraTasks);
                    foreach (var (ok, c, ms) in extraResults)
                    {
                        if (ok && !verifiedList.Any(v => v.host == c.Item4 && v.port == c.Item5))
                        {
                            verifiedList.Add((c.Item1, c.Item2, c.Item3, c.Item4, c.Item5, c.Item6, c.Item7, ms));
                        }
                    }
                }
                catch { }
            }
        }
        catch { }

        _presetsContainer.Children.Clear();
        _proxyPresetCards.Clear();

        var directDef = ("direct", "Прямое подключение (Без прокси)", "Direct", "", 0, "Выход в интернет напрямую через вашего интернет-провайдера на максимальной скорости.", "Провайдер", 0);
        var directCard = CreatePresetCard(directDef);
        _presetsContainer.Children.Add(directCard);

        foreach (var def in verifiedList)
        {
            var card = CreatePresetCard(def);
            _presetsContainer.Children.Add(card);
        }

        if (verifiedList.Count == 0)
        {
            var emptyNotice = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 4, 0, 8),
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1)
            };
            var emptyText = new TextBlock
            {
                Text = "В данный момент активные публичные узлы не ответили. Если у вас запущен Tor Browser, Clash или v2ray, нажмите «Обновить список», либо укажите свой прокси в блоке ниже.",
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(160, 165, 180))
            };
            emptyNotice.Child = emptyText;
            _presetsContainer.Children.Add(emptyNotice);
        }

        if (_proxyScanStatusText != null)
        {
            _proxyScanStatusText.Text = $"✓ Активных рабочих узлов: {verifiedList.Count}";
            _proxyScanStatusText.Foreground = new SolidColorBrush(Color.FromRgb(190, 244, 123));
        }

        UpdateProxyUiState();
        if (_btnRefreshProxies != null) _btnRefreshProxies.IsEnabled = true;
    }

    private void RestartApp()
    {
        try
        {
            SaveSession();
            SaveSettings();
            _settings.Save();

            foreach (var tab in _tabs.ToList())
            {
                try { tab.WebViewControl?.Dispose(); } catch { }
                tab.WebViewControl = null;
            }

            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                try { exePath = Process.GetCurrentProcess().MainModule?.FileName; } catch { }
            }
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                exePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NovaBrowser.exe");
            }

            var pid = Process.GetCurrentProcess().Id;
            var script = $"ping 127.0.0.1 -n 2 >nul & " +
                         $"taskkill /F /PID {pid} >nul 2>&1 & " +
                         $"start \"\" \"{exePath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{script}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
            Environment.Exit(0);
        }
        catch
        {
            try { Environment.Exit(0); } catch { }
        }
    }

    private void SelectProxyPreset(string id, string proto, string host, int port)
    {
        _selectedProxyPresetId = id;
        if (id == "direct")
        {
            if (_sProxyEnabled != null) _sProxyEnabled.IsChecked = false;
            _settings.ProxyEnabled = false;
            global::NovaBrowser.Helpers.NovaProxyBridge.SetUpstream(null, null, 0);
        }
        else
        {
            if (_sProxyEnabled != null) _sProxyEnabled.IsChecked = true;
            _settings.ProxyEnabled = true;
            _settings.ProxyType = proto;
            _settings.ProxyHost = host;
            _settings.ProxyPort = port;
            if (_sProxyType != null)
            {
                for (int i = 0; i < _sProxyType.Items.Count; i++)
                    if (_sProxyType.Items[i] is ComboBoxItem it && it.Tag?.ToString()?.Equals(proto, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _sProxyType.SelectedIndex = i;
                        break;
                    }
            }
            if (_sProxyHost != null) _sProxyHost.Text = host;
            if (_sProxyPort != null) _sProxyPort.Text = port.ToString();
            global::NovaBrowser.Helpers.NovaProxyBridge.SetUpstream(proto, host, port, _settings.ProxyUsername, _settings.ProxyPassword);
        }
        _settings.SelectedProxyPresetId = id;
        _settings.Save();
        SaveSettings();
        UpdateProxyUiState();
        UpdateProxyIndicator();
        ShowToast(id == "direct" ? "Прямое подключение активно (без перезапуска)" : $"Прокси {proto} {host}:{port} подключен мгновенно (без перезапуска)");
    }

    private void UpdateProxyUiState()
    {
        bool isEnabled = _sProxyEnabled?.IsChecked == true;
        var host = _sProxyHost?.Text?.Trim() ?? "";
        var port = _sProxyPort?.Text?.Trim() ?? "";
        var pType = (_sProxyType?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "HTTP";

        if (!isEnabled || _selectedProxyPresetId == "direct")
        {
            if (_proxyStatusBadge != null)
            {
                _proxyStatusBadge.Text = "ПРЯМОЕ ПОДКЛЮЧЕНИЕ (БЕЗ ПРОКСИ)";
                _proxyStatusBadge.Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(160, 160, 160));
            }
            if (_proxyStatusBorder != null)
            {
                _proxyStatusBorder.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
                _proxyStatusBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                if (_proxyStatusBorder.Child is StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is Border dot)
                {
                    dot.Background = Brushes.Gray;
                }
            }
        }
        else
        {
            var label = $"{pType} {host}:{port}";
            if (_proxyStatusBadge != null)
            {
                _proxyStatusBadge.Text = $"АКТИВЕН ПРОКСИ: {label}";
                _proxyStatusBadge.Foreground = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123));
            }
            if (_proxyStatusBorder != null)
            {
                var accCol = GetCurrentAccentColor();
                _proxyStatusBorder.Background = new SolidColorBrush(Color.FromArgb(25, accCol.R, accCol.G, accCol.B));
                _proxyStatusBorder.BorderBrush = new SolidColorBrush(accCol);
                if (_proxyStatusBorder.Child is StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is Border dot)
                {
                    dot.Background = new SolidColorBrush(accCol);
                }
            }
        }

        foreach (var (id, card, badge, btnSelect, btnTest) in _proxyPresetCards)
        {
            bool active = (id == "direct" && !isEnabled) || (isEnabled && id == _selectedProxyPresetId);
            if (active)
            {
                card.BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123));
                card.BorderThickness = new Thickness(1.5);
                btnSelect.Content = "✓ Активен";
                StyleDarkButton(btnSelect, true);
            }
            else
            {
                card.BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(36, 41, 62));
                card.BorderThickness = new Thickness(1);
                btnSelect.Content = (id == "direct") ? "Отключить прокси" : "Подключить";
                StyleDarkButton(btnSelect, false);
            }
        }
    }

    private StackPanel BuildProxyPanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Collapsed };

        var h1 = new TextBlock
        {
            Text = "Прокси-сервер",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 16),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        h1.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        panel.Children.Add(h1);

        var cardStatus = CreateCard("Статус подключения к сети", "Текущий режим работы сетевого стека браузера.");
        var stackStatus = (StackPanel)cardStatus.Child;

        _sProxyEnabled = new CheckBox
        {
            Content = "Использовать прокси-сервер в браузере",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        StyleDarkCheckBox(_sProxyEnabled);
        _sProxyEnabled.Checked += (_, _) =>
        {
            var host = _sProxyHost?.Text?.Trim() ?? "";
            int.TryParse(_sProxyPort?.Text?.Trim(), out var p);
            var proto = (_sProxyType?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "HTTP";
            var user = _sProxyUser?.Text?.Trim();
            var pass = _sProxyPass?.Password;
            if (!string.IsNullOrEmpty(host) && p > 0)
            {
                global::NovaBrowser.Helpers.NovaProxyBridge.SetUpstream(proto, host, p, user, pass);
            }
            UpdateProxyUiState();
            OnInputChanged();
        };
        _sProxyEnabled.Unchecked += (_, _) =>
        {
            global::NovaBrowser.Helpers.NovaProxyBridge.SetUpstream(null, null, 0);
            UpdateProxyUiState();
            OnInputChanged();
        };
        stackStatus.Children.Add(_sProxyEnabled);

        _proxyStatusBorder = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 4, 0, 4)
        };
        var statusInner = new StackPanel { Orientation = Orientation.Horizontal };
        var statusDot = new Border
        {
            Width = 10, Height = 10, CornerRadius = new CornerRadius(5),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _proxyStatusBadge = new TextBlock
        {
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusInner.Children.Add(statusDot);
        statusInner.Children.Add(_proxyStatusBadge);
        _proxyStatusBorder.Child = statusInner;
        stackStatus.Children.Add(_proxyStatusBorder);

        panel.Children.Add(cardStatus);

        var cardPresets = CreateCard("Готовые рабочие конфигурации прокси", "Отображаются только проверенные узлы, реально доступные прямо сейчас с вашего компьютера.");
        var stackPresets = (StackPanel)cardPresets.Child;

        var toolbarRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        toolbarRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toolbarRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _proxyScanStatusText = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(148, 154, 176)),
            Text = "Поиск активных узлов..."
        };
        Grid.SetColumn(_proxyScanStatusText, 0);
        toolbarRow.Children.Add(_proxyScanStatusText);

        _btnRefreshProxies = new Button
        {
            Content = "🔄 Обновить список",
            Height = 28,
            Padding = new Thickness(14, 0, 14, 0),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Cursor = Cursors.Hand
        };
        StyleDarkButton(_btnRefreshProxies, false);
        _btnRefreshProxies.Click += async (_, _) =>
        {
            await RefreshAvailableProxiesAsync();
        };
        Grid.SetColumn(_btnRefreshProxies, 1);
        toolbarRow.Children.Add(_btnRefreshProxies);
        stackPresets.Children.Add(toolbarRow);

        _presetsContainer = new StackPanel();
        stackPresets.Children.Add(_presetsContainer);

        panel.Children.Add(cardPresets);

        var cardCustom = CreateCard("Пользовательский прокси (Кастом)", "Введите параметры своего прокси-сервера вручную.");
        var stackCustom = (StackPanel)cardCustom.Child;

        var typeRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        typeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        typeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lblType = new TextBlock
        {
            Text = "Тип протокола:",
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        lblType.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        Grid.SetColumn(lblType, 0);
        typeRow.Children.Add(lblType);

        _sProxyType = new ComboBox { Width = 180, HorizontalAlignment = HorizontalAlignment.Left };
        _sProxyType.Items.Add(new ComboBoxItem { Content = "HTTP", Tag = "HTTP" });
        _sProxyType.Items.Add(new ComboBoxItem { Content = "HTTPS", Tag = "HTTPS" });
        _sProxyType.Items.Add(new ComboBoxItem { Content = "SOCKS5", Tag = "SOCKS5" });
        _sProxyType.Items.Add(new ComboBoxItem { Content = "SOCKS4", Tag = "SOCKS4" });
        StyleDarkComboBox(_sProxyType);
        _sProxyType.SelectionChanged += (_, _) => { OnInputChanged(); };
        Grid.SetColumn(_sProxyType, 1);
        typeRow.Children.Add(_sProxyType);
        stackCustom.Children.Add(typeRow);

        var hostPortRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        hostPortRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        hostPortRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hostPortRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        hostPortRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        hostPortRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lblHost = new TextBlock
        {
            Text = "Адрес и порт узла:",
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        lblHost.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        Grid.SetColumn(lblHost, 0);
        hostPortRow.Children.Add(lblHost);

        _sProxyHost = new TextBox { Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_sProxyHost);
        _sProxyHost.TextChanged += (_, _) => { OnInputChanged(); };
        Grid.SetColumn(_sProxyHost, 1);
        hostPortRow.Children.Add(_sProxyHost);

        var lblSep = new TextBlock
        {
            Text = ":",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        Grid.SetColumn(lblSep, 2);
        hostPortRow.Children.Add(lblSep);

        _sProxyPort = new TextBox { Height = 30, TextAlignment = TextAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_sProxyPort);
        _sProxyPort.TextChanged += (_, _) => { OnInputChanged(); };
        Grid.SetColumn(_sProxyPort, 3);
        hostPortRow.Children.Add(_sProxyPort);

        var btnTestCustom = new Button
        {
            Content = "⚡ Проверить",
            Height = 30,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(10, 0, 0, 0),
            Cursor = Cursors.Hand,
            FontSize = 11.5
        };
        StyleDarkButton(btnTestCustom, false);
        Grid.SetColumn(btnTestCustom, 4);
        hostPortRow.Children.Add(btnTestCustom);
        stackCustom.Children.Add(hostPortRow);

        var customPingStatus = new TextBlock
        {
            FontSize = 11,
            Margin = new Thickness(140, 0, 0, 10),
            Visibility = Visibility.Collapsed
        };
        stackCustom.Children.Add(customPingStatus);

        btnTestCustom.Click += async (_, _) =>
        {
            var host = _sProxyHost.Text?.Trim() ?? "";
            int.TryParse(_sProxyPort.Text?.Trim(), out var port);
            if (string.IsNullOrWhiteSpace(host) || port <= 0)
            {
                customPingStatus.Text = "Укажите корректный адрес и порт";
                customPingStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100));
                customPingStatus.Visibility = Visibility.Visible;
                return;
            }
            btnTestCustom.IsEnabled = false;
            btnTestCustom.Content = "Проверка...";
            customPingStatus.Text = "Подключение к узлу...";
            customPingStatus.Foreground = Brushes.Gray;
            customPingStatus.Visibility = Visibility.Visible;
            var pType = (_sProxyType?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "http";
            var (ok, ms, msg) = await TestProxyConnectionAsync(host, port, pType);
            btnTestCustom.IsEnabled = true;
            btnTestCustom.Content = "⚡ Проверить";
            if (ok)
            {
                customPingStatus.Text = $"✓ Узел доступен ({msg})";
                customPingStatus.Foreground = new SolidColorBrush(Color.FromRgb(190, 244, 123));
            }
            else
            {
                customPingStatus.Text = $"✗ Не удалось подключиться ({msg})";
                customPingStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100));
            }
        };

        var authRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        authRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        authRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        authRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        authRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lblAuth = new TextBlock
        {
            Text = "Авторизация:",
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        lblAuth.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        Grid.SetColumn(lblAuth, 0);
        authRow.Children.Add(lblAuth);

        _sProxyUser = new TextBox { Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_sProxyUser);
        _sProxyUser.TextChanged += (_, _) => { OnInputChanged(); };
        Grid.SetColumn(_sProxyUser, 1);
        authRow.Children.Add(_sProxyUser);

        _sProxyPass = new PasswordBox { Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkPasswordBox(_sProxyPass);
        _sProxyPass.PasswordChanged += (_, _) => { OnInputChanged(); };
        Grid.SetColumn(_sProxyPass, 3);
        authRow.Children.Add(_sProxyPass);

        stackCustom.Children.Add(authRow);

        var bypassRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        bypassRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        bypassRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lblBypass = new TextBlock
        {
            Text = "Исключения:",
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        lblBypass.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        Grid.SetColumn(lblBypass, 0);
        bypassRow.Children.Add(lblBypass);

        _sProxyBypass = new TextBox { Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_sProxyBypass);
        _sProxyBypass.TextChanged += (_, _) => { OnInputChanged(); };
        Grid.SetColumn(_sProxyBypass, 1);
        bypassRow.Children.Add(_sProxyBypass);
        stackCustom.Children.Add(bypassRow);

        var btnActivateCustom = new Button
        {
            Content = "Включить и использовать этот кастомный прокси",
            Height = 32,
            Padding = new Thickness(16, 0, 16, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(140, 4, 0, 4),
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.Bold
        };
        StyleDarkButton(btnActivateCustom, true);
        btnActivateCustom.Click += (_, _) =>
        {
            if (_sProxyEnabled != null) _sProxyEnabled.IsChecked = true;
            _selectedProxyPresetId = "custom";
            var host = _sProxyHost?.Text?.Trim() ?? "";
            int.TryParse(_sProxyPort?.Text?.Trim(), out var p);
            var proto = (_sProxyType?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "HTTP";
            var user = _sProxyUser?.Text?.Trim();
            var pass = _sProxyPass?.Password;
            global::NovaBrowser.Helpers.NovaProxyBridge.SetUpstream(proto, host, p > 0 ? p : 8080, user, pass);
            UpdateProxyUiState();
            OnInputChanged();
            OnApplySettingsClicked();
            ShowToast("Кастомный прокси применён мгновенно (без перезапуска)");
        };
        stackCustom.Children.Add(btnActivateCustom);

        panel.Children.Add(cardCustom);

        var cardRestart = CreateCard("Применение настроек прокси", "Изменения применяются мгновенно в реальном времени без необходимости перезапуска браузера.");
        var stackRestart = (StackPanel)cardRestart.Child;

        var restartRow = new StackPanel { Orientation = Orientation.Horizontal };

        var btnApplyP = new Button
        {
            Content = "Сохранить и применить настройки",
            Height = 34,
            Padding = new Thickness(20, 0, 20, 0),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.Bold
        };
        StyleDarkButton(btnApplyP, true);
        btnApplyP.Click += (_, _) =>
        {
            OnApplySettingsClicked();
            ShowToast("Настройки прокси применены мгновенно (без перезапуска)");
        };
        restartRow.Children.Add(btnApplyP);

        stackRestart.Children.Add(restartRow);
        panel.Children.Add(cardRestart);

        _ = RefreshAvailableProxiesAsync();

        return panel;
    }

    private StackPanel BuildPerformancePanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Collapsed };

        var h1 = new TextBlock
        {
            Text = "Производительность и память",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 16),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        h1.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        panel.Children.Add(h1);



        var cardRam = CreateCard("Оптимизация оперативной памяти (RAM Saver)", "Контроль расхода системных ресурсов.");
        var stackRam = (StackPanel)cardRam.Child;

        _sRamSaver = new CheckBox { Content = "Режим экономии оперативной памяти (RAM Saver)", Margin = new Thickness(0, 0, 0, 8) };
        _sRamSaver.Checked += (_, _) => OnInputChanged();
        _sRamSaver.Unchecked += (_, _) => OnInputChanged();
        stackRam.Children.Add(CreateCheckBoxWithHelp(_sRamSaver,
            "Агрессивно снижает объем памяти, занимаемой браузером при приближении к установленному лимиту."));

        _sRamLimit = new ComboBox { Width = 220, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 8) };
        _sRamLimit.Items.Add(new ComboBoxItem { Content = "1000 МБ", Tag = 1000 });
        _sRamLimit.Items.Add(new ComboBoxItem { Content = "1500 МБ", Tag = 1500 });
        _sRamLimit.Items.Add(new ComboBoxItem { Content = "2000 МБ", Tag = 2000 });
        _sRamLimit.Items.Add(new ComboBoxItem { Content = "3000 МБ", Tag = 3000 });
        _sRamLimit.SelectionChanged += (_, _) => OnInputChanged();

        stackRam.Children.Add(CreateRowWithHelp("Порог предупреждения RAM", _sRamLimit,
            "Уровень использования оперативной памяти, при котором строка состояния подсвечивается и запускается сборка мусора."));

        panel.Children.Add(cardRam);

        var cardHw = CreateCard("Аппаратное ускорение и кэш", "Использование видеокарты и оптимизация сети.");
        var stackHw = (StackPanel)cardHw.Child;

        _sGpuAcceleration = new CheckBox { Content = "Использовать аппаратное ускорение видео и графики (GPU)", Margin = new Thickness(0, 0, 0, 8) };
        _sGpuAcceleration.Checked += (_, _) => OnInputChanged();
        _sGpuAcceleration.Unchecked += (_, _) => OnInputChanged();
        stackHw.Children.Add(CreateCheckBoxWithHelp(_sGpuAcceleration,
            "Переносит обработку анимаций, 3D-графики и видео на графический процессор, снижая нагрузку на CPU."));

        _sPreloadPages = new CheckBox { Content = "Предварительная загрузка домашней страницы и частых ссылок", Margin = new Thickness(0, 0, 0, 4) };
        _sPreloadPages.Checked += (_, _) => OnInputChanged();
        _sPreloadPages.Unchecked += (_, _) => OnInputChanged();
        stackHw.Children.Add(CreateCheckBoxWithHelp(_sPreloadPages,
            "Заранее подготавливает страницы в фоновом режиме для мгновенного перехода без задержек."));

        panel.Children.Add(cardHw);
        return panel;
    }

    private StackPanel BuildMiscPanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Collapsed };

        var h1 = new TextBlock
        {
            Text = "Прочие параметры",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 16),
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        h1.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        panel.Children.Add(h1);

        var cardClean = CreateCard("Очистить данные браузера", "Удаление кэша, истории и файлов cookie.");
        var stackClean = (StackPanel)cardClean.Child;

        var btnChromeClear = new Button
        {
            Content = "Очистка данных браузера",
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(0, 0, 0, 10),
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.Bold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnChromeClear, true);
        btnChromeClear.Click += (_, _) =>
        {
            if (_clearDataModal != null) _clearDataModal.Visibility = Visibility.Visible;
        };
        stackClean.Children.Add(btnChromeClear);

        _sClearOnExit = new CheckBox { Content = "Автоматически очищать историю и кэш при выходе из браузера", Margin = new Thickness(0, 4, 0, 4) };
        _sClearOnExit.Checked += (_, _) => OnInputChanged();
        _sClearOnExit.Unchecked += (_, _) => OnInputChanged();
        stackClean.Children.Add(CreateCheckBoxWithHelp(_sClearOnExit,
            "При закрытии последнего окна Nova все следы сеанса, временные файлы и кэш будут безвозвратно стёрты."));

        panel.Children.Add(cardClean);

        var cardExtra = CreateCard("Дополнительные системные опции", "Поведение загрузчика и горячие клавиши разработчика.");
        var stackExtra = (StackPanel)cardExtra.Child;

        _sDevToolsF12 = new CheckBox { Content = "Открывать панель веб-разработчика (DevTools) по клавише F12", Margin = new Thickness(0, 0, 0, 8) };
        _sDevToolsF12.Checked += (_, _) => OnInputChanged();
        _sDevToolsF12.Unchecked += (_, _) => OnInputChanged();
        stackExtra.Children.Add(CreateCheckBoxWithHelp(_sDevToolsF12,
            "Предоставляет доступ к консоли инспектора элементов, сетевым запросам и отладке скриптов."));

        _sAskDownload = new CheckBox { Content = "Запрашивать место для сохранения каждого загружаемого файла", Margin = new Thickness(0, 0, 0, 4) };
        _sAskDownload.Checked += (_, _) => OnInputChanged();
        _sAskDownload.Unchecked += (_, _) => OnInputChanged();
        stackExtra.Children.Add(CreateCheckBoxWithHelp(_sAskDownload,
            "Перед началом любой загрузки открывает стандартный диалог выбора папки и имени файла на диске."));

        panel.Children.Add(cardExtra);

        var cardExt = CreateCard("Расширения браузера (Chrome / Edge)", "Установка дополнений из каталогов Chrome Web Store и Edge Add-ons.");
        var stackExt = (StackPanel)cardExt.Child;

        var warnBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(160, 245, 158, 11)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var warnText = new TextBlock
        {
            Text = "⚠️ Предупреждение: Nova Browser поддерживает расширения Chrome и Microsoft Edge (Manifest v2 / v3). Некоторые расширения, требующие внутренних API браузера Google Chrome (например, прямое изменение оболочки окна), могут работать с ограничениями.",
            FontSize = 11,
            LineHeight = 16,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 100))
        };
        warnBorder.Child = warnText;
        stackExt.Children.Add(warnBorder);

        var extBtnsWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };

        var btnStoreChrome = new Button
        {
            Content = "🌐 Chrome Web Store",
            Padding = new Thickness(14, 6, 14, 6),
            Margin = new Thickness(0, 0, 8, 6),
            Cursor = Cursors.Hand,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnStoreChrome);
        btnStoreChrome.Click += (_, _) =>
        {
            CreateNewTab("https://chromewebstore.google.com");
            OnCloseSettingsClicked();
        };
        extBtnsWrap.Children.Add(btnStoreChrome);

        var btnStoreEdge = new Button
        {
            Content = "🌐 Microsoft Edge Add-ons",
            Padding = new Thickness(14, 6, 14, 6),
            Margin = new Thickness(0, 0, 8, 6),
            Cursor = Cursors.Hand,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnStoreEdge);
        btnStoreEdge.Click += (_, _) =>
        {
            CreateNewTab("https://microsoftedge.microsoft.com/addons");
            OnCloseSettingsClicked();
        };
        extBtnsWrap.Children.Add(btnStoreEdge);

        var btnInstallFolder = new Button
        {
            Content = "📁 Установить (.crx / папку)...",
            Padding = new Thickness(14, 6, 14, 6),
            Margin = new Thickness(0, 0, 8, 6),
            Cursor = Cursors.Hand,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnInstallFolder, true);
        btnInstallFolder.Click += async (_, _) =>
        {
            try
            {
                var fbd = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Выберите распакованную папку расширения (с файлом manifest.json):"
                };
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(fbd.SelectedPath))
                {
                    var ok = await InstallExtensionAsync(fbd.SelectedPath);
                    if (ok)
                    {
                        ShowToast("Расширение успешно установлено");
                        RefreshExtensionsList();
                    }
                    else
                    {
                        ShowToast("Не удалось загрузить расширение");
                    }
                }
            }
            catch { }
        };
        extBtnsWrap.Children.Add(btnInstallFolder);
        stackExt.Children.Add(extBtnsWrap);

        _extensionsListContainer = new StackPanel();
        stackExt.Children.Add(_extensionsListContainer);
        RefreshExtensionsList();
        panel.Children.Add(cardExt);

        var cardDanger = CreateCard("Сброс параметров браузера", "Восстановление конфигурации приложения к первоначальным значениям.");
        var stackDanger = (StackPanel)cardDanger.Child;

        var dangerRow = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        dangerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dangerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var dangerInfo = new StackPanel();
        var dTitle = new TextBlock
        {
            Text = "Полный сброс параметров Nova Browser",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 90, 90))
        };
        var dSub = new TextBlock
        {
            Text = "Сбрасывает все визуальные темы, шрифты, фильтры поиска и сетевые параметры по дефолту.",
            FontSize = 11,
            Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(150, 150, 150))
        };
        dSub.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        dangerInfo.Children.Add(dTitle);
        dangerInfo.Children.Add(dSub);
        Grid.SetColumn(dangerInfo, 0);
        dangerRow.Children.Add(dangerInfo);

        var btnFullResetAct = new Button
        {
            Content = "Полный сброс...",
            Padding = new Thickness(14, 6, 14, 6),
            Cursor = Cursors.Hand,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
            BorderThickness = new Thickness(0),
            Tag = "SettingsInternal"
        };
        btnFullResetAct.Click += (_, _) => ShowFullResetModal();
        Grid.SetColumn(btnFullResetAct, 1);
        dangerRow.Children.Add(btnFullResetAct);

        stackDanger.Children.Add(dangerRow);
        panel.Children.Add(cardDanger);

        return panel;
    }

    private void UpdateColorChipsVisuals()
    {
        var curAcc = _sAccentColor?.Text?.Trim() ?? "";
        if (_accentPreviewSwatch != null && IsValidHex(curAcc))
        {
            _accentPreviewSwatch.Background = ParseHexBrush(curAcc, Color.FromRgb(190, 244, 123));
        }
        foreach (var (chip, hex) in _accentChips)
        {
            bool sel = hex.Equals(curAcc, StringComparison.OrdinalIgnoreCase);
            chip.BorderThickness = new Thickness(sel ? 2.5 : 1);
            chip.BorderBrush = sel ? Brushes.White : new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            try
            {
                chip.Effect = sel ? new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 0, Opacity = 0.85, Color = Colors.White } : null;
            }
            catch { }
        }
        var curBg = _sHtmlBgColor?.Text?.Trim() ?? "";
        if (_bgPreviewSwatch != null && IsValidHex(curBg))
        {
            _bgPreviewSwatch.Background = ParseHexBrush(curBg, Color.FromRgb(7, 9, 13));
        }
        foreach (var (chip, hex) in _bgChips)
        {
            bool sel = hex.Equals(curBg, StringComparison.OrdinalIgnoreCase);
            chip.BorderThickness = new Thickness(sel ? 2.5 : 1);
            chip.BorderBrush = sel ? (GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123))) : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            try
            {
                chip.Effect = sel ? new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 0, Opacity = 0.85, Color = GetCurrentAccentColor() } : null;
            }
            catch { }
        }
    }

    private void LoadSettingsIntoUi()
    {
        _suppressDirtyEvents = true;
        try
        {
            if (_sLanguage != null)
            {
                var lang = _settings.BrowserLanguage ?? "ru-RU";
                for (int i = 0; i < _sLanguage.Items.Count; i++)
                    if (_sLanguage.Items[i] is ComboBoxItem c && c.Tag?.ToString() == lang)
                    { _sLanguage.SelectedIndex = i; break; }
            }

            if (_sSearchEngine != null)
            {
                var eng = _settings.SearchEngine ?? "Nova";
                int matchIdx = -1;
                for (int i = 0; i < _sSearchEngine.Items.Count; i++)
                {
                    if (_sSearchEngine.Items[i] is ComboBoxItem c)
                    {
                        var content = c.Content?.ToString() ?? "";
                        if (content.Equals(eng, StringComparison.OrdinalIgnoreCase) ||
                            (eng.Equals("Nova Mix", StringComparison.OrdinalIgnoreCase) && content == "Nova") ||
                            (eng.Equals("Yandex", StringComparison.OrdinalIgnoreCase) && content == "Яндекс") ||
                            (eng.Equals("Яндекс", StringComparison.OrdinalIgnoreCase) && content == "Яндекс"))
                        {
                            matchIdx = i;
                            break;
                        }
                    }
                }
                _sSearchEngine.SelectedIndex = matchIdx >= 0 ? matchIdx : 0;
            }

            if (_sHomepage != null) _sHomepage.Text = _settings.HomepageUrl ?? "";

            if (_sZoom != null)
            {
                var z = _settings.DefaultZoom;
                for (int i = 0; i < _sZoom.Items.Count; i++)
                    if (_sZoom.Items[i] is ComboBoxItem c && (int)(c.Tag ?? 100) == z)
                    { _sZoom.SelectedIndex = i; break; }
            }

            if (_sNewTab != null) _sNewTab.IsChecked = _settings.OpenLinksInNewTab;
            if (_sRestoreSession != null) _sRestoreSession.IsChecked = _settings.RestoreLastSession;
            if (_sConfirmCloseTabs != null) _sConfirmCloseTabs.IsChecked = _settings.ConfirmCloseMultipleTabs;
            if (_sDownloadFolder != null) _sDownloadFolder.Text = _settings.DownloadFolder ?? "";

            if (_sSourceGoogle != null) _sSourceGoogle.IsChecked = _settings.SearchSourceGoogle;
            if (_sSourceYandex != null) _sSourceYandex.IsChecked = _settings.SearchSourceYandex;
            if (_sSourceWiki != null) _sSourceWiki.IsChecked = _settings.SearchSourceWiki;
            if (_sSourceHabr != null) _sSourceHabr.IsChecked = _settings.SearchSourceHabr;
            if (_sSourceGitHub != null) _sSourceGitHub.IsChecked = _settings.SearchSourceGitHub;
            if (_sSourceKinopoisk != null) _sSourceKinopoisk.IsChecked = _settings.SearchSourceKinopoisk;
            if (_sSourceYouTube != null) _sSourceYouTube.IsChecked = _settings.SearchSourceYouTube;
            if (_sSourceVK != null) _sSourceVK.IsChecked = _settings.SearchSourceVK;
            if (_sSiteFilter != null) _sSiteFilter.Text = _settings.SearchSiteFilter ?? "";

            if (_sWallpaperPreset != null)
            {
                if (!string.IsNullOrWhiteSpace(_settings.CustomWallpaperPath))
                {
                    _sWallpaperPreset.SelectedIndex = 6;
                }
                else if (_settings.WallpaperPresetIndex >= 0 && _settings.WallpaperPresetIndex < _sWallpaperPreset.Items.Count)
                {
                    _sWallpaperPreset.SelectedIndex = _settings.WallpaperPresetIndex;
                }
                else
                {
                    _sWallpaperPreset.SelectedIndex = 0;
                }
            }
            if (_sCustomWpPath != null) _sCustomWpPath.Text = _settings.CustomWallpaperPath ?? "";
            if (_sWpDimSlider != null) _sWpDimSlider.Value = Math.Clamp((int)(_settings.HomeBgImageOpacity * 100), 0, 80);

            if (_sTheme != null)
            {
                var th = _settings.Theme ?? "Dark";
                for (int i = 0; i < _sTheme.Items.Count; i++)
                    if (_sTheme.Items[i] is ComboBoxItem c && c.Tag?.ToString() == th)
                    { _sTheme.SelectedIndex = i; break; }
            }

            if (_sAccentColor != null) _sAccentColor.Text = _settings.AccentColor ?? "#BEF47B";
            if (_sHtmlBgColor != null) _sHtmlBgColor.Text = _settings.HtmlBackgroundColor ?? "#07090d";
            if (_sCustomTitleBarColor != null) _sCustomTitleBarColor.Text = _settings.CustomTitleBarColor ?? "";
            if (_sCustomNavBarColor != null) _sCustomNavBarColor.Text = _settings.CustomNavBarColor ?? "";
            if (_sCustomAddressBarColor != null) _sCustomAddressBarColor.Text = _settings.CustomAddressBarColor ?? "";
            if (_sCustomCardColor != null) _sCustomCardColor.Text = _settings.CustomCardColor ?? "";
            if (_sCustomPopupColor != null) _sCustomPopupColor.Text = _settings.CustomPopupColor ?? "";
            if (_sCustomBorderColor != null) _sCustomBorderColor.Text = _settings.CustomBorderColor ?? "";
            if (_sCustomTextColor != null) _sCustomTextColor.Text = _settings.CustomTextColor ?? "";
            if (_sCustomTextSecColor != null) _sCustomTextSecColor.Text = _settings.CustomTextSecondaryColor ?? "";
            if (_sCustomGlowColor != null) _sCustomGlowColor.Text = _settings.CustomGlowColor ?? "";
            if (_sCustomSidebarColor != null) _sCustomSidebarColor.Text = _settings.CustomSidebarColor ?? "";
            UpdateColorChipsVisuals();

            if (_sTabRadius != null)
            {
                var tr = _settings.TabRadius;
                for (int i = 0; i < _sTabRadius.Items.Count; i++)
                    if (_sTabRadius.Items[i] is ComboBoxItem c && (int)(c.Tag ?? 10) == tr)
                    { _sTabRadius.SelectedIndex = i; break; }
            }

            if (_sGlow != null) _sGlow.IsChecked = _settings.AnimationsEnabled;
            if (_sAnimations != null) _sAnimations.IsChecked = !_settings.ReduceAnimations;
            if (_sBookmarksBar != null) _sBookmarksBar.IsChecked = _settings.ShowBookmarksBar;
            if (_sStatusBar != null) _sStatusBar.IsChecked = _settings.ShowStatusBar;

            if (_sBlockAds != null) _sBlockAds.IsChecked = _settings.BlockAds;
            if (_sBlockTrackers != null) _sBlockTrackers.IsChecked = _settings.BlockTrackers;
            if (_sBlockCookieBanners != null) _sBlockCookieBanners.IsChecked = _settings.BlockCookieBanners;
            if (_sForceHttps != null) _sForceHttps.IsChecked = _settings.ForceHttps;
            if (_sBlockPopups != null) _sBlockPopups.IsChecked = _settings.BlockPopups;
            if (_sPhishingProtection != null) _sPhishingProtection.IsChecked = _settings.EnablePhishingProtection;
            if (_sBlockFingerprinting != null) _sBlockFingerprinting.IsChecked = _settings.BlockFingerprinting;
            if (_sSendDNT != null) _sSendDNT.IsChecked = _settings.SendDoNotTrack;
            if (_sBlockAutoplay != null) _sBlockAutoplay.IsChecked = _settings.BlockAutoplay;
            if (_sBlockCrypto != null) _sBlockCrypto.IsChecked = _settings.BlockCryptomining;
            if (_sAntiStealerJar != null) _sAntiStealerJar.IsChecked = _settings.AntiStealerProtection;
            if (_sAntiLeakOutbound != null) _sAntiLeakOutbound.IsChecked = _settings.AntiLeakProtection;
            if (_sUniversalPassAutofill != null) _sUniversalPassAutofill.IsChecked = _settings.UniversalPasswordAutofill;
            if (_sEnableSiteStealth != null) _sEnableSiteStealth.IsChecked = _settings.EnableSiteStealthIsolation;
            if (_sStealthIsolationDomains != null) _sStealthIsolationDomains.Text = _settings.StealthIsolationDomains ?? "max.gosuslugi.ru, gosuslugi.ru";
            if (_sStealthBlockGeo != null) _sStealthBlockGeo.IsChecked = _settings.StealthBlockGeo;
            if (_sStealthBlockFingerprint != null) _sStealthBlockFingerprint.IsChecked = _settings.StealthBlockFingerprint;
            if (_sStealthBlockSensors != null) _sStealthBlockSensors.IsChecked = _settings.StealthBlockSensorsAndHardware;
            if (_sStealthBlockWebRTC != null) _sStealthBlockWebRTC.IsChecked = _settings.StealthBlockWebRTC;
            if (_sStealthStripTracking != null) _sStealthStripTracking.IsChecked = _settings.StealthStripTrackingHeaders;



            if (_sRamSaver != null) _sRamSaver.IsChecked = _settings.RamSaverEnabled;
            if (_sRamLimit != null)
            {
                var rl = _settings.RamLimitThresholdMB;
                for (int i = 0; i < _sRamLimit.Items.Count; i++)
                    if (_sRamLimit.Items[i] is ComboBoxItem c && (int)(c.Tag ?? 1500) == rl)
                    { _sRamLimit.SelectedIndex = i; break; }
            }

            if (_sGpuAcceleration != null) _sGpuAcceleration.IsChecked = _settings.GpuAcceleration;
            if (_sPreloadPages != null) _sPreloadPages.IsChecked = _settings.PreloadPages;

            if (_sClearOnExit != null) _sClearOnExit.IsChecked = _settings.ClearOnExit;
            if (_sDevToolsF12 != null) _sDevToolsF12.IsChecked = _settings.OpenDevToolsOnF12;
            if (_sAskDownload != null) _sAskDownload.IsChecked = _settings.AskDownloadLocation;
            if (_sPasswordAutofill != null) _sPasswordAutofill.IsChecked = _settings.EnablePasswordAutofill;

            if (_sProxyEnabled != null) _sProxyEnabled.IsChecked = _settings.ProxyEnabled;
            if (_sProxyType != null)
            {
                var pt = _settings.ProxyType ?? "HTTP";
                for (int i = 0; i < _sProxyType.Items.Count; i++)
                    if (_sProxyType.Items[i] is ComboBoxItem it && it.Tag?.ToString()?.Equals(pt, StringComparison.OrdinalIgnoreCase) == true)
                    { _sProxyType.SelectedIndex = i; break; }
            }
            if (_sProxyHost != null) _sProxyHost.Text = _settings.ProxyHost ?? "";
            if (_sProxyPort != null) _sProxyPort.Text = _settings.ProxyPort > 0 ? _settings.ProxyPort.ToString() : "8080";
            if (_sProxyUser != null) _sProxyUser.Text = _settings.ProxyUsername ?? "";
            if (_sProxyPass != null) _sProxyPass.Password = _settings.ProxyPassword ?? "";
            if (_sProxyBypass != null) _sProxyBypass.Text = _settings.ProxyBypassList ?? "localhost;127.0.0.1;*.local";
            _selectedProxyPresetId = !string.IsNullOrEmpty(_settings.SelectedProxyPresetId) ? _settings.SelectedProxyPresetId : (_settings.ProxyEnabled ? "custom" : "direct");
            UpdateProxyUiState();

            RefreshAccountsList();
            RefreshPasswordsList();
            RefreshExtensionsList();
        }
        finally
        {
            _suppressDirtyEvents = false;
        }
    }

    private SettingsSnapshotData CaptureCurrentSnapshot()
    {
        var d = new SettingsSnapshotData();
        if (_sLanguage?.SelectedItem is ComboBoxItem cl) d.Language = cl.Tag?.ToString() ?? "ru-RU";
        if (_sSearchEngine?.SelectedItem is ComboBoxItem cs) d.SearchEngine = cs.Content?.ToString() ?? "Nova";
        d.Homepage = _sHomepage?.Text ?? "";
        if (_sZoom?.SelectedItem is ComboBoxItem cz && cz.Tag is int zi) d.Zoom = zi;
        d.NewTab = _sNewTab?.IsChecked == true;
        d.RestoreSession = _sRestoreSession?.IsChecked == true;
        d.ConfirmCloseTabs = _sConfirmCloseTabs?.IsChecked == true;
        d.DownloadFolder = _sDownloadFolder?.Text ?? "";

        d.SourceGoogle = _sSourceGoogle?.IsChecked == true;
        d.SourceYandex = _sSourceYandex?.IsChecked == true;
        d.SourceWiki = _sSourceWiki?.IsChecked == true;
        d.SourceHabr = _sSourceHabr?.IsChecked == true;
        d.SourceGitHub = _sSourceGitHub?.IsChecked == true;
        d.SourceKinopoisk = _sSourceKinopoisk?.IsChecked == true;
        d.SourceYouTube = _sSourceYouTube?.IsChecked == true;
        d.SourceVK = _sSourceVK?.IsChecked == true;
        d.SiteFilter = _sSiteFilter?.Text ?? "";

        d.WallpaperPreset = _sWallpaperPreset?.SelectedIndex ?? 0;
        d.WallpaperCustomPath = _sCustomWpPath?.Text ?? "";
        d.WallpaperDimming = (int)(_sWpDimSlider?.Value ?? 35);

        if (_sTheme?.SelectedItem is ComboBoxItem ct) d.Theme = ct.Tag?.ToString() ?? "Dark";
        d.AccentColor = _sAccentColor?.Text ?? "#BEF47B";
        d.HtmlBgColor = _sHtmlBgColor?.Text ?? "#07090d";
        d.CustomTitleBarColor = _sCustomTitleBarColor?.Text.Trim() ?? "";
        d.CustomNavBarColor = _sCustomNavBarColor?.Text.Trim() ?? "";
        d.CustomAddressBarColor = _sCustomAddressBarColor?.Text.Trim() ?? "";
        d.CustomCardColor = _sCustomCardColor?.Text.Trim() ?? "";
        d.CustomPopupColor = _sCustomPopupColor?.Text.Trim() ?? "";
        d.CustomBorderColor = _sCustomBorderColor?.Text.Trim() ?? "";
        d.CustomTextColor = _sCustomTextColor?.Text.Trim() ?? "";
        d.CustomTextSecColor = _sCustomTextSecColor?.Text.Trim() ?? "";
        d.CustomGlowColor = _sCustomGlowColor?.Text.Trim() ?? "";
        d.CustomSidebarColor = _sCustomSidebarColor?.Text.Trim() ?? "";
        d.CustomAddressBarGradient = _settings.CustomAddressBarGradient ?? "";
        d.CustomTitleBarGradient = _settings.CustomTitleBarGradient ?? "";
        d.CustomNavBarGradient = _settings.CustomNavBarGradient ?? "";
        d.CustomCardGradient = _settings.CustomCardGradient ?? "";
        d.CustomSidebarGradient = _settings.CustomSidebarGradient ?? "";
        if (_sTabRadius?.SelectedItem is ComboBoxItem cr && cr.Tag is int ri) d.TabRadius = ri;
        d.Glow = _sGlow?.IsChecked == true;
        d.Animations = _sAnimations?.IsChecked == true;
        d.BookmarksBar = _sBookmarksBar?.IsChecked == true;
        d.StatusBar = _sStatusBar?.IsChecked == true;

        d.BlockAds = _sBlockAds?.IsChecked == true;
        d.BlockTrackers = _sBlockTrackers?.IsChecked == true;
        d.BlockCookies = _sBlockCookieBanners?.IsChecked == true;
        d.ForceHttps = _sForceHttps?.IsChecked == true;
        d.BlockPopups = _sBlockPopups?.IsChecked == true;
        d.PhishingProtection = _sPhishingProtection?.IsChecked == true;
        d.BlockFingerprinting = _sBlockFingerprinting?.IsChecked == true;
        d.SendDNT = _sSendDNT?.IsChecked == true;
        d.BlockAutoplay = _sBlockAutoplay?.IsChecked == true;
        d.BlockCrypto = _sBlockCrypto?.IsChecked == true;
        d.AntiStealer = _sAntiStealerJar?.IsChecked == true;
        d.AntiLeak = _sAntiLeakOutbound?.IsChecked == true;
        d.UniversalPassAutofill = _sUniversalPassAutofill?.IsChecked == true;
        d.EnableSiteStealth = _sEnableSiteStealth?.IsChecked == true;
        d.StealthDomains = _sStealthIsolationDomains?.Text.Trim() ?? "max.gosuslugi.ru, gosuslugi.ru";
        d.StealthBlockGeo = _sStealthBlockGeo?.IsChecked == true;
        d.StealthBlockFingerprint = _sStealthBlockFingerprint?.IsChecked == true;
        d.StealthBlockSensors = _sStealthBlockSensors?.IsChecked == true;
        d.StealthBlockWebRTC = _sStealthBlockWebRTC?.IsChecked == true;
        d.StealthStripTracking = _sStealthStripTracking?.IsChecked == true;


        d.RamSaver = _sRamSaver?.IsChecked == true;
        if (_sRamLimit?.SelectedItem is ComboBoxItem crl && crl.Tag is int rli) d.RamLimit = rli;
        d.GpuAcceleration = _sGpuAcceleration?.IsChecked == true;
        d.PreloadPages = _sPreloadPages?.IsChecked == true;

        d.ClearOnExit = _sClearOnExit?.IsChecked == true;
        d.DevToolsF12 = _sDevToolsF12?.IsChecked == true;
        d.AskDownload = _sAskDownload?.IsChecked == true;
        d.PasswordAutofill = _sPasswordAutofill?.IsChecked == true;

        d.ProxyEnabled = _sProxyEnabled?.IsChecked == true;
        if (_sProxyType?.SelectedItem is ComboBoxItem cpt) d.ProxyType = cpt.Tag?.ToString() ?? "HTTP";
        d.ProxyHost = _sProxyHost?.Text?.Trim() ?? "";
        int.TryParse(_sProxyPort?.Text?.Trim(), out d.ProxyPort);
        d.ProxyUsername = _sProxyUser?.Text?.Trim() ?? "";
        d.ProxyPassword = _sProxyPass?.Password ?? "";
        d.ProxyBypassList = _sProxyBypass?.Text?.Trim() ?? "localhost;127.0.0.1;*.local";
        d.SelectedProxyPresetId = _selectedProxyPresetId;

        return d;
    }

    private void OnInputChanged()
    {
        if (_suppressDirtyEvents) return;
        var current = CaptureCurrentSnapshot();
        bool isDifferent = !current.Matches(_savedSnapshot);
        SetDirtyState(isDifferent);
    }

    private void SetDirtyState(bool dirty)
    {
        _isSettingsDirty = dirty;
        if (_floatingApplyBar != null)
        {
            _floatingApplyBar.Visibility = dirty ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnResetPendingClicked()
    {
        LoadSettingsIntoUi();
        SetDirtyState(false);
    }

    private void OnApplySettingsClicked()
    {
        var cur = CaptureCurrentSnapshot();

        _settings.BrowserLanguage = cur.Language;
        _settings.SearchEngine = cur.SearchEngine;
        _settings.HomepageUrl = cur.Homepage;
        _settings.DefaultZoom = cur.Zoom;
        _settings.OpenLinksInNewTab = cur.NewTab;
        _settings.RestoreLastSession = cur.RestoreSession;
        _settings.ConfirmCloseMultipleTabs = cur.ConfirmCloseTabs;
        _settings.DownloadFolder = cur.DownloadFolder;

        _settings.SearchSourceGoogle = cur.SourceGoogle;
        _settings.SearchSourceYandex = cur.SourceYandex;
        _settings.SearchSourceWiki = cur.SourceWiki;
        _settings.SearchSourceHabr = cur.SourceHabr;
        _settings.SearchSourceGitHub = cur.SourceGitHub;
        _settings.SearchSourceKinopoisk = cur.SourceKinopoisk;
        _settings.SearchSourceYouTube = cur.SourceYouTube;
        _settings.SearchSourceVK = cur.SourceVK;
        _settings.SearchSiteFilter = cur.SiteFilter;

        _settings.WallpaperPresetIndex = cur.WallpaperPreset;
        _settings.CustomWallpaperPath = cur.WallpaperCustomPath;
        _settings.CustomWallpaperUrl = cur.WallpaperCustomPath;
        _settings.HomeBgImageOpacity = cur.WallpaperDimming / 100.0;
        if (_sWallpaperPreset?.SelectedItem is ComboBoxItem wpItem)
        {
            var wpChoice = wpItem.Tag?.ToString() ?? "wallpaper-default.jpg";
            if (wpChoice == "custom")
            {
                wpChoice = !string.IsNullOrWhiteSpace(_sCustomWpPath?.Text) ? _sCustomWpPath.Text.Trim() : "wallpaper-default.jpg";
                _settings.CustomWallpaperPath = wpChoice;
                _settings.CustomWallpaperUrl = wpChoice;
                _settings.WallpaperPresetIndex = -1;
            }
            else
            {
                _settings.WallpaperPresetIndex = _sWallpaperPreset.SelectedIndex;
            }
            _settings.HomeBackgroundImage = wpChoice;
        }

        _settings.Theme = cur.Theme;
        _settings.AccentColor = cur.AccentColor;
        _settings.HtmlAccentColor = cur.AccentColor;
        _settings.HtmlBackgroundColor = cur.HtmlBgColor;
        _settings.CustomTitleBarColor = cur.CustomTitleBarColor;
        _settings.CustomNavBarColor = cur.CustomNavBarColor;
        _settings.CustomAddressBarColor = cur.CustomAddressBarColor;
        _settings.CustomCardColor = cur.CustomCardColor;
        _settings.CustomPopupColor = cur.CustomPopupColor;
        _settings.CustomBorderColor = cur.CustomBorderColor;
        _settings.CustomTextColor = cur.CustomTextColor;
        _settings.CustomTextSecondaryColor = cur.CustomTextSecColor;
        _settings.CustomGlowColor = cur.CustomGlowColor;
        _settings.CustomSidebarColor = cur.CustomSidebarColor;
        _settings.CustomAddressBarGradient = cur.CustomAddressBarGradient;
        _settings.CustomTitleBarGradient = cur.CustomTitleBarGradient;
        _settings.CustomNavBarGradient = cur.CustomNavBarGradient;
        _settings.CustomCardGradient = cur.CustomCardGradient;
        _settings.CustomSidebarGradient = cur.CustomSidebarGradient;
        ApplyAllGradients();
        _settings.TabRadius = cur.TabRadius;
        _settings.AnimationsEnabled = cur.Glow;
        _settings.ReduceAnimations = !cur.Animations;
        _settings.ShowBookmarksBar = cur.BookmarksBar;
        _settings.ShowStatusBar = cur.StatusBar;

        _settings.BlockAds = cur.BlockAds;
        _settings.BlockTrackers = cur.BlockTrackers;
        _settings.BlockCookieBanners = cur.BlockCookies;
        _settings.ForceHttps = cur.ForceHttps;
        _settings.BlockPopups = cur.BlockPopups;
        _settings.EnablePhishingProtection = cur.PhishingProtection;
        _settings.BlockFingerprinting = cur.BlockFingerprinting;
        _settings.SendDoNotTrack = cur.SendDNT;
        _settings.BlockAutoplay = cur.BlockAutoplay;
        _settings.BlockCryptomining = cur.BlockCrypto;

        _settings.RamSaverEnabled = cur.RamSaver;
        _settings.RamLimitThresholdMB = cur.RamLimit;
        _settings.GpuAcceleration = cur.GpuAcceleration;
        _settings.PreloadPages = cur.PreloadPages;

        _settings.ClearOnExit = cur.ClearOnExit;
        _settings.OpenDevToolsOnF12 = cur.DevToolsF12;
        _settings.AskDownloadLocation = cur.AskDownload;
        _settings.EnablePasswordAutofill = cur.PasswordAutofill;
        _settings.AntiStealerProtection = cur.AntiStealer;
        _settings.AntiLeakProtection = cur.AntiLeak;
        _settings.UniversalPasswordAutofill = cur.UniversalPassAutofill;
        _settings.EnableSiteStealthIsolation = cur.EnableSiteStealth;
        _settings.StealthIsolationDomains = cur.StealthDomains;
        _settings.StealthBlockGeo = cur.StealthBlockGeo;
        _settings.StealthBlockFingerprint = cur.StealthBlockFingerprint;
        _settings.StealthBlockSensorsAndHardware = cur.StealthBlockSensors;
        _settings.StealthBlockWebRTC = cur.StealthBlockWebRTC;
        _settings.StealthStripTrackingHeaders = cur.StealthStripTracking;

        _settings.ProxyEnabled = cur.ProxyEnabled;
        _settings.ProxyType = cur.ProxyType;
        _settings.ProxyHost = cur.ProxyHost;
        _settings.ProxyPort = cur.ProxyPort;
        _settings.ProxyUsername = cur.ProxyUsername;
        _settings.ProxyPassword = cur.ProxyPassword;
        _settings.ProxyBypassList = cur.ProxyBypassList;
        _settings.SelectedProxyPresetId = cur.SelectedProxyPresetId;

        if (_settings.ProxyEnabled && !string.IsNullOrWhiteSpace(_settings.ProxyHost))
        {
            global::NovaBrowser.Helpers.NovaProxyBridge.SetUpstream(
                _settings.ProxyType,
                _settings.ProxyHost,
                _settings.ProxyPort > 0 ? _settings.ProxyPort : 8080,
                _settings.ProxyUsername,
                _settings.ProxyPassword);
        }
        else
        {
            global::NovaBrowser.Helpers.NovaProxyBridge.SetUpstream(null, null, 0);
        }

        PushAdBlockerStateToAllTabs();
        try { if (_settings.RestoreLastSession) SaveSession(); } catch { }
        SaveSettings();
        try
        {
            _settings.Save();
        }
        catch { }

        ApplyBookmarksBarVisibility();
        UpdateBookmarksBar();
        ApplyZoomToActiveTabs();
        ApplyTabRadius();
        LoadWallpaper();
        ApplyVisualThemeToBrowser(cur.Theme, cur.AccentColor, cur.HtmlBgColor);

        PushThemeSettingsToAllTabs(cur.AccentColor, cur.HtmlBgColor);
        PushSearchSettingsToAllTabs();
        UpdateProxyIndicator();

        _savedSnapshot = CaptureCurrentSnapshot();
        SetDirtyState(false);
        ShowToast("Все настройки сохранены и применены");
    }

    private void ApplyVisualThemeToBrowser(string themeName, string accentHex, string? bgHex = null)
    {
        try
        {
            ApplyTheme(themeName);
        }
        catch { }

        try
        {
            ApplyAccentColor(accentHex);
        }
        catch { }

        try
        {
            HighlightSidebarButton(_btnNavGeneral, _activeSettingsSection == "General");
            HighlightSidebarButton(_btnNavAccounts, _activeSettingsSection == "Accounts");
            HighlightSidebarButton(_btnNavAppearance, _activeSettingsSection == "Appearance");
            HighlightSidebarButton(_btnNavSecurity, _activeSettingsSection == "Security");
            HighlightSidebarButton(_btnNavProxy, _activeSettingsSection == "Proxy");
            HighlightSidebarButton(_btnNavPerformance, _activeSettingsSection == "Performance");
            HighlightSidebarButton(_btnNavMisc, _activeSettingsSection == "Misc");
        }
        catch { }

        try
        {
            ApplyStatusBarVisibility();
            ApplyCompactMode();
            ApplyWindowOpacity();
            UpdateTabsUI();
        }
        catch { }

        try
        {
            PushThemeSettingsToAllTabs(accentHex, bgHex ?? _settings.HtmlBackgroundColor);
        }
        catch { }
    }

    private void OnMainWindowClosing_SaveSettings(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isFullResetting) return;
        try
        {
            _settings?.Save();
        }
        catch { }
    }

    private void PerformFullReset()
    {
        PerformFullResetAndRelaunch();
    }

    private void PerformFullResetAndRelaunch()
    {
        try
        {
            _isFullResetting = true;

            var localData = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovaBrowser");
            var userDataDir = System.IO.Path.Combine(localData, "UserData");
            var privateDataDir = System.IO.Path.Combine(localData, "Private");
            var exeDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');

            if (Directory.Exists(localData))
            {
                try
                {
                    foreach (var file in Directory.GetFiles(localData, "*.json"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    var customTheme = System.IO.Path.Combine(localData, "CustomTheme.xaml");
                    if (File.Exists(customTheme)) { try { File.Delete(customTheme); } catch { } }
                    var openUrl = System.IO.Path.Combine(localData, "open.url");
                    if (File.Exists(openUrl)) { try { File.Delete(openUrl); } catch { } }
                }
                catch { }
            }

            try
            {
                string[] jsonFiles = { "settings.json", "history.json", "passwords.json", "accounts.json", "bookmarks.json" };
                foreach (var name in jsonFiles)
                {
                    var p = System.IO.Path.Combine(exeDir, name);
                    if (File.Exists(p))
                    {
                        try { File.Delete(p); } catch { }
                    }
                }
            }
            catch { }

            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                try { exePath = Process.GetCurrentProcess().MainModule?.FileName; } catch { }
            }
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                exePath = System.IO.Path.Combine(exeDir, "NovaBrowser.exe");
            }

            foreach (var tab in _tabs.ToList())
            {
                try { tab.WebViewControl?.Dispose(); } catch { }
                tab.WebViewControl = null;
            }

            var pid = Process.GetCurrentProcess().Id;
            var tempBat = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nova_reset_{Guid.NewGuid():N}.cmd");
            var batContent =
                "@echo off\r\n" +
                "ping 127.0.0.1 -n 2 >nul\r\n" +
                $"taskkill /F /PID {pid} >nul 2>&1\r\n" +
                $"if exist \"{userDataDir}\" rmdir /S /Q \"{userDataDir}\" >nul 2>&1\r\n" +
                $"if exist \"{privateDataDir}\" rmdir /S /Q \"{privateDataDir}\" >nul 2>&1\r\n" +
                $"if exist \"{localData}\" del /F /Q /A \"{localData}\\*.json\" >nul 2>&1\r\n" +
                $"if exist \"{localData}\\CustomTheme.xaml\" del /F /Q \"{localData}\\CustomTheme.xaml\" >nul 2>&1\r\n" +
                $"if exist \"{localData}\\open.url\" del /F /Q \"{localData}\\open.url\" >nul 2>&1\r\n" +
                $"if exist \"{exeDir}\\settings.json\" del /F /Q \"{exeDir}\\settings.json\" >nul 2>&1\r\n" +
                $"if exist \"{exeDir}\\history.json\" del /F /Q \"{exeDir}\\history.json\" >nul 2>&1\r\n" +
                $"if exist \"{exeDir}\\passwords.json\" del /F /Q \"{exeDir}\\passwords.json\" >nul 2>&1\r\n" +
                $"if exist \"{exeDir}\\accounts.json\" del /F /Q \"{exeDir}\\accounts.json\" >nul 2>&1\r\n" +
                $"if exist \"{exeDir}\\bookmarks.json\" del /F /Q \"{exeDir}\\bookmarks.json\" >nul 2>&1\r\n" +
                $"start \"\" \"{exePath}\"\r\n" +
                "del \"%~f0\" >nul 2>&1\r\n";

            try
            {
                File.WriteAllText(tempBat, batContent);
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{tempBat}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
            }
            catch
            {
                try
                {
                    var directPsi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    };
                    Process.Start(directPsi);
                }
                catch { }
            }

            Environment.Exit(0);
        }
        catch
        {
            try { Environment.Exit(0); } catch { }
        }
    }

    private void OnCloseSettingsClicked()
    {
        var settingsTab = _tabs.FirstOrDefault(t => t.Url == "nova://settings");
        if (settingsTab != null && _tabs.Count > 1)
        {
            if (_isSettingsDirty)
            {
                ShowUnsavedModal(() => CloseTab(settingsTab));
            }
            else
            {
                CloseTab(settingsTab);
            }
        }
        else
        {
            if (_isSettingsDirty)
            {
                ShowUnsavedModal(() =>
                {
                    if (SettingsPage != null) SettingsPage.Visibility = Visibility.Collapsed;
                    if (_activeTab != null) _ = ShowNovaHomeAsync(_activeTab);
                });
            }
            else
            {
                if (SettingsPage != null) SettingsPage.Visibility = Visibility.Collapsed;
                if (_activeTab != null) _ = ShowNovaHomeAsync(_activeTab);
            }
        }
    }

    private async void ShowToast(string message)
    {
        if (_savedToastBanner == null) return;
        if (_savedToastBanner.Child is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
        {
            tb.Text = message;
        }

        _savedToastBanner.Visibility = Visibility.Visible;
        await System.Threading.Tasks.Task.Delay(2600);
        _savedToastBanner.Visibility = Visibility.Collapsed;
    }

    private Border BuildGradientCustomizerModal()
    {
        var overlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, 6, 8, 14)),
            Visibility = Visibility.Collapsed
        };
        _gradientModal = overlay;

        var card = new Border
        {
            Width = 660,
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = GetThemeBrush("BgCardBrush", Color.FromRgb(20, 24, 34)),
            BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
            BorderThickness = new Thickness(1)
        };
        try { card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 32, ShadowDepth = 10, Opacity = 0.75, Color = Colors.Black }; } catch { }

        var stack = new StackPanel();

        var topRow = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _gradTitleText = new TextBlock
        {
            Text = "Конструктор градиента элемента",
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        Grid.SetColumn(_gradTitleText, 0);
        topRow.Children.Add(_gradTitleText);

        var btnClose = new Button
        {
            Content = "✕",
            FontSize = 14,
            Width = 30,
            Height = 30,
            Cursor = Cursors.Hand
        };
        StyleDarkButton(btnClose, false);
        btnClose.Click += (_, _) => _gradientModal.Visibility = Visibility.Collapsed;
        Grid.SetColumn(btnClose, 1);
        topRow.Children.Add(btnClose);
        stack.Children.Add(topRow);

        var descText = new TextBlock
        {
            Text = "Кликните на интерактивную шкалу градиента в любой точке, чтобы добавить новый цвет (до 5 цветов). Выберите стороны перелива и настройте плавный переход.",
            FontSize = 12,
            Foreground = GetThemeBrush("TextSecondaryBrush", Color.FromRgb(150, 160, 185)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        };
        stack.Children.Add(descText);

        var previewLabel = new TextBlock
        {
            Text = "Предпросмотр элемента:",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White),
            Margin = new Thickness(0, 0, 0, 6)
        };
        stack.Children.Add(previewLabel);

        _gradPreviewBox = new Border
        {
            Height = 44,
            CornerRadius = new CornerRadius(10),
            BorderBrush = GetThemeBrush("BorderBrush", Color.FromRgb(60, 70, 95)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 16),
            Padding = new Thickness(14, 0, 14, 0)
        };
        var previewInner = new Grid();
        previewInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        previewInner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var prevLock = new TextBlock
        {
            Text = "🔒",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        var prevUrl = new TextBlock
        {
            Text = "https://nova.browser/custom-gradient-preview",
            FontSize = 12,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White),
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Cascadia Code, Consolas, monospace")
        };
        Grid.SetColumn(prevLock, 0);
        Grid.SetColumn(prevUrl, 1);
        previewInner.Children.Add(prevLock);
        previewInner.Children.Add(prevUrl);
        _gradPreviewBox.Child = previewInner;
        stack.Children.Add(_gradPreviewBox);

        var trackLabel = new TextBlock
        {
            Text = "Интерактивная шкала градиента (кликните, чтобы добавить цвет):",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White),
            Margin = new Thickness(0, 0, 0, 6)
        };
        stack.Children.Add(trackLabel);

        _gradTrackBar = new Border
        {
            Height = 40,
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            BorderThickness = new Thickness(1.5),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 0, 10)
        };
        _gradTrackBar.PreviewMouseLeftButtonDown += (_, e) =>
        {
            Point pos = e.GetPosition(_gradTrackBar);
            double w = _gradTrackBar.ActualWidth;
            double offset = Math.Clamp(Math.Round(pos.X / (w > 0 ? w : 1), 2), 0.0, 1.0);
            if (_gradStops.Count >= 5)
            {
                return;
            }
            OpenRgbColorPicker($"Цвет точки {Math.Round(offset * 100)}%", "#BEF47B", hex =>
            {
                var col = ParseHexColor(hex, Color.FromRgb(190, 244, 123));
                _gradStops.Add((col, offset));
                _gradStops.Sort((a, b) => a.offset.CompareTo(b.offset));
                UpdateGradientVisuals();
            });
        };
        stack.Children.Add(_gradTrackBar);

        _gradStopsChipsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 14)
        };
        stack.Children.Add(_gradStopsChipsPanel);

        var dirTitle = new TextBlock
        {
            Text = "Направление градиента:",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White),
            Margin = new Thickness(0, 0, 0, 6)
        };
        stack.Children.Add(dirTitle);

        var dirButtonsPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        void AddDirBtn(string label, double angle)
        {
            var btn = new Button
            {
                Content = label,
                Height = 28,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand
            };
            StyleDarkButton(btn, false);
            btn.Click += (_, _) =>
            {
                _gradAngle = angle;
                if (_gradAngleSlider != null) _gradAngleSlider.Value = angle;
                UpdateGradientVisuals();
            };
            dirButtonsPanel.Children.Add(btn);
        }
        AddDirBtn("→ Слева направо", 0);
        AddDirBtn("↓ Сверху вниз", 90);
        AddDirBtn("↘ По диагонали", 45);
        AddDirBtn("↗ Диагональ 2", 135);
        AddDirBtn("← Справа налево", 180);
        AddDirBtn("↑ Снизу вверх", 270);
        stack.Children.Add(dirButtonsPanel);

        var angleRow = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        angleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        angleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _gradAngleSlider = new Slider
        {
            Minimum = 0,
            Maximum = 360,
            Value = _gradAngle,
            TickFrequency = 15,
            IsSnapToTickEnabled = false,
            VerticalAlignment = VerticalAlignment.Center
        };
        _gradAngleValueText = new TextBlock
        {
            Text = $"{_gradAngle}°",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Width = 45
        };
        _gradAngleSlider.ValueChanged += (_, e) =>
        {
            _gradAngle = Math.Round(e.NewValue);
            if (_gradAngleValueText != null) _gradAngleValueText.Text = $"{_gradAngle}°";
            UpdateGradientVisuals();
        };
        Grid.SetColumn(_gradAngleSlider, 0);
        Grid.SetColumn(_gradAngleValueText, 1);
        angleRow.Children.Add(_gradAngleSlider);
        angleRow.Children.Add(_gradAngleValueText);
        stack.Children.Add(angleRow);

        var presetsRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
        void AddPresetBtn(string name, (Color col, double off)[] stops, double angle)
        {
            var btn = new Button
            {
                Content = name,
                Height = 26,
                Padding = new Thickness(8, 0, 8, 0),
                Margin = new Thickness(0, 0, 6, 6),
                FontSize = 11,
                Cursor = Cursors.Hand
            };
            StyleDarkButton(btn, false);
            btn.Click += (_, _) =>
            {
                _gradStops = new List<(Color color, double offset)>(stops);
                _gradAngle = angle;
                if (_gradAngleSlider != null) _gradAngleSlider.Value = angle;
                UpdateGradientVisuals();
            };
            presetsRow.Children.Add(btn);
        }
        AddPresetBtn("Lime Aurora", new[] { (Color.FromRgb(10, 13, 20), 0.0), (Color.FromRgb(24, 49, 33), 0.5), (Color.FromRgb(190, 244, 123), 1.0) }, 90);
        AddPresetBtn("Cyber Mint", new[] { (Color.FromRgb(10, 12, 20), 0.0), (Color.FromRgb(20, 56, 42), 0.45), (Color.FromRgb(78, 250, 150), 1.0) }, 45);
        AddPresetBtn("Obsidian Gold", new[] { (Color.FromRgb(8, 9, 13), 0.0), (Color.FromRgb(38, 30, 20), 0.5), (Color.FromRgb(226, 179, 87), 1.0) }, 135);
        AddPresetBtn("Deep Space", new[] { (Color.FromRgb(5, 8, 17), 0.0), (Color.FromRgb(18, 35, 64), 0.5), (Color.FromRgb(58, 96, 115), 1.0) }, 90);
        AddPresetBtn("Sunset Violet", new[] { (Color.FromRgb(14, 7, 23), 0.0), (Color.FromRgb(59, 19, 72), 0.5), (Color.FromRgb(190, 244, 123), 1.0) }, 45);
        stack.Children.Add(presetsRow);

        var bottomActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var btnApplyGrad = new Button
        {
            Content = "Применить градиент",
            Height = 34,
            Padding = new Thickness(16, 0, 16, 0),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        StyleDarkButton(btnApplyGrad, true);
        btnApplyGrad.Click += (_, _) =>
        {
            ApplyCurrentGradient();
            _gradientModal.Visibility = Visibility.Collapsed;
        };

        var btnResetGrad = new Button
        {
            Content = "Сбросить на сплошной",
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        StyleDarkButton(btnResetGrad, false);
        btnResetGrad.Click += (_, _) =>
        {
            ResetCurrentGradient();
            _gradientModal.Visibility = Visibility.Collapsed;
        };

        var btnCancelGrad = new Button
        {
            Content = "Отмена",
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            Cursor = Cursors.Hand
        };
        StyleDarkButton(btnCancelGrad, false);
        btnCancelGrad.Click += (_, _) => _gradientModal.Visibility = Visibility.Collapsed;

        bottomActions.Children.Add(btnApplyGrad);
        bottomActions.Children.Add(btnResetGrad);
        bottomActions.Children.Add(btnCancelGrad);
        stack.Children.Add(bottomActions);

        card.Child = stack;
        overlay.Child = card;
        return overlay;
    }

    private void OpenGradientCustomizer(string elementName, string resourceKey)
    {
        if (_gradientModal == null) return;
        _gradTargetLabel = elementName;
        _gradTargetResource = resourceKey;
        if (_gradTitleText != null) _gradTitleText.Text = "Конструктор градиента: " + elementName;

        string existingGrad = "";
        if (resourceKey == "BgInputBrush") existingGrad = _settings.CustomAddressBarGradient;
        else if (resourceKey == "TitleBarBrush") existingGrad = _settings.CustomTitleBarGradient;
        else if (resourceKey == "NavBarBrush") existingGrad = _settings.CustomNavBarGradient;
        else if (resourceKey == "BgCardBrush") existingGrad = _settings.CustomCardGradient;
        else if (resourceKey == "BgSurfaceBrush") existingGrad = _settings.CustomSidebarGradient;

        var existingBrush = ParseGradientString(existingGrad);
        if (existingBrush != null)
        {
            _gradStops.Clear();
            foreach (var s in existingBrush.GradientStops)
            {
                _gradStops.Add((s.Color, s.Offset));
            }
        }
        else
        {
            _gradStops.Clear();
            _gradStops.Add((Color.FromRgb(10, 13, 20), 0.0));
            _gradStops.Add((Color.FromRgb(190, 244, 123), 1.0));
            _gradAngle = 90;
        }

        if (_gradAngleSlider != null) _gradAngleSlider.Value = _gradAngle;
        UpdateGradientVisuals();
        _gradientModal.Visibility = Visibility.Visible;
    }

    private void UpdateGradientVisuals()
    {
        if (_gradTrackBar == null || _gradPreviewBox == null) return;
        double rad = _gradAngle * Math.PI / 180.0;
        double x1 = 0.5 - 0.5 * Math.Cos(rad);
        double y1 = 0.5 - 0.5 * Math.Sin(rad);
        double x2 = 0.5 + 0.5 * Math.Cos(rad);
        double y2 = 0.5 + 0.5 * Math.Sin(rad);
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(Math.Round(x1, 3), Math.Round(y1, 3)),
            EndPoint = new Point(Math.Round(x2, 3), Math.Round(y2, 3))
        };
        foreach (var s in _gradStops)
        {
            brush.GradientStops.Add(new GradientStop(s.color, s.offset));
        }

        var trackHorizBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5)
        };
        foreach (var s in _gradStops)
        {
            trackHorizBrush.GradientStops.Add(new GradientStop(s.color, s.offset));
        }

        _gradTrackBar.Background = trackHorizBrush;
        _gradPreviewBox.Background = brush;

        if (_gradStopsChipsPanel != null)
        {
            _gradStopsChipsPanel.Children.Clear();
            for (int i = 0; i < _gradStops.Count; i++)
            {
                int stopIdx = i;
                var (col, off) = _gradStops[i];
                var chip = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 0, 6, 0)
                };
                var chipStack = new StackPanel { Orientation = Orientation.Horizontal };
                var colorDot = new Border
                {
                    Width = 14,
                    Height = 14,
                    CornerRadius = new CornerRadius(7),
                    Background = new SolidColorBrush(col),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 6, 0),
                    Cursor = Cursors.Hand
                };
                colorDot.MouseLeftButtonDown += (_, _) =>
                {
                    OpenRgbColorPicker($"Изменить цвет точки {Math.Round(off * 100)}%", $"#{col.R:X2}{col.G:X2}{col.B:X2}", hex =>
                    {
                        var newCol = ParseHexColor(hex, col);
                        _gradStops[stopIdx] = (newCol, off);
                        UpdateGradientVisuals();
                    });
                };
                var offText = new TextBlock
                {
                    Text = $"{Math.Round(off * 100)}%",
                    FontSize = 11,
                    Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                chipStack.Children.Add(colorDot);
                chipStack.Children.Add(offText);

                if (_gradStops.Count > 2)
                {
                    var delBtn = new TextBlock
                    {
                        Text = "✕",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Cursor = Cursors.Hand
                    };
                    delBtn.MouseLeftButtonDown += (_, _) =>
                    {
                        _gradStops.RemoveAt(stopIdx);
                        UpdateGradientVisuals();
                    };
                    chipStack.Children.Add(delBtn);
                }

                chip.Child = chipStack;
                _gradStopsChipsPanel.Children.Add(chip);
            }
        }
    }

    private void ApplyCurrentGradient()
    {
        double rad = _gradAngle * Math.PI / 180.0;
        double x1 = 0.5 - 0.5 * Math.Cos(rad);
        double y1 = 0.5 - 0.5 * Math.Sin(rad);
        double x2 = 0.5 + 0.5 * Math.Cos(rad);
        double y2 = 0.5 + 0.5 * Math.Sin(rad);
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(Math.Round(x1, 3), Math.Round(y1, 3)),
            EndPoint = new Point(Math.Round(x2, 3), Math.Round(y2, 3))
        };
        foreach (var s in _gradStops)
        {
            brush.GradientStops.Add(new GradientStop(s.color, s.offset));
        }

        string gradStr = $"{_gradAngle};{string.Join(";", _gradStops.Select(s => $"#{s.color.R:X2}{s.color.G:X2}{s.color.B:X2}@{s.offset:F2}"))}";

        if (!string.IsNullOrEmpty(_gradTargetResource))
        {
            Application.Current.Resources[_gradTargetResource] = brush;
        }

        if (_gradTargetResource == "BgInputBrush")
        {
            _settings.CustomAddressBarGradient = gradStr;
            if (AddressBox != null) AddressBox.Background = brush;
        }
        else if (_gradTargetResource == "TitleBarBrush")
        {
            _settings.CustomTitleBarGradient = gradStr;
            if (TitleBarGrid != null) TitleBarGrid.Background = brush;
        }
        else if (_gradTargetResource == "NavBarBrush")
        {
            _settings.CustomNavBarGradient = gradStr;
            if (NavBarGrid != null) NavBarGrid.Background = brush;
        }
        else if (_gradTargetResource == "BgCardBrush")
        {
            _settings.CustomCardGradient = gradStr;
        }
        else if (_gradTargetResource == "BgSurfaceBrush")
        {
            _settings.CustomSidebarGradient = gradStr;
        }

        OnInputChanged();
    }

    private void ResetCurrentGradient()
    {
        if (_gradTargetResource == "BgInputBrush") _settings.CustomAddressBarGradient = "";
        else if (_gradTargetResource == "TitleBarBrush") _settings.CustomTitleBarGradient = "";
        else if (_gradTargetResource == "NavBarBrush") _settings.CustomNavBarGradient = "";
        else if (_gradTargetResource == "BgCardBrush") _settings.CustomCardGradient = "";
        else if (_gradTargetResource == "BgSurfaceBrush") _settings.CustomSidebarGradient = "";
        ApplyTheme(_settings?.Theme ?? "Dark");
        OnInputChanged();
    }

    public static LinearGradientBrush? ParseGradientString(string? gradStr)
    {
        if (string.IsNullOrWhiteSpace(gradStr) || !gradStr.Contains(';')) return null;
        var parts = gradStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double angle))
            angle = 90;
        double rad = angle * Math.PI / 180.0;
        double x1 = 0.5 - 0.5 * Math.Cos(rad);
        double y1 = 0.5 - 0.5 * Math.Sin(rad);
        double x2 = 0.5 + 0.5 * Math.Cos(rad);
        double y2 = 0.5 + 0.5 * Math.Sin(rad);
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(Math.Round(x1, 3), Math.Round(y1, 3)),
            EndPoint = new Point(Math.Round(x2, 3), Math.Round(y2, 3))
        };
        for (int i = 1; i < parts.Length; i++)
        {
            var stopParts = parts[i].Split('@');
            if (stopParts.Length == 2)
            {
                try
                {
                    var col = (Color)ColorConverter.ConvertFromString(stopParts[0].Trim());
                    if (double.TryParse(stopParts[1].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double offset))
                    {
                        brush.GradientStops.Add(new GradientStop(col, Math.Clamp(offset, 0.0, 1.0)));
                    }
                }
                catch { }
            }
        }
        return brush.GradientStops.Count > 0 ? brush : null;
    }

    private void ApplyAllGradients()
    {
        if (!string.IsNullOrEmpty(_settings.CustomAddressBarGradient))
        {
            var b = ParseGradientString(_settings.CustomAddressBarGradient);
            if (b != null)
            {
                Application.Current.Resources["BgInputBrush"] = b;
                if (AddressBox != null) AddressBox.Background = b;
            }
        }
        if (!string.IsNullOrEmpty(_settings.CustomTitleBarGradient))
        {
            var b = ParseGradientString(_settings.CustomTitleBarGradient);
            if (b != null)
            {
                Application.Current.Resources["TitleBarBrush"] = b;
                if (TitleBarGrid != null) TitleBarGrid.Background = b;
            }
        }
        if (!string.IsNullOrEmpty(_settings.CustomNavBarGradient))
        {
            var b = ParseGradientString(_settings.CustomNavBarGradient);
            if (b != null)
            {
                Application.Current.Resources["NavBarBrush"] = b;
                if (NavBarGrid != null) NavBarGrid.Background = b;
            }
        }
        if (!string.IsNullOrEmpty(_settings.CustomCardGradient))
        {
            var b = ParseGradientString(_settings.CustomCardGradient);
            if (b != null) Application.Current.Resources["BgCardBrush"] = b;
        }
        if (!string.IsNullOrEmpty(_settings.CustomSidebarGradient))
        {
            var b = ParseGradientString(_settings.CustomSidebarGradient);
            if (b != null) Application.Current.Resources["BgSurfaceBrush"] = b;
        }
    }

    private Border BuildColorPickerModal()
    {
        var overlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            Visibility = Visibility.Collapsed
        };
        _colorPickerModal = overlay;

        var card = new Border
        {
            Width = 480,
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(22),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = GetThemeBrush("BgCardBrush", Color.FromRgb(24, 26, 32)),
            BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
            BorderThickness = new Thickness(1)
        };
        try { card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.65, Color = Colors.Black }; } catch { }

        var stack = new StackPanel();

        var topRow = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _cpTitleText = new TextBlock
        {
            Text = "Выбор цвета (RGB / Палитра)",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        Grid.SetColumn(_cpTitleText, 0);
        topRow.Children.Add(_cpTitleText);

        var btnClose = new Button
        {
            Content = "✕",
            FontSize = 14,
            Width = 28,
            Height = 28,
            Cursor = Cursors.Hand,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnClose);
        btnClose.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(_colorPickerInitialHex))
                _colorPickerOnLive?.Invoke(_colorPickerInitialHex);
            overlay.Visibility = Visibility.Collapsed;
        };
        Grid.SetColumn(btnClose, 1);
        topRow.Children.Add(btnClose);
        stack.Children.Add(topRow);

        var previewGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var oldBox = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        oldBox.Children.Add(new TextBlock { Text = "Текущий цвет:", FontSize = 11, Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(150, 150, 150)), Margin = new Thickness(0, 0, 0, 4) });
        _cpOldColorPreview = new Border { Height = 42, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)) };
        oldBox.Children.Add(_cpOldColorPreview);
        Grid.SetColumn(oldBox, 0);
        previewGrid.Children.Add(oldBox);

        var newBox = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        newBox.Children.Add(new TextBlock { Text = "Новый цвет (Live):", FontSize = 11, Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(150, 150, 150)), Margin = new Thickness(0, 0, 0, 4) });
        _cpNewColorPreview = new Border { Height = 42, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)) };
        newBox.Children.Add(_cpNewColorPreview);
        Grid.SetColumn(newBox, 1);
        previewGrid.Children.Add(newBox);
        stack.Children.Add(previewGrid);

        var slidersStack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };

        var rowR = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        rowR.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        rowR.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowR.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
        var lblR = new TextBlock { Text = "R", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(255, 85, 85)), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(lblR, 0);
        rowR.Children.Add(lblR);
        _cpSliderR = new Slider { Minimum = 0, Maximum = 255, SmallChange = 1, LargeChange = 16, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) };
        Grid.SetColumn(_cpSliderR, 1);
        rowR.Children.Add(_cpSliderR);
        _cpTextR = new TextBox { Width = 50, Height = 28, TextAlignment = TextAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_cpTextR);
        Grid.SetColumn(_cpTextR, 2);
        rowR.Children.Add(_cpTextR);
        slidersStack.Children.Add(rowR);

        var rowG = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        rowG.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        rowG.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowG.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
        var lblG = new TextBlock { Text = "G", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(85, 255, 120)), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(lblG, 0);
        rowG.Children.Add(lblG);
        _cpSliderG = new Slider { Minimum = 0, Maximum = 255, SmallChange = 1, LargeChange = 16, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) };
        Grid.SetColumn(_cpSliderG, 1);
        rowG.Children.Add(_cpSliderG);
        _cpTextG = new TextBox { Width = 50, Height = 28, TextAlignment = TextAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_cpTextG);
        Grid.SetColumn(_cpTextG, 2);
        rowG.Children.Add(_cpTextG);
        slidersStack.Children.Add(rowG);

        var rowB = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        rowB.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        rowB.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowB.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
        var lblB = new TextBlock { Text = "B", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(85, 180, 255)), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(lblB, 0);
        rowB.Children.Add(lblB);
        _cpSliderB = new Slider { Minimum = 0, Maximum = 255, SmallChange = 1, LargeChange = 16, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) };
        Grid.SetColumn(_cpSliderB, 1);
        rowB.Children.Add(_cpSliderB);
        _cpTextB = new TextBox { Width = 50, Height = 28, TextAlignment = TextAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_cpTextB);
        Grid.SetColumn(_cpTextB, 2);
        rowB.Children.Add(_cpTextB);
        slidersStack.Children.Add(rowB);

        stack.Children.Add(slidersStack);

        bool updatingInternally = false;
        void UpdateFromSliders()
        {
            if (updatingInternally) return;
            updatingInternally = true;
            try
            {
                byte r = (byte)Math.Clamp((int)(_cpSliderR?.Value ?? 0), 0, 255);
                byte g = (byte)Math.Clamp((int)(_cpSliderG?.Value ?? 0), 0, 255);
                byte b = (byte)Math.Clamp((int)(_cpSliderB?.Value ?? 0), 0, 255);
                if (_cpTextR != null) _cpTextR.Text = r.ToString();
                if (_cpTextG != null) _cpTextG.Text = g.ToString();
                if (_cpTextB != null) _cpTextB.Text = b.ToString();
                var hex = $"#{r:X2}{g:X2}{b:X2}";
                if (_cpTextHex != null) _cpTextHex.Text = hex;
                if (_cpNewColorPreview != null) _cpNewColorPreview.Background = new SolidColorBrush(Color.FromRgb(r, g, b));
                _colorPickerOnLive?.Invoke(hex);
            }
            finally { updatingInternally = false; }
        }

        void UpdateFromHex(string hex)
        {
            if (updatingInternally) return;
            updatingInternally = true;
            try
            {
                var col = ParseHexColor(hex, Color.FromRgb(190, 244, 123));
                if (_cpSliderR != null) _cpSliderR.Value = col.R;
                if (_cpSliderG != null) _cpSliderG.Value = col.G;
                if (_cpSliderB != null) _cpSliderB.Value = col.B;
                if (_cpTextR != null) _cpTextR.Text = col.R.ToString();
                if (_cpTextG != null) _cpTextG.Text = col.G.ToString();
                if (_cpTextB != null) _cpTextB.Text = col.B.ToString();
                if (_cpNewColorPreview != null) _cpNewColorPreview.Background = new SolidColorBrush(col);
                _colorPickerOnLive?.Invoke(hex);
            }
            finally { updatingInternally = false; }
        }

        _cpSliderR.ValueChanged += (_, _) => UpdateFromSliders();
        _cpSliderG.ValueChanged += (_, _) => UpdateFromSliders();
        _cpSliderB.ValueChanged += (_, _) => UpdateFromSliders();

        _cpTextR.TextChanged += (_, _) =>
        {
            if (updatingInternally) return;
            if (byte.TryParse(_cpTextR.Text, out var v) && _cpSliderR != null) _cpSliderR.Value = v;
        };
        _cpTextG.TextChanged += (_, _) =>
        {
            if (updatingInternally) return;
            if (byte.TryParse(_cpTextG.Text, out var v) && _cpSliderG != null) _cpSliderG.Value = v;
        };
        _cpTextB.TextChanged += (_, _) =>
        {
            if (updatingInternally) return;
            if (byte.TryParse(_cpTextB.Text, out var v) && _cpSliderB != null) _cpSliderB.Value = v;
        };

        var hexRow = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        hexRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hexRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var hexBoxStack = new StackPanel { Orientation = Orientation.Horizontal };
        hexBoxStack.Children.Add(new TextBlock { Text = "HEX:", FontWeight = FontWeights.Bold, Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        _cpTextHex = new TextBox { Width = 110, Height = 28, VerticalContentAlignment = VerticalAlignment.Center };
        StyleDarkTextBox(_cpTextHex);
        _cpTextHex.TextChanged += (_, _) =>
        {
            if (_cpTextHex.Text.Length >= 7) UpdateFromHex(_cpTextHex.Text);
        };
        hexBoxStack.Children.Add(_cpTextHex);
        Grid.SetColumn(hexBoxStack, 0);
        hexRow.Children.Add(hexBoxStack);

        var btnWinDialog = new Button
        {
            Content = "Палитра Windows...",
            Padding = new Thickness(10, 4, 10, 4),
            Cursor = Cursors.Hand,
            FontSize = 11,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnWinDialog);
        btnWinDialog.Click += (_, _) =>
        {
            try
            {
                var curCol = ParseHexColor(_cpTextHex?.Text ?? "#BEF47B", Color.FromRgb(190, 244, 123));
                var cd = new System.Windows.Forms.ColorDialog
                {
                    Color = System.Drawing.Color.FromArgb(curCol.R, curCol.G, curCol.B),
                    FullOpen = true
                };
                if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var hex = $"#{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}";
                    if (_cpTextHex != null) _cpTextHex.Text = hex;
                    UpdateFromHex(hex);
                }
            }
            catch { }
        };
        Grid.SetColumn(btnWinDialog, 1);
        hexRow.Children.Add(btnWinDialog);
        stack.Children.Add(hexRow);

        var swatchesTitle = new TextBlock
        {
            Text = "Быстрая палитра цветов:",
            FontSize = 11,
            Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(150, 150, 150)),
            Margin = new Thickness(0, 0, 0, 6)
        };
        stack.Children.Add(swatchesTitle);

        var quickPalette = new[]
        {
            "#BEF47B", "#A855F7", "#10B981", "#F59E0B", "#EF4444", "#3B82F6",
            "#EC4899", "#06B6D4", "#8B5CF6", "#14B8A6", "#84CC16", "#E11D48",
            "#07090D", "#000000", "#0E121A", "#141722", "#18122B", "#0A191E"
        };
        var swatchesWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 18) };
        foreach (var swHex in quickPalette)
        {
            var sw = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                Background = ParseHexBrush(swHex, Color.FromRgb(190, 244, 123))
            };
            var hx = swHex;
            sw.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;
                if (_cpTextHex != null) _cpTextHex.Text = hx;
                UpdateFromHex(hx);
            };
            swatchesWrap.Children.Add(sw);
        }
        stack.Children.Add(swatchesWrap);

        var actionsRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var btnApplyColor = new Button
        {
            Content = "Применить цвет",
            Padding = new Thickness(16, 7, 16, 7),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.Bold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnApplyColor, true);
        btnApplyColor.Click += (_, _) =>
        {
            var selectedHex = _cpTextHex?.Text?.Trim() ?? "#BEF47B";
            _colorPickerOnApply?.Invoke(selectedHex);
            overlay.Visibility = Visibility.Collapsed;
        };
        actionsRow.Children.Add(btnApplyColor);

        var btnCancelColor = new Button
        {
            Content = "Отмена",
            Padding = new Thickness(14, 7, 14, 7),
            Cursor = Cursors.Hand,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnCancelColor);
        btnCancelColor.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(_colorPickerInitialHex))
                _colorPickerOnLive?.Invoke(_colorPickerInitialHex);
            overlay.Visibility = Visibility.Collapsed;
        };
        actionsRow.Children.Add(btnCancelColor);

        stack.Children.Add(actionsRow);

        card.Child = stack;
        overlay.Child = card;
        return overlay;
    }

    private void OpenColorPicker(string title, string initialHex, Action<string> onApply, Action<string>? onLive = null) => OpenRgbColorPicker(title, initialHex, onApply, onLive);

    private void OpenRgbColorPicker(string title, string initialHex, Action<string> onApply, Action<string>? onLive = null)
    {
        if (_colorPickerModal == null) return;
        if (_cpTitleText != null) _cpTitleText.Text = title;
        _colorPickerOnApply = onApply;
        _colorPickerOnLive = onLive;
        _colorPickerInitialHex = initialHex;
        var startColor = ParseHexColor(initialHex, Color.FromRgb(190, 244, 123));
        if (_cpOldColorPreview != null) _cpOldColorPreview.Background = new SolidColorBrush(startColor);
        if (_cpNewColorPreview != null) _cpNewColorPreview.Background = new SolidColorBrush(startColor);
        if (_cpSliderR != null) _cpSliderR.Value = startColor.R;
        if (_cpSliderG != null) _cpSliderG.Value = startColor.G;
        if (_cpSliderB != null) _cpSliderB.Value = startColor.B;
        if (_cpTextR != null) _cpTextR.Text = startColor.R.ToString();
        if (_cpTextG != null) _cpTextG.Text = startColor.G.ToString();
        if (_cpTextB != null) _cpTextB.Text = startColor.B.ToString();
        if (_cpTextHex != null) _cpTextHex.Text = initialHex.StartsWith("#") ? initialHex : "#" + initialHex;
        _colorPickerModal.Visibility = Visibility.Visible;
    }

    private Border BuildClearDataModal()
    {
        var overlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            Visibility = Visibility.Collapsed
        };
        _clearDataModal = overlay;

        var card = new Border
        {
            Width = 500,
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = GetThemeBrush("BgCardBrush", Color.FromRgb(24, 26, 32)),
            BorderBrush = GetThemeBrush("AccentBrush", Color.FromRgb(190, 244, 123)),
            BorderThickness = new Thickness(1)
        };
        try { card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.65, Color = Colors.Black }; } catch { }

        var stack = new StackPanel();

        var topRow = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "Очистить историю и данные (Chrome / Chromium)",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        Grid.SetColumn(title, 0);
        topRow.Children.Add(title);

        var btnClose = new Button
        {
            Content = "✕",
            FontSize = 14,
            Width = 28,
            Height = 28,
            Cursor = Cursors.Hand,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnClose);
        btnClose.Click += (_, _) => { overlay.Visibility = Visibility.Collapsed; };
        Grid.SetColumn(btnClose, 1);
        topRow.Children.Add(btnClose);
        stack.Children.Add(topRow);

        var timeRow = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        timeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        timeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lblTime = new TextBlock
        {
            Text = "Временной диапазон:",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
        };
        Grid.SetColumn(lblTime, 0);
        timeRow.Children.Add(lblTime);

        _comboCleanTimeRange = new ComboBox { Height = 32 };
        StyleDarkComboBox(_comboCleanTimeRange);
        _comboCleanTimeRange.Items.Add(new ComboBoxItem { Content = "За всё время", Tag = 0, IsSelected = true });
        _comboCleanTimeRange.Items.Add(new ComboBoxItem { Content = "За последний час", Tag = 1 });
        _comboCleanTimeRange.Items.Add(new ComboBoxItem { Content = "За последние 24 часа", Tag = 24 });
        _comboCleanTimeRange.Items.Add(new ComboBoxItem { Content = "За последние 7 дней", Tag = 168 });
        _comboCleanTimeRange.Items.Add(new ComboBoxItem { Content = "За последние 4 недели", Tag = 672 });
        Grid.SetColumn(_comboCleanTimeRange, 1);
        timeRow.Children.Add(_comboCleanTimeRange);
        stack.Children.Add(timeRow);

        var itemsBox = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

        _chkCleanHistory = new CheckBox { Content = "История просмотров (очищает журнал переходов)", IsChecked = true, Margin = new Thickness(0, 0, 0, 10) };
        StyleDarkCheckBox(_chkCleanHistory);
        itemsBox.Children.Add(_chkCleanHistory);

        _chkCleanCookies = new CheckBox { Content = "Файлы cookie и другие данные сайтов (выход из аккаунтов)", IsChecked = true, Margin = new Thickness(0, 0, 0, 10) };
        StyleDarkCheckBox(_chkCleanCookies);
        itemsBox.Children.Add(_chkCleanCookies);

        _chkCleanCache = new CheckBox { Content = "Изображения и другие файлы, сохранённые в кэше", IsChecked = true, Margin = new Thickness(0, 0, 0, 10) };
        StyleDarkCheckBox(_chkCleanCache);
        itemsBox.Children.Add(_chkCleanCache);

        _chkCleanPasswords = new CheckBox { Content = "Пароли и другие данные для входа", IsChecked = false, Margin = new Thickness(0, 0, 0, 10) };
        StyleDarkCheckBox(_chkCleanPasswords);
        itemsBox.Children.Add(_chkCleanPasswords);

        _chkCleanAutofill = new CheckBox { Content = "Данные автозаполнения форм", IsChecked = false, Margin = new Thickness(0, 0, 0, 10) };
        StyleDarkCheckBox(_chkCleanAutofill);
        itemsBox.Children.Add(_chkCleanAutofill);

        _chkCleanDownloads = new CheckBox { Content = "История загрузок", IsChecked = false, Margin = new Thickness(0, 0, 0, 6) };
        StyleDarkCheckBox(_chkCleanDownloads);
        itemsBox.Children.Add(_chkCleanDownloads);

        stack.Children.Add(itemsBox);

        var actionsRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var btnSubmitClean = new Button
        {
            Content = "Удалить данные",
            Padding = new Thickness(18, 8, 18, 8),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.Bold,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnSubmitClean, true);
        btnSubmitClean.Click += async (_, _) =>
        {
            try
            {
                int hours = 0;
                if (_comboCleanTimeRange?.SelectedItem is ComboBoxItem cbi && cbi.Tag is int h) hours = h;
                DateTime? startTime = hours > 0 ? DateTime.Now.AddHours(-hours) : null;

                var kinds = (Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds)0;
                if (_chkCleanHistory?.IsChecked == true) kinds |= Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.BrowsingHistory;
                if (_chkCleanCookies?.IsChecked == true) kinds |= Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.Cookies;
                if (_chkCleanCache?.IsChecked == true) kinds |= (Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.DiskCache | Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.CacheStorage);
                if (_chkCleanPasswords?.IsChecked == true) kinds |= Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.PasswordAutosave;
                if (_chkCleanAutofill?.IsChecked == true) kinds |= Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.GeneralAutofill;
                if (_chkCleanDownloads?.IsChecked == true) kinds |= Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.DownloadHistory;

                await ClearBrowsingDataChromeAsync(kinds, startTime);

                if (_chkCleanHistory?.IsChecked == true)
                {
                    _settings.History.Clear();
                    _settings.Save();
                }
                if (_chkCleanPasswords?.IsChecked == true)
                {
                    _settings.SavedPasswords?.Clear();
                    _settings.Save();
                    RefreshPasswordsList();
                }
                if (_chkCleanDownloads?.IsChecked == true)
                {
                    ClearDownloads_Click(this, new RoutedEventArgs());
                }

                overlay.Visibility = Visibility.Collapsed;
                ShowToast("Выбранные данные успешно удалены");
            }
            catch { }
        };
        actionsRow.Children.Add(btnSubmitClean);

        var btnCancelClean = new Button
        {
            Content = "Отмена",
            Padding = new Thickness(14, 8, 14, 8),
            Cursor = Cursors.Hand,
            Tag = "SettingsInternal"
        };
        StyleDarkButton(btnCancelClean);
        btnCancelClean.Click += (_, _) => { overlay.Visibility = Visibility.Collapsed; };
        actionsRow.Children.Add(btnCancelClean);

        stack.Children.Add(actionsRow);

        card.Child = stack;
        overlay.Child = card;
        return overlay;
    }

    private void RefreshExtensionsList()
    {
        if (_extensionsListContainer == null) return;
        _extensionsListContainer.Children.Clear();

        if (_settings.InstalledExtensionPaths == null || _settings.InstalledExtensionPaths.Count == 0)
        {
            _extensionsListContainer.Children.Add(new TextBlock
            {
                Text = "Установленных расширений пока нет. Вы можете установить расширение из папки или перейти в магазин.",
                FontSize = 11,
                Foreground = GetThemeBrush("TextMutedBrush", Color.FromRgb(150, 150, 150)),
                Margin = new Thickness(0, 4, 0, 4)
            });
            return;
        }

        foreach (var path in _settings.InstalledExtensionPaths.ToList())
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameText = new TextBlock
            {
                Text = System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = GetThemeBrush("TextPrimaryBrush", Colors.White)
            };
            Grid.SetColumn(nameText, 0);
            row.Children.Add(nameText);

            var btnDel = new Button
            {
                Content = "Удалить",
                Padding = new Thickness(10, 4, 10, 4),
                Cursor = Cursors.Hand,
                FontSize = 11,
                Tag = "SettingsInternal"
            };
            StyleDarkButton(btnDel);
            var curP = path;
            btnDel.Click += (_, _) =>
            {
                _settings.InstalledExtensionPaths.Remove(curP);
                _settings.Save();
                RefreshExtensionsList();
                ShowToast("Расширение удалено");
            };
            Grid.SetColumn(btnDel, 1);
            row.Children.Add(btnDel);

            _extensionsListContainer.Children.Add(row);
        }
    }

}
