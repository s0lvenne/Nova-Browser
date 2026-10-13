#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
namespace NovaBrowser;
public partial class MainWindow : Window
{
    private readonly List<BrowserTab> _tabs = new();
    private BrowserTab? _activeTab;
    private readonly DispatcherTimer _memoryTimer;
    private bool _cacheCleared = false;
    
    private bool _isMaximized;
    private Rect _restoreBounds;
    private BrowserSettings _settings;
    private bool _isOnHomePage = true;
    private bool _settingsLoading;
    private CoreWebView2Environment? _webViewEnv;
    private HashSet<string>? _cachedCustomDomains;
    private string?          _cachedInjectionScript;
    private string           _lastCustomDomainsRaw = "";
    private string           _lastInjectionKey     = "";
    private bool _tabsUiPending;
    private readonly DispatcherTimer _tabsUiTimer = new(DispatcherPriority.Render)
        { Interval = TimeSpan.FromMilliseconds(16) };
    private BrowserTab?  _dragTab;
    private Point        _dragStartPoint;
    private bool         _isDragging;
    internal string? _pendingUrl;
    internal static readonly List<MainWindow> OpenWindows = new();
    public MainWindow()
    {
        _settings = BrowserSettings.Load();
        InitializeComponent();

        _memoryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _memoryTimer.Tick += UpdateMemoryUsage;
        _memoryTimer.Start();
        _tabsUiTimer.Tick += (_, _) =>
        {
            _tabsUiTimer.Stop();
            _tabsUiPending = false;
            UpdateTabsUIImmediate();
        };
        
        
        ApplyAccentColor(_settings.AccentColor);
        if (!string.IsNullOrEmpty(_settings.LogoColor))
            Loaded += (_, _) => ApplyLogoColor(_settings.LogoColor);
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => CreateNewTab()), Key.T, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ShowDownloadsTab()), Key.J, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ShowHistoryTab()), Key.H, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ShowBookmarksTab()), Key.B, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleDevTools()), Key.F12, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleDevTools()), Key.I, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleDevTools()), Key.J, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleDevTools()), Key.C, ModifierKeys.Control | ModifierKeys.Shift));
        if (DownloadsPopup != null) DownloadsPopup.Closed += (_, _) => { _downloadsPopupClosedTime = DateTime.Now; };
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => CloseActiveTab()), Key.W, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => AddressBox.Focus()), Key.L, ModifierKeys.Control));
        AddressBox.LostFocus += AddressBox_LostFocus;
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => Refresh_Click(this, new RoutedEventArgs())), Key.F5, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => AddBookmark_Click(this, new RoutedEventArgs())), Key.D, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleFind()),        Key.F, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleFocusMode()), Key.F, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => PrintPage()),         Key.P, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => { if (!string.IsNullOrEmpty(_lastClosedUrl)) CreateNewTab(_lastClosedUrl); }), Key.T, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenTabSearch()), Key.A, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenCommandPalette()), Key.K, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => TogglePictureInPicture()), Key.P, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => CopyPlainText()), Key.C, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ShowPageInfo()), Key.I, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ScrollToTop()), Key.Home, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ScrollToBottom()), Key.End, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleCompactTabs()), Key.OemPipe, ModifierKeys.Control));
        OpenWindows.Add(this);
        Closed += async (_, _) =>
        {
            if (_isFullResetting) return;
            SaveSession();
            OpenWindows.Remove(this);
            
            _memoryTimer.Stop();
            if (_settings.ClearOnExit)
            {
                foreach (var tab in _tabs.ToList())
                {
                    try
                    {
                        if (tab.WebViewControl?.CoreWebView2 != null)
                            await tab.WebViewControl.CoreWebView2.Profile.ClearBrowsingDataAsync();
                    }
                    catch { }
                }
            }
            foreach (var tab in _tabs.ToList())
            {
                try { tab.WebViewControl?.Dispose(); } catch { }
                tab.WebViewControl = null;
            }
        };
        Loaded += (_, _) => AnimateNovaLogo();
    }
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.PreWarmedEnv != null)
                _webViewEnv = App.PreWarmedEnv;
            else if (App.EnvTask != null)
                _webViewEnv = await App.EnvTask;
            else
                _webViewEnv = null;
            ApplySettingsToUI();
            ApplyTheme(_settings.Theme);
            ApplyUIFontFamily(_settings.UIFontFamily);
            if (_settings.Theme == "Custom")
            {
                try
                {
                    var customPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "NovaBrowser", "CustomTheme.xaml");
                    try { if (System.IO.File.Exists(customPath)) System.IO.File.Delete(customPath); } catch { }
                    ApplyTheme("Dark");
                    ApplyAccentColor(_settings.AccentColor);
                }
                catch { _settings.Theme = "Dark"; ApplyTheme("Dark"); }
            }
            ApplyZoomToActiveTabs();
            ApplyBookmarksBarVisibility();
            ApplyLargeText();
            ApplyStatusBarVisibility();
            ApplyWindowOpacity();
            if (!string.IsNullOrEmpty(_settings.NewTabBackground))
            {
                try {
                    var col = (Color)ColorConverter.ConvertFromString(_settings.NewTabBackground);
                    WallpaperLayer.Background = new SolidColorBrush(col);
                } catch { }
            }
            if (Enum.TryParse<Stretch>(_settings.WallpaperFit, out var stretch))
                WallpaperImage.Stretch = stretch;
            StartClock();
            LoadWallpaper();
            UpdateBookmarksBar();
            _settings.LoadHistoryAsync();
            if (_pendingUrl != null)
            {
                CreateNewTab(_pendingUrl);
                _pendingUrl = null;
            }
            else if (_settings.RestoreLastSession && _settings.LastSession?.Count > 0)
            {
                RestoreSession();
            }
            else
            {
                CreateNewTab();
            }
            if (ProxyIndicator != null)
            {
                ProxyIndicator.PreviewMouseLeftButtonDown += (_, _) =>
                {
                    OpenSettingsFromUi();
                    SelectSettingsSection("Proxy");
                };
            }
            UpdateProxyIndicator();
            if (_settings.IsFirstRun)
            {
                _ = Dispatcher.InvokeAsync(ShowFirstRunSetupModal, DispatcherPriority.ApplicationIdle);
            }
            AnimateStatusText("Готов");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка запуска:\n{ex.Message}\n\nУстановите WebView2 Runtime.",
                "Nova Browser",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private void ApplySettingsToUI()
    {
        _settingsLoading = true;
        if (SearchEngineCombo != null)
            for (int i = 0; i < SearchEngineCombo.Items.Count; i++)
                if (SearchEngineCombo.Items[i] is ComboBoxItem ci && ci.Content?.ToString() == _settings.SearchEngine)
                { SearchEngineCombo.SelectedIndex = i; break; }
        if (HomepageBox       != null) HomepageBox.Text              = _settings.HomepageUrl;
        if (ChkNewTab         != null) ChkNewTab.IsChecked            = _settings.OpenLinksInNewTab;
        if (ChkRestoreSession != null) ChkRestoreSession.IsChecked    = _settings.RestoreLastSession;
        if (DownloadFolderBox != null) DownloadFolderBox.Text         = _settings.DownloadFolder;
        if (ZoomCombo != null)
            foreach (ComboBoxItem ci in ZoomCombo.Items)
                if (ci.Tag?.ToString() == _settings.DefaultZoom.ToString()) { ZoomCombo.SelectedItem = ci; break; }
        if (ChkBlockCookies     != null) ChkBlockCookies.IsChecked     = _settings.BlockCookieBanners;
        if (ChkBlockTrackers    != null) ChkBlockTrackers.IsChecked    = _settings.BlockTrackers;
        if (ChkBlockAds         != null) ChkBlockAds.IsChecked         = _settings.BlockAds;
        if (ChkBlockNotif       != null) ChkBlockNotif.IsChecked       = _settings.BlockNotifications;
        if (ChkBlockPopups      != null) ChkBlockPopups.IsChecked      = _settings.BlockPopups;
        if (ChkBlockAutoplay    != null) ChkBlockAutoplay.IsChecked    = _settings.BlockAutoplay;
        if (ChkBlockCrypto      != null) ChkBlockCrypto.IsChecked      = _settings.BlockCryptomining;
        if (ChkBlockFingerprint != null) ChkBlockFingerprint.IsChecked = _settings.BlockFingerprinting;
        if (ChkBlockWebRTC      != null) ChkBlockWebRTC.IsChecked      = _settings.BlockWebRTC;
        if (ChkBlockCanvas      != null) ChkBlockCanvas.IsChecked      = _settings.BlockCanvas;
        if (ChkSendDNT          != null) ChkSendDNT.IsChecked          = _settings.SendDoNotTrack;
        if (ChkClearOnExit      != null) ChkClearOnExit.IsChecked      = _settings.ClearOnExit;
        if (ChkForceHttps       != null) ChkForceHttps.IsChecked       = _settings.ForceHttps;
        if (ChkBlockMixed       != null) ChkBlockMixed.IsChecked       = _settings.BlockMixedContent;
        if (ChkSafeBrowsing     != null) ChkSafeBrowsing.IsChecked     = _settings.SafeBrowsingMode;
        if (ChkJavaScript       != null) ChkJavaScript.IsChecked       = _settings.JavaScriptEnabled;
        if (ChkLoadImages       != null) ChkLoadImages.IsChecked       = _settings.LoadImages;
        if (ChkHwAccel          != null) ChkHwAccel.IsChecked          = _settings.HardwareAcceleration;
        if (CustomBlockedBox    != null) CustomBlockedBox.Text         = _settings.CustomBlockedDomains;
        
        
        if (LiveWpPathLabel != null)
            LiveWpPathLabel.Text = string.IsNullOrEmpty(_settings.LiveWallpaperPath)
                ? "Видео не выбрано"
                : System.IO.Path.GetFileName(_settings.LiveWallpaperPath);
        if (!string.IsNullOrEmpty(_settings.LiveWallpaperPath))
            _liveWallpaperPath = _settings.LiveWallpaperPath;
        if (ChkShowBookmarksBar != null) ChkShowBookmarksBar.IsChecked = _settings.ShowBookmarksBar;
        if (ChkShowStatusBar    != null) ChkShowStatusBar.IsChecked    = _settings.ShowStatusBar;
        if (ChkShowMemory       != null) ChkShowMemory.IsChecked       = _settings.ShowMemoryUsage;
        if (ChkCompactMode      != null) ChkCompactMode.IsChecked      = _settings.CompactMode;
        if (ChkShowClock        != null) ChkShowClock.IsChecked        = _settings.ShowClock;
        if (ChkShowFavicons     != null) ChkShowFavicons.IsChecked     = _settings.ShowFavicons;
        if (WindowOpacitySlider != null) WindowOpacitySlider.Value     = _settings.WindowOpacity * 100;
        if (WindowOpacityLabel  != null) WindowOpacityLabel.Text       = $"{(int)(_settings.WindowOpacity * 100)}%";
        if (CustomCssBox        != null) CustomCssBox.Text             = _settings.CustomCss;
        if (NewTabBgBox         != null) NewTabBgBox.Text              = _settings.NewTabBackground;
        if (FontSizeCombo != null)
            foreach (ComboBoxItem ci in FontSizeCombo.Items)
                if (ci.Tag?.ToString() == _settings.FontSize.ToString()) { FontSizeCombo.SelectedItem = ci; break; }
        if (FontFamilyCombo != null)
            foreach (ComboBoxItem ci in FontFamilyCombo.Items)
                if (ci.Tag?.ToString() == _settings.UIFontFamily) { FontFamilyCombo.SelectedItem = ci; break; }
        if (TabRadiusCombo != null)
            foreach (ComboBoxItem ci in TabRadiusCombo.Items)
                if (ci.Tag?.ToString() == _settings.TabRadius.ToString()) { TabRadiusCombo.SelectedItem = ci; break; }
        if (ChkHomeShowSearch != null) ChkHomeShowSearch.IsChecked = _settings.HomeShowSearchBar;
        if (ChkHomeShowLinks  != null) ChkHomeShowLinks.IsChecked  = _settings.HomeShowQuickLinks;
        if (HomeBgStyleCombo != null)
            foreach (ComboBoxItem ci in HomeBgStyleCombo.Items)
                if (ci.Tag?.ToString() == _settings.HomeBackgroundStyle) { HomeBgStyleCombo.SelectedItem = ci; break; }
        if (HomeBgOpacitySlider != null) HomeBgOpacitySlider.Value = _settings.HomeBgImageOpacity;
        if (_settings.WallpaperPresetIndex >= 0 && _settings.WallpaperPresetIndex < _wpPresets.Length)
        {
            int idx = _settings.WallpaperPresetIndex;
            _customThemeWallpaperPath = $"preset:{idx}";
            if (CustomWpPathLabel != null)
                CustomWpPathLabel.Text = $"Пресет: {_wpPresets[idx].Item1}";
            var previews = new[] { WpPreview0, WpPreview1, WpPreview2, WpPreview3, WpPreview4, WpPreview5 };
            for (int i = 0; i < previews.Length; i++)
            {
                if (previews[i] == null) continue;
                previews[i]!.BorderThickness = new Thickness(i == idx ? 2 : 1);
                previews[i]!.Opacity = i == idx ? 1.0 : 0.55;
            }
        }
        RefreshQuickLinksEditor();
        if (ChkReduceAnimations != null) ChkReduceAnimations.IsChecked = _settings.ReduceAnimations;
        if (ChkLargeText        != null) ChkLargeText.IsChecked        = _settings.LargeText;
        if (ChkAskDownload   != null) ChkAskDownload.IsChecked   = _settings.AskDownloadLocation;
        if (ChkAutoOpen      != null) ChkAutoOpen.IsChecked       = _settings.AutoOpenDownloads;
        if (ChkDownloadBadge != null) ChkDownloadBadge.IsChecked  = _settings.ShowDownloadBadge;
        if (DownloadFolderBox2 != null) DownloadFolderBox2.Text   = _settings.DownloadFolder;
        if (ChkSmoothScroll != null) ChkSmoothScroll.IsChecked = _settings.SmoothScrolling;
        if (ChkHwAccelPerf  != null) ChkHwAccelPerf.IsChecked  = _settings.HardwareAcceleration;
        if (ChkReduceAnim   != null) ChkReduceAnim.IsChecked    = _settings.ReduceAnimations;

        if (RamWarningSlider    != null) RamWarningSlider.Value   = _settings.RamWarningThresholdMB;
        if (RamWarningLabel     != null) RamWarningLabel.Text     = _settings.RamWarningThresholdMB.ToString();
        if (ChkLazyImages        != null) ChkLazyImages.IsChecked       = _settings.LazyLoadImages;
        if (ChkClearCacheStartup != null) ChkClearCacheStartup.IsChecked= _settings.ClearCacheOnStartup;
        if (ChkForceDark   != null) ChkForceDark.IsChecked   = _settings.ForceDarkMode;
        if (ChkPDFViewer   != null) ChkPDFViewer.IsChecked   = _settings.EnablePDFViewer;
        if (ChkAutofill    != null) ChkAutofill.IsChecked    = _settings.EnableAutofill;
        if (ChkSpellCheck  != null) ChkSpellCheck.IsChecked  = _settings.EnableSpellCheck2;
        if (ChkDevToolsF12 != null) ChkDevToolsF12.IsChecked = _settings.OpenDevToolsOnF12;
        if (UserAgentBox   != null) UserAgentBox.Text        = _settings.UserAgent;
        if (LogoColorBox   != null) LogoColorBox.Text = string.IsNullOrEmpty(_settings.LogoColor)
            ? _settings.AccentColor : _settings.LogoColor;
        UpdateThemeCards();
        _settingsLoading = false;
    }
    private async Task<WebView2> CreateWebViewForTab(BrowserTab tab)
    {
        if (tab.WebViewControl != null)
            return tab.WebViewControl;
        var webView = new WebView2
        {
            Visibility  = Visibility.Visible,
            Width       = 0,
            Height      = 0,
        };
        WebViewContainer.Children.Add(webView);
        if (_webViewEnv == null && App.EnvTask != null)
            _webViewEnv = await App.EnvTask;
        if (_webViewEnv == null)
        {
            var dataFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovaBrowser", "UserData");
            _webViewEnv = await CoreWebView2Environment.CreateAsync(
                userDataFolder: dataFolder,
                options: new CoreWebView2EnvironmentOptions(App.BuildWebView2Arguments(_settings)));
        }
        try
        {
            await webView.EnsureCoreWebView2Async(_webViewEnv);
        }
        catch (System.Runtime.InteropServices.COMException ex)
            when (ex.HResult == unchecked((int)0x8007139F))
        {
            System.Diagnostics.Debug.WriteLine("EnsureCoreWebView2Async 0x8007139F — retrying with same env after delay");
            bool recovered = false;
            for (int attempt = 0; attempt < 3 && !recovered; attempt++)
            {
                await Task.Delay(400);
                try
                {
                    await webView.EnsureCoreWebView2Async(_webViewEnv);
                    recovered = true;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                }
            }
            if (!recovered)
            {
                try
                {
                    WebViewContainer.Children.Remove(webView);
                    var fallbackFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "NovaBrowser", $"UserData_{DateTime.Now:yyyyMMddHHmmss}");
                    var fallbackEnv = await CoreWebView2Environment.CreateAsync(userDataFolder: fallbackFolder);
                    var webView2 = new WebView2 { Visibility = Visibility.Visible, Width = 0, Height = 0 };
                    WebViewContainer.Children.Add(webView2);
                    await webView2.EnsureCoreWebView2Async(fallbackEnv);
                    webView = webView2;
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"WebView2 fallback failed: {fallbackEx.Message}");
                    MessageBox.Show(
                        "Не удалось инициализировать WebView2. Перезапустите браузер.\n\n" + fallbackEx.Message,
                        "Nova Browser — ошибка WebView2",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        if (webView.CoreWebView2 != null)
        {
            if (_settings.ClearCacheOnStartup && !_cacheCleared)
            {
                _cacheCleared = true;
                try
                {
                    _ = webView.CoreWebView2.Profile.ClearBrowsingDataAsync(
                        Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.AllProfile);
                }
                catch { }
            }
            var settings = webView.CoreWebView2.Settings;
            settings.IsScriptEnabled               = _settings.JavaScriptEnabled;
            settings.IsStatusBarEnabled             = false;
            settings.AreDevToolsEnabled             = _settings.OpenDevToolsOnF12;
            settings.IsWebMessageEnabled            = true;
            settings.AreHostObjectsAllowed          = true;
            settings.IsSwipeNavigationEnabled       = false;
            settings.IsBuiltInErrorPageEnabled      = true;
            settings.IsGeneralAutofillEnabled       = _settings.EnableAutofill;
            settings.IsPasswordAutosaveEnabled      = _settings.EnableAutofill;
            settings.AreDefaultContextMenusEnabled  = true;
            settings.IsPinchZoomEnabled             = true;
            settings.AreBrowserAcceleratorKeysEnabled = true;
            if (!string.IsNullOrWhiteSpace(_settings.UserAgent))
                settings.UserAgent = _settings.UserAgent;
            var zoomFactor = tab.ZoomLevel > 0
                ? tab.ZoomLevel / 100.0
                : _settings.DefaultZoom / 100.0;
            webView.ZoomFactor = zoomFactor;
            try
            {
                webView.CoreWebView2.Profile.PreferredColorScheme = _settings.ForceDarkMode
                    ? CoreWebView2PreferredColorScheme.Dark
                    : CoreWebView2PreferredColorScheme.Auto;
            }
            catch { }
            try
            {
                var cm = webView.CoreWebView2.CookieManager;
                var darkCookie = cm.CreateCookie("PREF", "f6=400&f7=1", ".youtube.com", "/");
                cm.AddOrUpdateCookie(darkCookie);
            }
            catch { }
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                GetCachedInjectionScript());
            WireDownloads(webView.CoreWebView2);
            webView.CoreWebView2.ContainsFullScreenElementChanged += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    bool goFull = webView.CoreWebView2?.ContainsFullScreenElement == true;
                    if (goFull && !_isFullscreen)
                    {
                        _preFullscreenStyle  = WindowStyle;
                        _preFullscreenState  = WindowState;
                        _preFullscreenMargin = MainBorder != null ? MainBorder.Margin : new Thickness(6);
                        if (TitleBarGrid  != null) TitleBarGrid.Visibility  = Visibility.Collapsed;
                        if (NavBarGrid    != null) NavBarGrid.Visibility    = Visibility.Collapsed;
                        if (BookmarksBar  != null) BookmarksBar.Visibility  = Visibility.Collapsed;
                        if (StatusBarGrid != null) StatusBarGrid.Visibility = Visibility.Collapsed;
                        if (MainBorder != null)
                        {
                            MainBorder.BorderThickness = new Thickness(0);
                            MainBorder.CornerRadius    = new CornerRadius(0);
                            MainBorder.Margin          = new Thickness(0);
                            try { MainBorder.Effect = null; } catch { }
                        }
                        WindowStyle = WindowStyle.None;
                        WindowState = WindowState.Normal;
                        Topmost = true;
                        Left   = 0; Top    = 0;
                        Width  = SystemParameters.PrimaryScreenWidth;
                        Height = SystemParameters.PrimaryScreenHeight;
                        webView.Width  = double.NaN;
                        webView.Height = double.NaN;
                        Panel.SetZIndex(webView, 999);
                        _isFullscreen = true;
                    }
                    else if (!goFull && _isFullscreen)
                    {
                        Topmost = false;
                        Panel.SetZIndex(webView, 0);
                        var radius = _settings.Theme == "Win7" ? 4 : 12;
                        if (MainBorder != null)
                        {
                            MainBorder.BorderThickness = new Thickness(_isMaximized ? 0 : 1);
                            MainBorder.CornerRadius    = new CornerRadius(_isMaximized ? 0 : radius);
                            MainBorder.Margin          = _isMaximized ? new Thickness(0) : _preFullscreenMargin;
                            if (!_isMaximized)
                            {
                                try { MainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                                      { BlurRadius = 24, ShadowDepth = 0, Opacity = 0.6,
                                        Color = System.Windows.Media.Colors.Black }; } catch { }
                            }
                        }
                        if (TitleBarGrid != null) TitleBarGrid.Visibility = Visibility.Visible;
                        if (NavBarGrid   != null) NavBarGrid.Visibility   = Visibility.Visible;
                        ApplyBookmarksBarVisibility();
                        ApplyStatusBarVisibility();
                        WindowStyle = _preFullscreenStyle;
                        WindowState = _preFullscreenState;
                        _isFullscreen = false;
                    }
                });
            };
            WireAdBlocker(webView.CoreWebView2, tab);
            try
            {
                webView.CoreWebView2.WebMessageReceived += (_, args) =>
                {
                    try
                    {
                        var rawJson = args.TryGetWebMessageAsString();
                        if (!string.IsNullOrEmpty(rawJson))
                            HandleWebMessageFromPage(rawJson, tab);
                    }
                    catch { }
                };
            }
            catch { }
            if (_settings.InstalledExtensionPaths != null)
            {
                foreach (var extPath in _settings.InstalledExtensionPaths)
                {
                    if (System.IO.Directory.Exists(extPath) || System.IO.File.Exists(extPath))
                    {
                        try { _ = webView.CoreWebView2.Profile.AddBrowserExtensionAsync(extPath); } catch { }
                    }
                }
            }
            webView.CoreWebView2.NavigationStarting += (_, args) =>
            {
                if (args.Uri != null && args.Uri.Contains("youtube.com"))
                {
                    try
                    {
                        var cm = webView.CoreWebView2.CookieManager;
                        var darkCookie = cm.CreateCookie("PREF", "f6=400&f7=1", ".youtube.com", "/");
                        cm.AddOrUpdateCookie(darkCookie);
                    }
                    catch { }
                }
                if (_settings.EnableSiteStealthIsolation && _settings.StealthStripTrackingHeaders && !string.IsNullOrEmpty(args.Uri))
                {
                    var clean = SecurityHelper.SanitizeTrackingUrl(args.Uri);
                    if (clean != args.Uri)
                    {
                        args.Cancel = true;
                        webView.CoreWebView2?.Navigate(clean);
                        return;
                    }
                }
                var (isSafe, reason) = SecurityHelper.ValidateUrl(
                    args.Uri ?? "",
                    customBlockedDomains: _settings.CustomBlockedDomains?.Split(new[]{'\n','\r'}, StringSplitOptions.RemoveEmptyEntries),
                    forceHttps:           _settings.ForceHttps);
                if (!isSafe && reason.StartsWith("HTTPS_UPGRADE:"))
                {
                    args.Cancel = true;
                    var upgradeUrl = reason["HTTPS_UPGRADE:".Length..];
                    webView.CoreWebView2?.Navigate(upgradeUrl);
                    return;
                }
                if (!isSafe)
                {
                    args.Cancel = true;
                    if (tab == _activeTab)
                        ShowBlockedPage(reason);
                    return;
                }
                tab.IsLoading = true;
                if (tab == _activeTab)
                {
                    AnimateLoadingBarIn();
                    StatusText.Text = "Загрузка...";
                    BlockedPage.Visibility = Visibility.Collapsed;
                    _isOnHomePage = false;
                    HomePage.Visibility = Visibility.Collapsed;
                    SettingsPage.Visibility = Visibility.Collapsed;
                }
            };
            webView.CoreWebView2.PermissionRequested += (_, args) =>
            {
                var reqUri = args.Uri ?? "";
                bool isIsolated = _settings.EnableSiteStealthIsolation && SecurityHelper.IsStealthDomain(reqUri, _settings.StealthIsolationDomains);
                if (isIsolated)
                {
                    if (_settings.StealthBlockGeo && args.PermissionKind == CoreWebView2PermissionKind.Geolocation)
                    {
                        args.State = CoreWebView2PermissionState.Deny;
                        return;
                    }
                    if (_settings.StealthBlockSensorsAndHardware && (args.PermissionKind == CoreWebView2PermissionKind.Camera ||
                                                                    args.PermissionKind == CoreWebView2PermissionKind.Microphone ||
                                                                    args.PermissionKind == CoreWebView2PermissionKind.ClipboardRead))
                    {
                        args.State = CoreWebView2PermissionState.Deny;
                        return;
                    }
                }
                switch (args.PermissionKind)
                {
                    case CoreWebView2PermissionKind.Autoplay:
                        args.State = CoreWebView2PermissionState.Allow;
                        break;
                    case CoreWebView2PermissionKind.Notifications when _settings.BlockNotifications:
                        args.State = CoreWebView2PermissionState.Deny;
                        break;
                    default:
                        break;
                }
            };
            webView.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                tab.IsLoading = false;
                var sourceUrl = webView.Source?.ToString() ?? "";
                if (!tab.Url.StartsWith("nova://"))
                    tab.Url = sourceUrl;
                tab.IsSecure = SecurityHelper.IsSecureConnection(tab.Url);
                var zoomFactor = tab.ZoomLevel > 0
                    ? tab.ZoomLevel / 100.0
                    : _settings.DefaultZoom / 100.0;
                webView.ZoomFactor = zoomFactor;
                if (args.IsSuccess && !string.IsNullOrEmpty(tab.Url) && tab.Url != "about:blank")
                {
                    AddToHistory(webView.CoreWebView2.DocumentTitle, tab.Url);
                    SaveSession();
                }
                if (tab == _activeTab)
                {
                    AnimateLoadingBarOut();
                    BackButton.IsEnabled = webView.CanGoBack;
                    ForwardButton.IsEnabled = webView.CanGoForward;
                    UpdateSecurityIndicator();
                    UpdateMuteButton();
                    UpdateZoomLabel();
                    if (args.IsSuccess)
                    {
                        AnimateStatusText("Готов");
                    }
                    else if (args.WebErrorStatus != CoreWebView2WebErrorStatus.ConnectionAborted &&
                             args.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
                    {
                        AnimateStatusText($"Ошибка: {args.WebErrorStatus}");
                    }
                }
            };
            webView.CoreWebView2.SourceChanged += (_, _) =>
            {
                tab.Url = webView.Source?.ToString() ?? "";
                if (tab == _activeTab && !_isOnHomePage)
                    AddressBox.Text = (IsNovaUrl(tab.Url) || IsNovaHost(tab.Url) || tab.Url.Contains("private.html", StringComparison.OrdinalIgnoreCase)) ? "" : tab.Url;
            };
            webView.CoreWebView2.DocumentTitleChanged += (_, _) =>
            {
                tab.Title = webView.CoreWebView2.DocumentTitle;
                UpdateTabsUI();
            };
            webView.CoreWebView2.IsDocumentPlayingAudioChanged += (_, _) =>
            {
                tab.IsPlayingAudio = webView.CoreWebView2.IsDocumentPlayingAudio;
                UpdateTabsUI();
            };
            webView.CoreWebView2.IsMutedChanged += (_, _) =>
            {
                tab.IsMuted = webView.CoreWebView2.IsMuted;
                UpdateTabsUI();
            };
            webView.CoreWebView2.FaviconChanged += async (_, _) =>
            {
                try
                {
                    using var stream = await webView.CoreWebView2.GetFaviconAsync(
                        CoreWebView2FaviconImageFormat.Png);
                    if (stream == null) return;
                    var ms = new System.IO.MemoryStream();
                    await stream.CopyToAsync(ms);
                    ms.Position = 0;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.StreamSource = ms;
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            bmp.Freeze();
                            tab.FaviconImage = bmp;
                            UpdateTabsUI();
                        }
                        catch { }
                    });
                }
                catch { }
            };
            webView.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                if (_settings.BlockPopups && !args.IsUserInitiated) return;
                Dispatcher.Invoke(() =>
                {
                    CreateNewTab(args.Uri);
                });
            };
        }
        tab.WebViewControl = webView;
        return webView;
    }

    private void CreateNewTab(string? url = null)
    {
        _ = CreateNewTabAsync(url, true);
    }

    private async Task<BrowserTab> CreateNewTabAsync(string? url = null, bool activate = true)
    {
        if (string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(_settings.HomepageUrl))
            url = _settings.HomepageUrl;

        var tab = new BrowserTab
        {
            Title = "Новая вкладка",
            Url = url ?? ""
        };
        _tabs.Add(tab);

        if (activate)
        {
            _activeTab = tab;
            HideAllWebViews();
        }

        if (string.IsNullOrWhiteSpace(url) || url == "nova://newtab" || (IsNovaHost(url) && !url.Contains("#/search?q=")))
        {
            tab.Url = "";
            if (activate)
            {
                ShowHomePage();
                AddressBox.Text = "";
            }
            UpdateTabsUI();
            SaveSession();
            return tab;
        }

        if (url.Equals("nova://settings", StringComparison.OrdinalIgnoreCase))
        {
            tab.Title = "Настройки";
            if (activate)
            {
                ShowSettingsTab();
                AddressBox.Text = "";
            }
            UpdateTabsUI();
            SaveSession();
            return tab;
        }

        if (url.Equals("nova://downloads", StringComparison.OrdinalIgnoreCase))
        {
            tab.Title = "Загрузки";
            if (activate)
            {
                SwitchToTab(tab);
            }
            UpdateTabsUI();
            SaveSession();
            return tab;
        }

        if (url.Equals("nova://history", StringComparison.OrdinalIgnoreCase))
        {
            tab.Title = "История";
            if (activate)
            {
                SwitchToTab(tab);
            }
            UpdateTabsUI();
            SaveSession();
            return tab;
        }

        if (url.StartsWith("nova://search?q=", StringComparison.OrdinalIgnoreCase))
        {
            var q = Uri.UnescapeDataString(url.Substring("nova://search?q=".Length));
            var fullUrl = $"{NovaHomeUrl}#/search?q={Uri.EscapeDataString(q)}";
            tab.Url = fullUrl;
            tab.Title = $"{q} — Nova Search";
            if (activate)
            {
                AddressBox.Text = "";
                _ = ShowNovaHomeAsync(tab, fullUrl);
            }
            UpdateTabsUI();
            SaveSession();
            return tab;
        }

        if (IsNovaHost(url) && url.Contains("#/search?q="))
        {
            var qMatch = System.Text.RegularExpressions.Regex.Match(url, @"[?&]q=([^&]*)");
            var q = qMatch.Success ? Uri.UnescapeDataString(qMatch.Groups[1].Value) : "";
            tab.Url = url;
            if (!string.IsNullOrWhiteSpace(q)) tab.Title = $"{q} — Nova Search";
            if (activate)
            {
                AddressBox.Text = "";
                _ = ShowNovaHomeAsync(tab, url);
            }
            UpdateTabsUI();
            SaveSession();
            return tab;
        }

        var webView = await CreateWebViewForTab(tab);
        if (WebViewContainer != null && !WebViewContainer.Children.Contains(webView))
        {
            WebViewContainer.Children.Add(webView);
        }
        webView.Width = double.NaN;
        webView.Height = double.NaN;
        webView.HorizontalAlignment = HorizontalAlignment.Stretch;
        webView.VerticalAlignment = VerticalAlignment.Stretch;

        if (activate)
        {
            HideAllPages();
            if (WebViewContainer != null) WebViewContainer.Visibility = Visibility.Visible;
            webView.Visibility = Visibility.Visible;
            AnimateTabSwitch(webView);
            _isOnHomePage = false;
        }
        else
        {
            webView.Visibility = Visibility.Hidden;
        }

        var normalized = SecurityHelper.NormalizeInput(url, _settings.SearchEngine);
        var (ok, _) = SecurityHelper.ValidateUrl(normalized);
        if (ok)
        {
            for (int i = 0; i < 30 && webView.CoreWebView2 == null; i++)
                await Task.Delay(50);
            if (webView.CoreWebView2 != null)
            {
                try { webView.CoreWebView2.Navigate(normalized); }
                catch (System.Runtime.InteropServices.COMException)
                {
                    if (activate) ShowHomePage();
                }
            }
            else if (activate)
            {
                ShowHomePage();
            }
        }
        else if (activate)
        {
            ShowHomePage();
        }

        if (activate)
        {
            AddressBox.Text = (IsNovaUrl(tab.Url) || IsNovaHost(tab.Url) || tab.Url.Contains("private.html", StringComparison.OrdinalIgnoreCase)) ? "" : tab.Url;
        }
        UpdateTabsUI();
        SaveSession();
        return tab;
    }
    private void SwitchToTab(BrowserTab tab)
    {
        if (tab == _activeTab) return;
        var prevTab = _activeTab;
        _activeTab = tab;
        if (prevTab != null && prevTab != tab && prevTab.WebViewControl?.CoreWebView2 != null && !prevTab.IsPlayingAudio)
        {
            try
            {
                _ = prevTab.WebViewControl.CoreWebView2.CallDevToolsProtocolMethodAsync("Memory.forciblyPurgeCache", "{}");
            }
            catch { }
        }
        ApplyIncognitoTheme(tab.IsPrivate);
        HideAllWebViews();
        if (tab.Url == "nova://settings")
        {
            HideAllPages();
            SettingsPage.Visibility = Visibility.Visible;
            if (ShouldAnimate) AnimateSlideInFromLeft(SettingsPage);
            _isOnHomePage = false;
            AddressBox.Text = "";
            UpdateTabsUI();
            return;
        }
        if (tab.Url == "nova://downloads")
        {
            HideAllPages();
            HideAllWebViews();
            if (DownloadsPage != null)
            {
                DownloadsPage.Visibility = Visibility.Visible;
                if (ShouldAnimate) AnimateSlideIn(DownloadsPage, 180);
            }
            _isOnHomePage = false;
            AddressBox.Text = "";
            UpdateTabsUI();
            UpdateFullDownloadsPage(DownloadsSearchBox?.Text ?? "");
            return;
        }
        if (tab.Url == "nova://history")
        {
            HideAllPages();
            HideAllWebViews();
            if (HistoryPage != null)
            {
                HistoryPage.Visibility = Visibility.Visible;
                if (ShouldAnimate) AnimateSlideIn(HistoryPage, 180);
            }
            _isOnHomePage = false;
            AddressBox.Text = "";
            UpdateTabsUI();
            UpdateHistoryPage(HistorySearchBox?.Text ?? "");
            return;
        }
        if (tab.Url == "nova://bookmarks")
        {
            HideAllPages();
            HideAllWebViews();
            if (BookmarksPage != null)
            {
                BookmarksPage.Visibility = Visibility.Visible;
                if (ShouldAnimate) AnimateSlideIn(BookmarksPage, 180);
            }
            _isOnHomePage = false;
            AddressBox.Text = "";
            UpdateTabsUI();
            UpdateBookmarksPage();
            return;
        }
        if (tab.Url == "nova://history")
        {
            HideAllPages();
            HideAllWebViews();
            if (HistoryPage != null)
            {
                HistoryPage.Visibility = Visibility.Visible;
                if (ShouldAnimate) AnimateSlideIn(HistoryPage, 180);
            }
            _isOnHomePage = false;
            AddressBox.Text = "";
            UpdateTabsUI();
            UpdateHistoryPage(HistorySearchBox?.Text ?? "");
            return;
        }
        if (tab.Url == "nova://bookmarks")
        {
            HideAllPages();
            HideAllWebViews();
            if (BookmarksPage != null)
            {
                BookmarksPage.Visibility = Visibility.Visible;
                if (ShouldAnimate) AnimateSlideIn(BookmarksPage, 180);
            }
            _isOnHomePage = false;
            AddressBox.Text = "";
            UpdateTabsUI();
            UpdateBookmarksPage();
            return;
        }

        if (string.IsNullOrEmpty(tab.Url) || tab.Url == "nova://newtab" || (IsNovaHost(tab.Url) && !tab.Url.Contains("#/search?q=")))
        {
            _isOnHomePage = true;
            if (tab.WebViewControl == null)
            {
                _ = ShowNovaHomeAsync(tab);
            }
            else
            {
                HideAllPages();
                if (WebViewContainer != null)
                {
                    WebViewContainer.Visibility = Visibility.Visible;
                    if (!WebViewContainer.Children.Contains(tab.WebViewControl))
                        WebViewContainer.Children.Add(tab.WebViewControl);
                }
                tab.WebViewControl.Visibility = Visibility.Visible;
                tab.WebViewControl.Width = double.NaN;
                tab.WebViewControl.Height = double.NaN;
            }
            ClearHomeAddress();
            UpdateTabsUI();
            SaveSession();
            return;
        }

        if (IsNovaHost(tab.Url) && tab.Url.Contains("#/search?q="))
        {
            _isOnHomePage = true;
            if (tab.WebViewControl == null || tab.WebViewControl.Source == null || tab.WebViewControl.Source.ToString() == "" || tab.WebViewControl.Source.ToString() == "about:blank")
            {
                _ = ShowNovaHomeAsync(tab, tab.Url);
            }
            else
            {
                HideAllPages();
                if (WebViewContainer != null)
                {
                    WebViewContainer.Visibility = Visibility.Visible;
                    if (!WebViewContainer.Children.Contains(tab.WebViewControl))
                        WebViewContainer.Children.Add(tab.WebViewControl);
                }
                tab.WebViewControl.Visibility = Visibility.Visible;
                tab.WebViewControl.Width = double.NaN;
                tab.WebViewControl.Height = double.NaN;
            }
            ClearHomeAddress();
            UpdateTabsUI();
            SaveSession();
            return;
        }

        if (string.IsNullOrEmpty(tab.Url) && tab.WebViewControl == null)
        {
            ShowHomePage();
        }
        else if (tab.WebViewControl is { } webView)
        {
            HideAllPages();
            if (WebViewContainer != null)
            {
                WebViewContainer.Visibility = Visibility.Visible;
                if (!WebViewContainer.Children.Contains(webView))
                    WebViewContainer.Children.Add(webView);
            }
            webView.Visibility = Visibility.Visible;
            webView.Width  = double.NaN;
            webView.Height = double.NaN;
            webView.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView.VerticalAlignment = VerticalAlignment.Stretch;
            try { webView.CoreWebView2?.Resume(); } catch { }
            if (webView.CoreWebView2 != null && (webView.Source == null || webView.Source.ToString() == "" || webView.Source.ToString() == "about:blank") && !string.IsNullOrEmpty(tab.Url) && !IsNovaHost(tab.Url))
            {
                var norm = SecurityHelper.NormalizeInput(tab.Url, _settings.SearchEngine);
                try { webView.CoreWebView2.Navigate(norm); } catch { }
            }
            AnimateTabSwitch(webView);
            _isOnHomePage = false;
            if (BackButton != null) BackButton.IsEnabled    = webView.CanGoBack;
            if (ForwardButton != null) ForwardButton.IsEnabled = webView.CanGoForward;
            UpdateSecurityIndicator();
            var zoomFactor = tab.ZoomLevel > 0
                ? tab.ZoomLevel / 100.0
                : _settings.DefaultZoom / 100.0;
            webView.ZoomFactor = zoomFactor;
            UpdateZoomLabel();
            if (tab.IsLoading)
                AnimateLoadingBarIn();
            else
                AnimateLoadingBarOut();
        }
        else
        {
            ShowHomePage();
        }
        var displayUrl = tab.WebViewControl?.Source?.ToString() ?? tab.Url;
        AddressBox.Text = (IsNovaUrl(displayUrl) || IsNovaHost(displayUrl) || displayUrl.Contains("private.html", StringComparison.OrdinalIgnoreCase)) ? "" : displayUrl;
        UpdateTabsUI();
        UpdateMuteButton();
        if (ShouldAnimate) AnimateNavigation();
    }
    private void HideAllWebViews()
    {
        foreach (var tab in _tabs)
        {
            if (tab.WebViewControl == null) continue;
            if (tab != _activeTab)
            {
                tab.WebViewControl.Visibility = Visibility.Hidden;
            }
        }
    }
    private void CloseActiveTab()
    {
        if (_activeTab != null)
            CloseTab(_activeTab);
    }
    private void CloseTab(BrowserTab tab)
    {
        if (tab.IsPrivate)
        {
            try { _ = tab.WebViewControl?.CoreWebView2?.Profile?.ClearBrowsingDataAsync(); } catch { }
        }
        else
        {
            RememberLastClosed(tab);
        }
        int index = _tabs.IndexOf(tab);
        bool wasActive = (tab == _activeTab);
        if (tab.WebViewControl != null)
        {
            WebViewContainer.Children.Remove(tab.WebViewControl);
            tab.WebViewControl.Dispose();
            tab.WebViewControl = null;
        }
        _tabs.Remove(tab);
        TrimBackgroundRam();
        if (_tabs.Count == 0)
        {
            _activeTab = null;
            CreateNewTab();
            SaveSession();
            return;
        }
        if (wasActive)
        {
            _activeTab = null;
            int nextIdx = Math.Min(index, _tabs.Count - 1);
            SwitchToTab(_tabs[nextIdx]);
        }
        else
        {
            UpdateTabsUI();
        }
        SaveSession();
    }
    private void UpdateTabsUI()
    {
        if (_tabsUiPending) return;
        _tabsUiPending = true;
        _tabsUiTimer.Stop();
        _tabsUiTimer.Start();
    }
    private void UpdateTabsUIImmediate()
    {
        TabsPanel.Children.Clear();
        for (int i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            var idx = i;
            var border = new Border
            {
                CornerRadius  = new CornerRadius(_settings.TabRadius, _settings.TabRadius, 0, 0),
                Margin        = new Thickness(1, 0, 1, 0),
                Padding       = new Thickness(tab.IsPinned ? 8 : 12, 6, 4, 6),
                Cursor        = Cursors.Hand,
                MinWidth      = tab.IsPinned ? 36 : 80,
                MaxWidth      = tab.IsPinned ? 36 : 220,
                Tag           = tab,
                AllowDrop     = true,
                ClipToBounds  = true,
                RenderTransformOrigin = new Point(0.5, 1.0),
                RenderTransform = new ScaleTransform(1, 1),
                Background    = tab == _activeTab
                    ? new SolidColorBrush(Color.FromArgb(42, (Application.Current?.Resources["AccentColor"] is Color _ac1 ? _ac1.R : (byte)0), (Application.Current?.Resources["AccentColor"] is Color _ac2 ? _ac2.G : (byte)212), (Application.Current?.Resources["AccentColor"] is Color _ac3 ? _ac3.B : (byte)255)))
                    : Brushes.Transparent,
                BorderBrush   = tab == _activeTab
                    ? (Application.Current?.Resources["AccentBrush"] as Brush ?? Brushes.Cyan)
                    : Brushes.Transparent,
                BorderThickness = tab == _activeTab
                    ? new Thickness(0, 0, 0, 2)
                    : new Thickness(0)
            };
            if (ShouldAnimate && i == _tabs.Count - 1 && _tabs.Count > 1)
            {
                var st = (ScaleTransform)border.RenderTransform;
                st.ScaleX = 0.7; st.ScaleY = 0.85;
                border.Opacity = 0;
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                var dur = TimeSpan.FromMilliseconds(160);
                st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.7, 1.0, dur) { EasingFunction = ease });
                st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.85, 1.0, dur) { EasingFunction = ease });
                border.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, dur));
            }
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            if (_settings.ShowFavicons && !tab.IsPinned)
            {
                grid.ColumnDefinitions.Insert(0, new ColumnDefinition { Width = GridLength.Auto });
                UIElement faviconEl;
                if (tab.IsLoading)
                {
                    var spinner = new TextBlock
                    {
                        Text = "",
                        FontSize = 11,
                        Width = 16, Height = 16,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                        Foreground = (Brush)FindResource("AccentBrush"),
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = new RotateTransform(0)
                    };
                    if (ShouldAnimate)
                    {
                        var spin = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(0.8)))
                            { RepeatBehavior = RepeatBehavior.Forever };
                        ((RotateTransform)spinner.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, spin);
                    }
                    faviconEl = spinner;
                }
                else if (tab.FaviconImage != null)
                {
                    var img = new Image
                    {
                        Source = tab.FaviconImage,
                        Width = 16, Height = 16,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                    
                    faviconEl = img;
                }
                else
                {
                    faviconEl = new TextBlock
                    {
                        Text = tab.IsSecure ? "\uE72E" : "\uE774",
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                        Foreground = tab.IsSecure
                            ? (Brush)FindResource("AccentGreenBrush")
                            : (Brush)FindResource("TextMutedBrush"),
                        Opacity = 1.0
                    };
                }
                Grid.SetColumn(faviconEl, 0);
                grid.Children.Add(faviconEl);
            }
            var colOffset = (_settings.ShowFavicons && !tab.IsPinned) ? 1 : 0;
            string? iconPrefix = null;
            string displayTitle;
            if (tab.IsPinned)
            {
                if (tab.FaviconImage != null)
                    displayTitle = "";
                else
                {
                    iconPrefix = "\uE718";
                    displayTitle = "";
                }
            }
            else
            {
                if (tab.IsMuted) iconPrefix = "\uE74F";
                else if (tab.IsPlayingAudio) iconPrefix = "\uE767";
                var raw = tab.Title;
                displayTitle = raw.Length > 22 ? raw[..21] + "…" : raw;
            }
            var title = new TextBlock
            {
                Foreground        = tab == _activeTab
                    ? (Brush)FindResource("TextPrimaryBrush")
                    : (Brush)FindResource("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize          = 12,
                TextTrimming      = TextTrimming.CharacterEllipsis,
            };
            if (!string.IsNullOrEmpty(iconPrefix))
            {
                title.Inlines.Add(new Run(iconPrefix)
                {
                    FontFamily = new FontFamily("Segoe MDL2 Assets")
                });
                if (!string.IsNullOrEmpty(displayTitle))
                    title.Inlines.Add(new Run(" " + displayTitle));
            }
            else
            {
                title.Text = displayTitle;
            }
            Grid.SetColumn(title, colOffset);
            grid.Children.Add(title);
            if (!tab.IsPinned)
            {
                var closeBtn = new Button
                {
                    Content         = "✕",
                    FontFamily      = new FontFamily("Segoe UI, Arial"),
                    FontWeight      = FontWeights.SemiBold,
                    Width           = 18, Height = 18,
                    Background      = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground      = (Brush)FindResource("TextMutedBrush"),
                    Cursor          = Cursors.Hand,
                    Margin          = new Thickness(4, 0, 0, 0),
                    Opacity         = tab == _activeTab ? 0.8 : 0,
                    FontSize        = 10,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Padding         = new Thickness(0)
                };
                closeBtn.MouseEnter += (_, _) =>
                {
                    closeBtn.Foreground = (Brush)FindResource("TextPrimaryBrush");
                    closeBtn.Opacity = 1.0;
                };
                closeBtn.MouseLeave += (_, _) =>
                {
                    closeBtn.Foreground = (Brush)FindResource("TextMutedBrush");
                    closeBtn.Opacity = tab == _activeTab ? 0.8 : 0.4;
                };
                Grid.SetColumn(closeBtn, colOffset + 1);
                grid.Children.Add(closeBtn);
                closeBtn.Click += (_, _) => CloseTabWithAnimation(tab, border);
                border.MouseEnter += (_, _) =>
                {
                    if (tab != _activeTab) border.Background = (Brush)FindResource("BgHoverBrush");
                    closeBtn.Opacity = 0.8;
                };
                border.MouseLeave += (_, _) =>
                {
                    if (tab != _activeTab) border.Background = Brushes.Transparent;
                    if (tab != _activeTab) closeBtn.Opacity = 0;
                };
            }
            border.Child = grid;
            border.MouseDown += (_, e) =>
            {
                if (e.ChangedButton == MouseButton.Middle)
                {
                    e.Handled = true;
                    CloseTabWithAnimation(tab, border);
                    return;
                }
            };
            border.MouseLeftButtonDown += (_, e) =>
            {
                if (FindVisualParent<Button>(e.OriginalSource as DependencyObject) != null) return;
                _dragStartPoint = e.GetPosition(TabsPanel);
                _dragTab = tab;
                _isDragging = false;
                border.CaptureMouse();
                e.Handled = true;
            };
            border.MouseMove += (_, e) =>
            {
                if (_dragTab != tab || !border.IsMouseCaptured) return;
                var pos = e.GetPosition(TabsPanel);
                if (!_isDragging && Math.Abs(pos.X - _dragStartPoint.X) > 8)
                    _isDragging = true;
                if (!_isDragging) return;
                var posInWindow = e.GetPosition(this);
                var tabBarBottom = TabsPanel.TransformToAncestor(this)
                    .Transform(new Point(0, TabsPanel.ActualHeight)).Y;
                if (posInWindow.Y > tabBarBottom + 50 || posInWindow.Y < -25)
                {
                    border.ReleaseMouseCapture();
                    TearOutTab(tab, PointToScreen(posInWindow));
                    return;
                }
                int targetIdx = GetTabIndexAtX(pos.X);
                int currentIdx = _tabs.IndexOf(tab);
                if (targetIdx >= 0 && targetIdx != currentIdx)
                {
                    _tabs.RemoveAt(currentIdx);
                    _tabs.Insert(Math.Clamp(targetIdx, 0, _tabs.Count), tab);
                    UpdateTabsUI();
                    foreach (UIElement child in TabsPanel.Children)
                        if (child is Border nb && nb.Tag is BrowserTab t && t == tab)
                        { nb.CaptureMouse(); break; }
                    return;
                }
            };
            border.MouseLeftButtonUp += (_, e) =>
            {
                if (!border.IsMouseCaptured) return;
                border.ReleaseMouseCapture();
                if (!_isDragging) SwitchToTab(tab);
                _dragTab = null;
                _isDragging = false;
                UpdateTabsUI();
            };
            border.LostMouseCapture += (_, _) =>
            {
                if (!_isDragging) return;
                _isDragging = false;
                _dragTab = null;
            };
            border.MouseRightButtonUp += (_, e) =>
            {
                e.Handled = true;
                var menu = new ContextMenu();
                void Add(string h, Action a) { var mi = new MenuItem { Header = h }; mi.Click += (_,_) => a(); menu.Items.Add(mi); }
                Add(tab.IsPinned ? "Открепить" : "Закрепить", () => PinTab(tab));
                Add("Дублировать", () => CreateNewTab(tab.Url));
                Add(tab.IsMuted ? "Включить звук" : "Выключить звук", () => { if (tab.WebViewControl?.CoreWebView2 != null) { tab.IsMuted = !tab.IsMuted; tab.WebViewControl.CoreWebView2.IsMuted = tab.IsMuted; UpdateTabsUI(); } });
                menu.Items.Add(new Separator());
                Add("Закрыть", () => CloseTabWithAnimation(tab, border));
                Add("Закрыть остальные", () => { foreach (var t in _tabs.ToList()) if (t != tab) CloseTab(t); });
                Add("Закрыть справа", () => { var idx2 = _tabs.IndexOf(tab); foreach (var t in _tabs.Skip(idx2+1).ToList()) CloseTab(t); });
                menu.IsOpen = true;
            };
            TabsPanel.Children.Add(border);
        }
    }
    private void CloseTabWithAnimation(BrowserTab tab, Border border)
    {
        if (_tabs.Count <= 1) return;
        if (!ShouldAnimate) { CloseTab(tab); return; }
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
        var dur  = TimeSpan.FromMilliseconds(130);
        var scaleAnim = new DoubleAnimation(1, 0, dur) { EasingFunction = ease };
        var fadeAnim  = new DoubleAnimation(1, 0, dur) { EasingFunction = ease };
        var widthAnim = new DoubleAnimation(border.ActualWidth, 0, dur) { EasingFunction = ease };
        widthAnim.Completed += (_, _) => CloseTab(tab);
        border.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        border.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
        if (border.RenderTransform is ScaleTransform st)
            st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
    }
    private int GetTabIndexAtX(double x)
    {
        var borders = new List<(double mid, int tabIdx)>();
        int tabIdx = 0;
        foreach (UIElement child in TabsPanel.Children)
        {
            if (child is not Border b) continue;
            try
            {
                var pos = b.TransformToAncestor(TabsPanel).Transform(new Point(0, 0));
                borders.Add((pos.X + b.ActualWidth / 2.0, tabIdx));
            }
            catch { }
            tabIdx++;
        }
        if (borders.Count == 0) return -1;
        for (int i = 0; i < borders.Count; i++)
            if (x < borders[i].mid) return i;
        return borders.Count - 1;
    }
    private void TearOutTab(BrowserTab tab, Point screenPos)
    {
        if (_tabs.Count <= 1) return;
        _dragTab    = null;
        _isDragging = false;
        var targetWin = OpenWindows.FirstOrDefault(w =>
            w != this && IsPointInTabBar(w, screenPos));
        if (targetWin != null)
            TransferTabToWindow(tab, targetWin);
        else
            DetachTabToNewWindow(tab, screenPos);
    }
    private bool IsPointInTabBar(MainWindow win, Point screenPoint)
    {
        try
        {
            var tabBar = win.TabsPanel;
            var topLeft  = tabBar.PointToScreen(new Point(0, 0));
            var botRight = tabBar.PointToScreen(new Point(tabBar.ActualWidth, tabBar.ActualHeight + 30));
            return screenPoint.X >= topLeft.X && screenPoint.X <= botRight.X
                && screenPoint.Y >= topLeft.Y - 10 && screenPoint.Y <= botRight.Y;
        }
        catch { return false; }
    }
    private void TransferTabToWindow(BrowserTab tab, MainWindow target)
    {
        var url   = tab.Url;
        var title = tab.Title;
        int idx   = _tabs.IndexOf(tab);
        CloseTabSilent(tab, idx);
        target.Dispatcher.Invoke(() => target.AcceptTabFromWindow(url, title));
    }
    private void DetachTabToNewWindow(BrowserTab tab, Point screenPos)
    {
        var url   = tab.Url;
        var title = tab.Title;
        int idx   = _tabs.IndexOf(tab);
        CloseTabSilent(tab, idx);
        var newWin = new MainWindow
        {
            Left   = screenPos.X - 300,
            Top    = screenPos.Y - 20,
            Width  = this.Width,
            Height = this.Height,
            _pendingUrl = url
        };
        newWin.Show();
    }
    private void CloseTabSilent(BrowserTab tab, int idx)
    {
        if (tab.WebViewControl != null)
        {
            WebViewContainer.Children.Remove(tab.WebViewControl);
            tab.WebViewControl.Dispose();
            tab.WebViewControl = null;
        }
        _tabs.Remove(tab);
        TrimBackgroundRam();
        if (_activeTab == tab)
        {
            _activeTab = null;
            if (_tabs.Count > 0)
                SwitchToTab(_tabs[Math.Min(idx, _tabs.Count - 1)]);
            else
            {
                CreateNewTab();
                SaveSession();
                return;
            }
        }
        else
        {
            UpdateTabsUI();
        }
        SaveSession();
    }
    internal void AcceptTabFromWindow(string url, string title)
    {
        CreateNewTab(url);
        StatusText.Text = $"Вкладка «{title}» перемещена";
    }
    private void ApplyAccentColor(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(color);
            Application.Current.Resources["AccentBrush"] = brush;
            Application.Current.Resources["AccentColor"] = color;
            Application.Current.Resources["AccentBrushSemi"] = new SolidColorBrush(Color.FromArgb(42, color.R, color.G, color.B));
            var glow = new LinearGradientBrush();
            glow.StartPoint = new Point(0, 0);
            glow.EndPoint = new Point(1, 0);
            glow.GradientStops.Add(new GradientStop(Colors.Transparent, 0));
            glow.GradientStops.Add(new GradientStop(color, 0.5));
            glow.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
            Application.Current.Resources["GlowLine"] = glow;
            if (LogoGlow != null)   LogoGlow.Color    = color;
            if (HomeLogoGlow != null) HomeLogoGlow.Color = color;
            ApplyLogoColor(string.IsNullOrEmpty(_settings.LogoColor) ? hex : _settings.LogoColor);
            UpdateTabsUI();
        }
        catch { }
    }
    private void ApplyLogoColor(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(color);
            Application.Current.Resources["LogoBrush"] = brush;
            if (LogoIcon  != null) LogoIcon.Foreground  = brush;
            if (LogoText  != null) LogoText.Foreground  = brush;
            if (LogoGlow  != null) LogoGlow.Color       = color;
            if (HomeLogoText != null) HomeLogoText.Foreground = brush;
            if (HomeLogoGlow != null) HomeLogoGlow.Color = color;
        }
        catch { }
    }
    private void UpdateSecurityIndicator()
    {
        if (_activeTab == null || SecurityButton == null) return;
        bool isSec = _activeTab.IsSecure;
        SecurityButton.FontFamily = (FontFamily)(TryFindResource("IconFont") ?? new FontFamily("Segoe MDL2 Assets"));
        SecurityButton.Content = isSec ? "\uE72E" : "\uE7BA";
        SecurityButton.Foreground = isSec
            ? (Brush)FindResource("AccentGreenBrush")
            : (Brush)FindResource("AccentOrangeBrush");
        if (SecurityIcon != null)
        {
            SecurityIcon.FontFamily = (FontFamily)(TryFindResource("IconFont") ?? new FontFamily("Segoe MDL2 Assets"));
            SecurityIcon.Text = isSec ? "\uE72E" : "\uE7BA";
            SecurityIcon.Foreground = SecurityButton.Foreground;
        }
        if (SecurityText != null)
        {
            SecurityText.Text = isSec
                ? "Подключение защищено (HTTPS). Ваши данные зашифрованы."
                : "Подключение не защищено (HTTP). Не вводите конфиденциальные данные.";
        }
    }
    private void UpdateProxyIndicator()
    {
        try
        {
            if (ProxyIndicator != null)
            {
                if (_settings.ProxyEnabled && !string.IsNullOrWhiteSpace(_settings.ProxyHost))
                {
                    var port = _settings.ProxyPort > 0 ? _settings.ProxyPort : 8080;
                    ProxyIndicator.Text = $"[ Прокси: {_settings.ProxyType} {_settings.ProxyHost}:{port} ]";
                    ProxyIndicator.Visibility = Visibility.Visible;
                }
                else
                {
                    ProxyIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }
        catch { }
    }
    private void ShowHomePage()
    {
        if (HomeGreetingText != null)
        {
            var h = DateTime.Now.Hour;
            HomeGreetingText.Text = h < 5 ? "Доброй ночи" : h < 12 ? "Доброе утро" :
                                    h < 17 ? "Добрый день" : h < 22 ? "Добрый вечер" : "Доброй ночи";
        }
        _isOnHomePage = true;
        HideAllPages();
        WallpaperLayer.Visibility = Visibility.Visible;
        HomePage.Visibility = Visibility.Visible;
        ApplyHomePageCustomization();
        if (_settings.ShowHomepageWallpaper && _wallpaperCache != null
            && _wallpaperCachedPath == _settings.CustomWallpaperPath)
        {
            ApplyCachedWallpaper();
        }
        else if (_settings.ShowHomepageWallpaper && !string.IsNullOrEmpty(_settings.CustomWallpaperPath))
        {
            LoadWallpaper();
        }
        else
        {
            WallpaperImage.Visibility   = Visibility.Collapsed;
            WallpaperOverlay.Visibility = Visibility.Collapsed;
        }
        AnimateSlideIn(HomePage);
        AddressBox.Text = "";
        WebViewContainer.Visibility = Visibility.Visible;
        if (!string.IsNullOrEmpty(_settings.LiveWallpaperPath))
            StartLiveWallpaper(_settings.LiveWallpaperPath);
    }
    private void HideAllPages()
    {
        HomePage.Visibility       = Visibility.Collapsed;
        BlockedPage.Visibility    = Visibility.Collapsed;
        SettingsPage.Visibility   = Visibility.Collapsed;
        HistoryPage.Visibility    = Visibility.Collapsed;
        BookmarksPage.Visibility  = Visibility.Collapsed;
        if (DownloadsPage != null) DownloadsPage.Visibility = Visibility.Collapsed;
        if (FindPopup != null) FindPopup.IsOpen = false;
        if (FindPopup != null) FindPopup.IsOpen = false;
        WallpaperLayer.Visibility = Visibility.Collapsed;
        StopBlobAnimation();
        StopLiveWallpaper();
    }
    private void ShowBlockedPage(string reason)
    {
        HideAllPages();
        HideAllWebViews();
        BlockedPage.Visibility = Visibility.Visible;
        AnimateFadeIn(BlockedPage);
        BlockedReason.Text = reason;
        StatusText.Text = "Заблокировано";
    }
    private BitmapImage? _wallpaperCache;
    private string       _wallpaperCachedPath = "";
    private void LoadWallpaper()
    {
        ApplyHomePageCustomization();
        try
        {
            if (!_settings.ShowHomepageWallpaper || string.IsNullOrWhiteSpace(_settings.CustomWallpaperPath))
            {
                WallpaperImage.Source = null;
                WallpaperImage.Visibility = Visibility.Collapsed;
                WallpaperOverlay.Visibility = Visibility.Collapsed;
                _wallpaperCache = null;
                _wallpaperCachedPath = "";
                return;
            }
            if (!File.Exists(_settings.CustomWallpaperPath))
            {
                WallpaperImage.Source = null;
                WallpaperImage.Visibility = Visibility.Collapsed;
                WallpaperOverlay.Visibility = Visibility.Collapsed;
                return;
            }
            if (_wallpaperCache != null && _wallpaperCachedPath == _settings.CustomWallpaperPath)
            {
                ApplyCachedWallpaper();
                return;
            }
            var path = _settings.CustomWallpaperPath;
            Task.Run(() =>
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource        = new Uri(path);
                    bmp.CacheOption      = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 1920;
                    bmp.EndInit();
                    bmp.Freeze();
                    Dispatcher.Invoke(() =>
                    {
                        _wallpaperCache      = bmp;
                        _wallpaperCachedPath = path;
                        ApplyCachedWallpaper();
                    });
                }
                catch { }
            });
        }
        catch
        {
            WallpaperImage.Source = null;
            WallpaperImage.Visibility = Visibility.Collapsed;
            WallpaperOverlay.Visibility = Visibility.Collapsed;
        }
    }
    private void ApplyCachedWallpaper()
    {
        if (_wallpaperCache == null) return;
        WallpaperImage.Source     = _wallpaperCache;
        WallpaperImage.Opacity    = _settings.WallpaperOpacity / 100.0;
        WallpaperImage.Visibility = Visibility.Visible;
        WallpaperOverlay.Visibility = Visibility.Visible;
    }
    private void UpdateBookmarksBar()
    {
        BookmarksPanel.Children.Clear();
        if (_settings.Bookmarks?.Count == 0 || _settings.Bookmarks == null)
        {
            BookmarksBar.Visibility = Visibility.Collapsed;
            return;
        }
        BookmarksBar.Visibility = Visibility.Visible;
        foreach (var bm in _settings.Bookmarks ?? new())
        {
            var titleText = bm.Title.Length > 20 ? bm.Title[..19] + "…" : bm.Title;
            var contentBlock = new TextBlock();
            contentBlock.Inlines.Add(new Run("\uE728 ") { FontFamily = new FontFamily("Segoe MDL2 Assets") });
            contentBlock.Inlines.Add(new Run(titleText));
            var btn = new Button
            {
                Content = contentBlock,
                Tag = bm.Url,
                Style = (Style)FindResource("BookmarkBtn"),
                Margin = new Thickness(0, 0, 4, 0)
            };
            btn.Click += (_, _) => NavigateTo(bm.Url);
            btn.MouseRightButtonUp += (_, _) =>
            {
                (_settings.Bookmarks ??= new()).Remove(bm);
                SaveSettings();
                UpdateBookmarksBar();
                StatusText.Text = "Закладка удалена";
            };
            BookmarksPanel.Children.Add(btn);
        }
    }
    private void SaveSettings()
    {
        if (_isFullResetting) return;
        try
        {
            _settings.Save();
        }
        catch { }
    }
    private async Task SaveSettingsNow() => await _settings.SaveAsync();
    private void AddBookmark_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab == null || string.IsNullOrWhiteSpace(_activeTab.Url))
        {
            StatusText.Text = "Нечего добавить в закладки";
            return;
        }
        if (_settings.Bookmarks?.Any(b => b.Url == _activeTab.Url) == true)
        {
            StatusText.Text = "Уже в закладках";
            return;
        }
        (_settings.Bookmarks ??= new()).Add(new BookmarkItem
        {
            Title = _activeTab.Title,
            Url = _activeTab.Url
        });
        SaveSettings();
        UpdateBookmarksBar();
        StatusText.Text = "Добавлено в закладки";
    }
    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);
    private bool _ramWarningShown = false;
    private int _ramTrimCounter = 0;
    private void UpdateMemoryUsage(object? sender, EventArgs e)
    {
        try
        {
            if (++_ramTrimCounter >= 10)
            {
                _ramTrimCounter = 0;
                TrimBackgroundRam();
            }

            long totalBytes = 0;
            var countedPids = new HashSet<int>();

            using (var proc = Process.GetCurrentProcess())
            {
                totalBytes += proc.WorkingSet64;
                countedPids.Add(proc.Id);
            }

            if (_webViewEnv != null)
            {
                try
                {
                    var procInfos = _webViewEnv.GetProcessInfos();
                    if (procInfos != null)
                    {
                        foreach (var info in procInfos)
                        {
                            if (info != null && info.ProcessId > 0 && countedPids.Add(info.ProcessId))
                            {
                                try
                                {
                                    using var p = Process.GetProcessById(info.ProcessId);
                                    if (!p.HasExited)
                                        totalBytes += p.WorkingSet64;
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }

            foreach (var tab in _tabs)
            {
                try
                {
                    if (tab.WebViewControl?.CoreWebView2 != null)
                    {
                        int browserPid = (int)tab.WebViewControl.CoreWebView2.BrowserProcessId;
                        if (browserPid > 0 && countedPids.Add(browserPid))
                        {
                            try
                            {
                                using var p = Process.GetProcessById(browserPid);
                                if (!p.HasExited)
                                    totalBytes += p.WorkingSet64;
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }

            var mb = totalBytes / (1024.0 * 1024.0);
            MemoryUsage.Text = $"RAM: {mb:F0} MB";
            if (_settings.RamWarningThresholdMB > 0 && mb >= _settings.RamWarningThresholdMB)
            {
                if (!_ramWarningShown)
                {
                    _ramWarningShown = true;
                    AnimateStatusText($"Высокое использование ОЗУ: {mb:F0} МБ");
                }
            }
            else if (mb < _settings.RamWarningThresholdMB * 0.85)
            {
                _ramWarningShown = false;
            }
        }
        catch { }
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject src && IsClickOnInteractiveControl(src)) return;
        if (e.ClickCount == 2)
            ToggleMaximize();
        else if (e.LeftButton == MouseButtonState.Pressed)
            DragMoveSafe(e);
    }
    private void TitleBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (e.OriginalSource is not DependencyObject src) return;
        if (IsClickOnInteractiveControl(src)) return;
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            e.Handled = true;
            return;
        }
        DragMoveSafe(e);
        e.Handled = true;
    }
    private void DragMoveSafe(MouseButtonEventArgs e)
    {
        if (_isMaximized || WindowState == WindowState.Maximized)
        {
            var screenPoint = PointToScreen(e.GetPosition(this));
            var restoredWidth = _restoreBounds.Width > 0 ? _restoreBounds.Width : 1200;
            var ratioX = e.GetPosition(this).X / ActualWidth;
            _isMaximized = false;
            WindowState = WindowState.Normal;
            if (MainBorder != null)
            {
                MainBorder.Margin = new Thickness(6);
                MainBorder.CornerRadius = new CornerRadius(_settings.Theme == "Win7" ? 4 : 12);
                MainBorder.BorderThickness = new Thickness(1);
                try { MainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 24, ShadowDepth = 0, Opacity = 0.6, Color = System.Windows.Media.Colors.Black }; } catch { }
            }
            if (MaximizeButton != null) MaximizeButton.Content = "\uE922";
            SetResizeBordersVisibility(Visibility.Visible);
            Left = screenPoint.X - (restoredWidth * ratioX);
            Top  = screenPoint.Y - 10;
            Width = restoredWidth;
            Height = _restoreBounds.Height > 0 ? _restoreBounds.Height : 800;
        }
        try { DragMove(); } catch { }
    }
    private static bool IsClickOnInteractiveControl(DependencyObject? element)
    {
        var current = element;
        int steps = 0;
        while (current != null && steps++ < 100)
        {
            if (current is Button) return true;
            if (current is Border b && b.Tag is BrowserTab) return true;
            if (current is TextBox) return true;
            if (current is ScrollBar) return true;
            current = GetParentSafe(current);
        }
        return false;
    }
    private void ToggleMaximize()
    {
        if (_isMaximized)
        {
            _isMaximized = false;
            WindowState = WindowState.Normal;
            Left = _restoreBounds.Left;
            Top = _restoreBounds.Top;
            Width = _restoreBounds.Width > 0 ? _restoreBounds.Width : 1200;
            Height = _restoreBounds.Height > 0 ? _restoreBounds.Height : 800;
            if (MainBorder != null)
            {
                MainBorder.Margin = new Thickness(6);
                MainBorder.CornerRadius = new CornerRadius(_settings.Theme == "Win7" ? 4 : 12);
                MainBorder.BorderThickness = new Thickness(1);
                try { MainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 24, ShadowDepth = 0, Opacity = 0.6, Color = System.Windows.Media.Colors.Black }; } catch { }
            }
            if (MaximizeButton != null) MaximizeButton.Content = "\uE922";
            SetResizeBordersVisibility(Visibility.Visible);
        }
        else
        {
            _restoreBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
            _isMaximized = true;

            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(hwnd);
                var work = screen.WorkingArea;

                var source = PresentationSource.FromVisual(this);
                double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

                Left = work.Left / dpiX;
                Top = work.Top / dpiY;
                Width = work.Width / dpiX;
                Height = work.Height / dpiY;
            }
            catch
            {
                var work = SystemParameters.WorkArea;
                Left = work.Left;
                Top = work.Top;
                Width = work.Width;
                Height = work.Height;
            }

            if (MainBorder != null)
            {
                MainBorder.Margin = new Thickness(0);
                MainBorder.CornerRadius = new CornerRadius(0);
                MainBorder.BorderThickness = new Thickness(0);
                try { MainBorder.Effect = null; } catch { }
            }
            if (MaximizeButton != null) MaximizeButton.Content = "\uE923";
            SetResizeBordersVisibility(Visibility.Collapsed);
        }
    }
    private void SetResizeBordersVisibility(Visibility vis)
    {
        if (ResizeTop != null) ResizeTop.Visibility = vis;
        if (ResizeBottom != null) ResizeBottom.Visibility = vis;
        if (ResizeLeft != null) ResizeLeft.Visibility = vis;
        if (ResizeRight != null) ResizeRight.Visibility = vis;
        if (ResizeTopLeft != null) ResizeTopLeft.Visibility = vis;
        if (ResizeTopRight != null) ResizeTopRight.Visibility = vis;
        if (ResizeBottomLeft != null) ResizeBottomLeft.Visibility = vis;
        if (ResizeBottomRight != null) ResizeBottomRight.Visibility = vis;
    }
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private void ResizeBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isMaximized) return;
        if (sender is FrameworkElement fe && fe.Tag is string dir)
        {
            int code = dir switch
            {
                "Left" => 1,
                "Right" => 2,
                "Top" => 3,
                "TopLeft" => 4,
                "TopRight" => 5,
                "Bottom" => 6,
                "BottomLeft" => 7,
                "BottomRight" => 8,
                _ => 0
            };
            if (code > 0)
            {
                try
                {
                    var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    ReleaseCapture();
                    SendMessage(handle, 0x0112, (IntPtr)(0xF000 + code), IntPtr.Zero);
                    e.Handled = true;
                }
                catch { }
            }
        }
    }
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var source = System.Windows.Interop.HwndSource.FromHwnd(handle);
            source?.AddHook(WndProc);
        }
        catch { }
    }
    private const int WM_NCHITTEST = 0x0084;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST && !_isMaximized)
        {
            try
            {
                var screenPoint = new Point((short)(lParam.ToInt64() & 0xFFFF), (short)((lParam.ToInt64() >> 16) & 0xFFFF));
                var p = PointFromScreen(screenPoint);
                double border = 8.0;
                if (p.X >= 0 && p.X <= ActualWidth && p.Y >= 0 && p.Y <= ActualHeight)
                {
                    if (p.Y <= border)
                    {
                        if (p.X <= border * 2) { handled = true; return (IntPtr)HTTOPLEFT; }
                        if (p.X >= ActualWidth - border * 2) { handled = true; return (IntPtr)HTTOPRIGHT; }
                        handled = true; return (IntPtr)HTTOP;
                    }
                    if (p.Y >= ActualHeight - border)
                    {
                        if (p.X <= border * 2) { handled = true; return (IntPtr)HTBOTTOMLEFT; }
                        if (p.X >= ActualWidth - border * 2) { handled = true; return (IntPtr)HTBOTTOMRIGHT; }
                        handled = true; return (IntPtr)HTBOTTOM;
                    }
                    if (p.X <= border) { handled = true; return (IntPtr)HTLEFT; }
                    if (p.X >= ActualWidth - border) { handled = true; return (IntPtr)HTRIGHT; }
                }
            }
            catch { }
        }
        return IntPtr.Zero;
    }
    private void NewTab_Click(object sender, RoutedEventArgs e) => CreateNewTab();
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            if (!_isMaximized) ToggleMaximize();
        }
        else if (WindowState == WindowState.Normal && _isMaximized)
        {
            _isMaximized = false;
            if (MainBorder != null)
            {
                MainBorder.Margin = new Thickness(6);
                MainBorder.CornerRadius = new CornerRadius(_settings.Theme == "Win7" ? 4 : 12);
                MainBorder.BorderThickness = new Thickness(1);
                try { MainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 24, ShadowDepth = 0, Opacity = 0.6, Color = System.Windows.Media.Colors.Black }; } catch { }
            }
            if (MaximizeButton != null) MaximizeButton.Content = "\uE922";
            SetResizeBordersVisibility(Visibility.Visible);
        }
        if (WindowState == WindowState.Minimized)
        {
            try { EmptyWorkingSet(Process.GetCurrentProcess().Handle); } catch { }
        }
        else if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
        {
            foreach (var tab in _tabs)
            {
                if (tab.WebViewControl?.CoreWebView2 != null)
                {
                    try { tab.WebViewControl.CoreWebView2.Resume(); }
                    catch { }
                }
            }
            if (_activeTab?.WebViewControl != null)
            {
                _activeTab.WebViewControl.Width  = double.NaN;
                _activeTab.WebViewControl.Height = double.NaN;
            }
        }
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private bool _closeConfirmed = false;
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isFullResetting) return;
        if (_closeConfirmed) return;
        var realTabCount = _tabs.Count(t => t.Url != "nova://settings");
        if (_settings.ConfirmCloseMultipleTabs && realTabCount > 1)
        {
            var result = MessageBox.Show(
                $"Закрыть все {_tabs.Count} вкладки?",
                "Nova Browser",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }
        }
        _closeConfirmed = true;
        try
        {
            if (_isSettingsDirty)
            {
                OnApplySettingsClicked();
            }
        }
        catch { }
        try { if (_settings.RestoreLastSession) SaveSession(); } catch { }
        try { _settings.Save(); } catch { }
    }
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab?.WebViewControl?.CanGoBack == true)
            _activeTab.WebViewControl.GoBack();
    }
    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab?.WebViewControl?.CanGoForward == true)
            _activeTab.WebViewControl.GoForward();
    }
    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _activeTab?.WebViewControl?.Reload();
    }
    private void AddressBox_GotFocus(object sender, RoutedEventArgs e)
    {
        AddressBox.SelectAll();
        if (ShouldAnimate)
            AnimateAddressBarFocus(true);
    }
    private void AddressBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (ShouldAnimate)
            AnimateAddressBarFocus(false);
    }
    private void AddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SuggestPopup.IsOpen = false;
            Keyboard.ClearFocus();
            var text = AddressBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            if (text.Equals("nova://settings", StringComparison.OrdinalIgnoreCase))
            {
                ShowSettingsTab();
                e.Handled = true;
                return;
            }
            bool isUrl = text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                      || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                      || text.StartsWith("nova://", StringComparison.OrdinalIgnoreCase)
                      || text.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
                      || (!text.Contains(' ') && text.Contains('.') && text.Length > 3);
            if (isUrl)
            {
                var targetUrl = text;
                if (!targetUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    && !targetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    && !targetUrl.StartsWith("nova://", StringComparison.OrdinalIgnoreCase)
                    && !targetUrl.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                {
                    targetUrl = "https://" + targetUrl;
                }
                NavigateTo(targetUrl);
            }
            else
            {
                var engine = _settings?.SearchEngine ?? "Nova";
                if (IsNovaSearchEngine(engine))
                {
                    ExecuteNovaHtmlSearch(text);
                }
                else
                {
                    var searchUrl = SecurityHelper.NormalizeInput(text, engine);
                    NavigateTo(searchUrl);
                }
            }
        }
        else if (e.Key == Key.Escape)
        {
            SuggestPopup.IsOpen = false;
            AddressBox.Text = _activeTab?.WebViewControl?.Source?.ToString() ?? _activeTab?.Url ?? "";
            _activeTab?.WebViewControl?.Focus();
            e.Handled = true;
        }
    }
    private async void NavigateTo(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        var url = input.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("nova://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
        {
            if (!url.Contains(' ') && url.Contains('.'))
                url = "https://" + url;
            else
            {
                var engine = _settings?.SearchEngine ?? "Nova";
                if (IsNovaSearchEngine(engine))
                {
                    ExecuteNovaHtmlSearch(url);
                    return;
                }
                url = SecurityHelper.NormalizeInput(url, engine);
            }
        }
        if (string.IsNullOrEmpty(url) || url == "about:blank") return;
        var tab = _activeTab;
        if (tab == null) return;
        if (tab.WebViewControl == null)
            await CreateWebViewForTab(tab);
        if (_activeTab != tab) return;
        if (tab.WebViewControl?.CoreWebView2 == null)
        {
            for (int i = 0; i < 20 && tab.WebViewControl?.CoreWebView2 == null; i++)
                await Task.Delay(100);
        }
        if (tab.WebViewControl?.CoreWebView2 == null) return;
        HideAllPages();
        HideAllWebViews();
        if (WebViewContainer != null)
        {
            WebViewContainer.Visibility = Visibility.Visible;
            if (!WebViewContainer.Children.Contains(tab.WebViewControl))
                WebViewContainer.Children.Add(tab.WebViewControl);
        }
        tab.WebViewControl.Visibility = Visibility.Visible;
        tab.WebViewControl.Width  = double.NaN;
        tab.WebViewControl.Height = double.NaN;
        tab.WebViewControl.HorizontalAlignment = HorizontalAlignment.Stretch;
        tab.WebViewControl.VerticalAlignment = VerticalAlignment.Stretch;
        try { tab.WebViewControl.CoreWebView2.Resume(); } catch { }
        try
        {
            tab.WebViewControl.CoreWebView2.Navigate(url);
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            System.Diagnostics.Debug.WriteLine($"NavigateTo COMException: {ex.Message}");
            tab.WebViewControl = null;
            await CreateWebViewForTab(tab);
            try { tab.WebViewControl?.CoreWebView2?.Navigate(url); } catch { }
        }
        tab.Url = url;
        AddressBox.Text = (IsNovaUrl(url) || IsNovaHost(url)) ? "" : url;
        _isOnHomePage = false;
        AnimateNavigation();
    }
    private void Security_Click(object sender, RoutedEventArgs e)
    {
        SecurityInfoBar.Visibility = SecurityInfoBar.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettingsTabSafe();
    private void ShowSettingsTab() => OpenSettingsTabSafe();
    private void SearchEngine_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_settingsLoading) return;
        if (SearchEngineCombo.SelectedItem is ComboBoxItem item)
        {
            _settings.SearchEngine = item.Content?.ToString() ?? "Google";
            SaveSettings();
            if (StatusText != null)
                StatusText.Text = $"Поисковик: {_settings.SearchEngine}";
        }
    }
    private void Setting_Changed(object sender, RoutedEventArgs e)
    {
    }

    private void RamWarning_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settingsLoading) return;
        var val = (int)e.NewValue;
        _settings.RamWarningThresholdMB = val;
        if (RamWarningLabel != null) RamWarningLabel.Text = val.ToString();
        SaveSettings();
    }
    private void ApplyAdvancedSettings()
    {
        foreach (var tab in _tabs)
        {
            var wv  = tab.WebViewControl?.CoreWebView2;
            var wvw = tab.WebViewControl;
            if (wv == null) continue;
            try
            {
                wv.Settings.IsBuiltInErrorPageEnabled        = true;
                wv.Settings.IsPinchZoomEnabled               = true;
                wv.Settings.IsSwipeNavigationEnabled         = false;
                wv.Settings.AreBrowserAcceleratorKeysEnabled = true;
                wv.Settings.IsPasswordAutosaveEnabled        = _settings.EnableAutofill;
                wv.Settings.IsGeneralAutofillEnabled         = _settings.EnableAutofill;
                wv.Settings.IsScriptEnabled                  = _settings.JavaScriptEnabled;
                wv.Settings.AreDefaultScriptDialogsEnabled   = _settings.JavaScriptEnabled;
                if (!string.IsNullOrWhiteSpace(_settings.UserAgent))
                    wv.Settings.UserAgent = _settings.UserAgent;
            }
            catch { }
            try
            {
                wv.DefaultDownloadDialogCornerAlignment = CoreWebView2DefaultDownloadDialogCornerAlignment.TopLeft;
                if (_settings.ForceDarkMode)
                    wv.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
                else
                    wv.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
            }
            catch { }
        }
    }
    private void ExportSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title    = "Экспортировать настройки Nova Browser",
                Filter   = "Настройки Nova Browser|*.novasettings|JSON|*.json",
                FileName = $"NovaBrowser_Settings_{DateTime.Now:yyyyMMdd}.novasettings"
            };
            if (dlg.ShowDialog() != true) return;
            var json = System.Text.Json.JsonSerializer.Serialize(_settings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(dlg.FileName, json);
            AnimateStatusText("Настройки экспортированы ");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось экспортировать настройки:\n{ex.Message}",
                "Nova Browser", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void ImportSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Импортировать настройки Nova Browser",
                Filter = "Настройки Nova Browser|*.novasettings;*.json|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;
            var json = System.IO.File.ReadAllText(dlg.FileName);
            var imported = System.Text.Json.JsonSerializer.Deserialize<Models.BrowserSettings>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (imported == null)
            {
                MessageBox.Show("Файл повреждён или не является настройками Nova Browser.",
                    "Nova Browser", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = MessageBox.Show(
                "Импортировать эти настройки? Текущие настройки будут заменены.",
                "Nova Browser", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            imported.Bookmarks ??= new();
            imported.HomeQuickLinks ??= new();
            imported.LastSession ??= new();
            _settings = imported;
            _settings.Save();
            ApplySettingsToUI();
            ApplyTheme(_settings.Theme);
            ApplyUIFontFamily(_settings.UIFontFamily);
            ApplyAccentColor(_settings.AccentColor);
            ApplyBookmarksBarVisibility();
            ApplyLargeText();
            ApplyStatusBarVisibility();
            ApplyCompactMode();
            ApplyWindowOpacity();
            ApplyAdvancedSettings();
            RebuildCustomDomainCache();
            if (_isOnHomePage) ApplyHomePageCustomization();
            UpdateBookmarksBar();
            AnimateStatusText("Настройки импортированы ");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось импортировать настройки:\n{ex.Message}",
                "Nova Browser", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void SetDefaultBrowser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RegisterAsBrowser();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                { FileName = "ms-settings:defaultapps", UseShellExecute = true });
            AnimateStatusText("Откройте «Браузер» → выберите Nova Browser ");
        }
        catch (Exception ex) { AnimateStatusText($"Ошибка: {ex.Message}"); }
    }
    private static void RegisterAsBrowser()
    {
        var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
        if (string.IsNullOrEmpty(exe)) return;
        const string prog = "NovaBrowserHTML", appName = "Nova Browser";
        using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + prog))
            k.SetValue("", appName);
        using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + prog + @"\DefaultIcon"))
            k.SetValue("", exe + ",0");
        using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + prog + @"\shell\open\command"))
            k.SetValue("", "\"" + exe + "\" \"%1\"");
        using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\" + appName + @"\Capabilities"))
        { k.SetValue("ApplicationName", appName); k.SetValue("ApplicationDescription", "Nova Browser"); }
        using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\" + appName + @"\Capabilities\FileAssociations"))
            foreach (var ext in new[]{".htm",".html",".xht",".xhtml",".shtml"})
                k.SetValue(ext, prog);
        using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\" + appName + @"\Capabilities\URLAssociations"))
            foreach (var p in new[]{"http","https","ftp"}) k.SetValue(p, prog);
        using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
            k.SetValue(appName, @"Software\" + appName + @"\Capabilities");
        foreach (var proto in new[]{"http","https","ftp"}) {
            using var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + proto);
            k.SetValue("", "URL:" + proto.ToUpper() + " Protocol"); k.SetValue("URL Protocol", "");
            using var cmd = k.CreateSubKey(@"shell\open\command");
            cmd.SetValue("", "\"" + exe + "\" \"%1\"");
        }
    }
    private static string? PickColor(string? initialHex = null)
    {
        try
        {
            var dlg = new System.Windows.Forms.ColorDialog { FullOpen = true, AnyColor = true };
            if (!string.IsNullOrEmpty(initialHex))
                try { var c = (Color)ColorConverter.ConvertFromString(initialHex);
                      dlg.Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B); } catch { }
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            { var c = dlg.Color; return $"#{c.R:X2}{c.G:X2}{c.B:X2}"; }
        } catch { }
        return null;
    }
    private void AccentColorCustom_Click(object sender, RoutedEventArgs e)
    {
        var hex = PickColor(_settings.AccentColor);
        if (hex == null) return;
        _settings.AccentColor = hex; SaveSettings(); ApplyAccentColor(hex);
        AnimateStatusText($"Акцент: {hex}");
    }
    private void ResetUserAgent_Click(object sender, RoutedEventArgs e)
    {
        _settings.UserAgent = "";
        if (UserAgentBox != null) UserAgentBox.Text = "";
        SaveSettings();
        AnimateStatusText("User Agent сброшен");
    }
    private async void ClearBrowsingData_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Очистить куки, кеш и данные сайтов?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        foreach (var tab in _tabs)
        {
            if (tab.WebViewControl?.CoreWebView2 == null) continue;
            try
            {
                await tab.WebViewControl.CoreWebView2.Profile.ClearBrowsingDataAsync();
            }
            catch { }
        }
        AnimateStatusText("Данные очищены");
    }
    private void ClearHistory_Btn_Click(object sender, RoutedEventArgs e)
    {
        _settings.History.Clear();
        SaveSettings();
        if (HistoryListPanel != null) HistoryListPanel.Children.Clear();
        AnimateStatusText("История очищена");
    }
    private void ClearBookmarks_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Удалить все закладки?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        (_settings.Bookmarks ??= new()).Clear();
        SaveSettings();
        UpdateBookmarksBar();
        AnimateStatusText("Закладки очищены");
    }
    private void ChooseWallpaper_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            _settings.CustomWallpaperPath = dlg.FileName;
            _settings.ShowHomepageWallpaper = true;
            _wallpaperCache      = null;
            _wallpaperCachedPath = "";
            SaveSettings();
            LoadWallpaper();
        }
    }
    private void WallpaperOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settingsLoading) return;
        _settings.WallpaperOpacity = (int)e.NewValue;
        SaveSettings();
        if (WallpaperImage != null)
        {
            WallpaperImage.Opacity = e.NewValue / 100.0;
            if (WallpaperOverlay != null)
                WallpaperOverlay.Opacity = 1.0 - (e.NewValue / 200.0);
        }
    }
    private async void ClearData_Click(object sender, RoutedEventArgs e)
    {
        foreach (var tab in _tabs)
        {
            if (tab.WebViewControl?.CoreWebView2 != null)
                await tab.WebViewControl.CoreWebView2.Profile.ClearBrowsingDataAsync();
        }
        StatusText.Text = "Данные очищены";
    }
    private void CloseSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsTab = _tabs.FirstOrDefault(t => t.Url == "nova://settings");
        if (settingsTab != null && _tabs.Count > 1)
        {
            CloseTab(settingsTab);
        }
        else
        {
            SettingsPage.Visibility = Visibility.Collapsed;
            ShowHomePage();
        }
    }
    private void ApplySettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        ApplyAccentColor(_settings.AccentColor);
        ApplyStatusBarVisibility();
        ApplyCompactMode();
        ApplyWindowOpacity();
        ApplyTabRadius();
        if (_isOnHomePage) ApplyHomePageCustomization();
        AnimateStatusText(" Настройки применены");
    }
    private void SettingsTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var tag = btn.Tag?.ToString();
        var panels = new (UIElement panel, string key)[]
        {
            (TabGeneral,    "General"),
            (TabSecurity,   "Security"),
            (TabAppearance, "Appearance"),
            (TabAccess,     "Access"),
            (TabDownloads,  "Downloads"),
            (TabPerf,       "Perf"),
            (TabAdvanced,   "Advanced"),
        };
        foreach (var (panel, key) in panels)
        {
            var visible = key == tag;
            panel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (visible) AnimateSlideIn(panel, 160);
        }
        var activeStyle = (Style)FindResource("SettingsTabBtnActive");
        var normalStyle = (Style)FindResource("SettingsTabBtn");
        TabBtnGeneral.Style    = tag == "General"    ? activeStyle : normalStyle;
        TabBtnSecurity.Style   = tag == "Security"   ? activeStyle : normalStyle;
        TabBtnAppearance.Style = tag == "Appearance" ? activeStyle : normalStyle;
        TabBtnAccess.Style     = tag == "Access"     ? activeStyle : normalStyle;
        if (TabBtnDownloads != null) TabBtnDownloads.Style = tag == "Downloads" ? activeStyle : normalStyle;
        if (TabBtnPerf      != null) TabBtnPerf.Style      = tag == "Perf"      ? activeStyle : normalStyle;
        if (TabBtnAdvanced  != null) TabBtnAdvanced.Style  = tag == "Advanced"  ? activeStyle : normalStyle;
    }
    private void HomepageBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_settingsLoading) return;
        _settings.HomepageUrl = HomepageBox.Text.Trim();
        SaveSettings();
    }
    private void Home_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_settings.HomepageUrl))
            NavigateTo(_settings.HomepageUrl);
        else
            ShowHomePage();
    }
    private void Zoom_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_settingsLoading) return;
        if (ZoomCombo.SelectedItem is ComboBoxItem ci && int.TryParse(ci.Tag?.ToString(), out int z))
        {
            _settings.DefaultZoom = z;
            SaveSettings();
            ApplyZoomToActiveTabs();
        }
    }
    private void ApplyZoomToActiveTabs()
    {
        var factor = _settings.DefaultZoom / 100.0;
        foreach (var tab in _tabs)
        {
            if (tab.WebViewControl?.CoreWebView2 != null)
                tab.WebViewControl.ZoomFactor = factor;
        }
    }
    private void ChooseDownloadFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Выберите папку для загрузок",
            FileName = "Выберите папку",
            Filter = "Папка|*.none",
            CheckFileExists = false,
            CheckPathExists = true
        };
        if (dlg.ShowDialog() == true)
        {
            var folder = System.IO.Path.GetDirectoryName(dlg.FileName) ?? "";
            _settings.DownloadFolder = folder;
            if (DownloadFolderBox != null) DownloadFolderBox.Text = folder;
            SaveSettings();
        }
    }
    private void FontSize_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_settingsLoading) return;
        if (FontSizeCombo.SelectedItem is ComboBoxItem ci && int.TryParse(ci.Tag?.ToString(), out int sz))
        {
            _settings.FontSize = sz;
            SaveSettings();
            ApplyLargeText();
        }
    }
    private void FontFamily_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_settingsLoading) return;
        if (FontFamilyCombo.SelectedItem is ComboBoxItem ci && ci.Tag is string fontStr)
        {
            _settings.UIFontFamily = fontStr;
            ApplyUIFontFamily(fontStr);
            SaveSettings();
        }
    }
    private void ApplyUIFontFamily(string fontStr)
    {
        try
        {
            var family = new FontFamily(fontStr);
            var res = Application.Current.Resources;
            if (res.Contains("MainFont")) res.Remove("MainFont");
            res.Add("MainFont", family);
        }
        catch { }
    }
    private void ApplyLargeText()
    {
        double normal = _settings.FontSize;
        double large  = _settings.LargeText ? normal + 2 : normal;
        double sm     = _settings.LargeText ? normal     : normal - 1;
        Application.Current.Resources["UiFontSize"]   = large;
        Application.Current.Resources["UiFontSizeSm"] = sm;
    }
    private void ApplyBookmarksBarVisibility()
    {
        if (BookmarksBar != null)
            BookmarksBar.Visibility = _settings.ShowBookmarksBar && (_settings.Bookmarks?.Count ?? 0) > 0
                ? Visibility.Visible : Visibility.Collapsed;
    }
    private static readonly Dictionary<string, (string bg, string card, string surface, string input, string border, string text, string textSec, string textMuted, string titleBar, string navBar, string popup, string accent)> _themePalettes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dark"] = ("#0B0D14", "#141724", "#1A1D2E", "#10131E", "#262A3E", "#F4F6FC", "#949AB0", "#5E647D", "#0E101A", "#141724", "#1A1D2E", "#BEF47B"),
        ["Cyberpunk"] = ("#070B19", "#0D1326", "#121A35", "#0A0F20", "#1E2A4A", "#E0F7FF", "#7997B8", "#3E5575", "#0A0F20", "#0D1326", "#121A35", "#BEF47B"),
        ["Nord"] = ("#2E3440", "#3B4252", "#434C5E", "#2E3440", "#4C566A", "#ECEFF4", "#D8DEE9", "#4C566A", "#2E3440", "#3B4252", "#3B4252", "#88C0D0"),
        ["Dracula"] = ("#282A36", "#343746", "#44475A", "#21222C", "#6272A4", "#F8F8F2", "#BFBFBF", "#6272A4", "#21222C", "#282A36", "#343746", "#BD93F9"),
        ["Mocha"] = ("#1E1E2E", "#181825", "#313244", "#11111B", "#45475A", "#CDD6F4", "#A6ADC8", "#585B70", "#181825", "#1E1E2E", "#313244", "#CBA6F7"),
        ["Light"] = ("#F8FAFC", "#FFFFFF", "#F1F5F9", "#FFFFFF", "#CBD5E1", "#0F172A", "#475569", "#94A3B8", "#F1F5F9", "#FFFFFF", "#FFFFFF", "#BEF47B"),
        ["OneDark"] = ("#21252B", "#282C34", "#2F3440", "#1E2227", "#3E4451", "#ABB2BF", "#7F848E", "#5C6370", "#1E2227", "#282C34", "#2F3440", "#61AFEF"),
        ["TokyoNight"] = ("#1A1B26", "#24283B", "#2F354F", "#16161E", "#3B4261", "#C0CAF5", "#9AA5CE", "#565F89", "#16161E", "#24283B", "#2F354F", "#7AA2F7"),
        ["Gruvbox"] = ("#1D2021", "#282828", "#32302F", "#1D2021", "#504945", "#EBDBB2", "#BDAE93", "#928374", "#1D2021", "#282828", "#32302F", "#D79921"),
        ["Midnight"] = ("#050811", "#091024", "#0F1C3F", "#060A18", "#172E66", "#DDF0FF", "#8BACDE", "#49689E", "#060A18", "#091024", "#0F1C3F", "#00B4FF"),
        ["RosePine"] = ("#191724", "#1F1D2E", "#26233A", "#14131D", "#403D52", "#E0DEF4", "#908CAA", "#6E6A86", "#14131D", "#1F1D2E", "#26233A", "#EBBCBA"),
        ["Solarized"] = ("#002B36", "#073642", "#0E4857", "#00212B", "#256372", "#93A1A1", "#657B83", "#586E75", "#00212B", "#073642", "#0E4857", "#268BD2"),
        ["Evening"] = ("#1A1423", "#231B30", "#302542", "#15101C", "#483763", "#F3E8FF", "#BFA7D9", "#7D6696", "#15101C", "#231B30", "#302542", "#E0A070"),
        ["Win7"] = ("#0A192F", "#132845", "#1B3A63", "#0E2038", "#2C5A96", "#E6F1FF", "#97B4D6", "#587B9F", "#0E2038", "#132845", "#1B3A63", "#1E90FF"),
        ["NeonGreen"] = ("#051205", "#0A1F0A", "#102E10", "#040D04", "#1A4D1A", "#E6FFE6", "#8AE68A", "#478A47", "#040D04", "#0A1F0A", "#102E10", "#39FF14"),
        ["NeonPink"] = ("#140510", "#240A1D", "#330F2A", "#10040D", "#571A47", "#FFE6F9", "#E68AC8", "#8A4776", "#10040D", "#240A1D", "#330F2A", "#FF10F0"),
        ["NeonBlue"] = ("#050F1A", "#0A1D33", "#0F2B4D", "#040D17", "#1A4B80", "#E6F4FF", "#8ACBFF", "#4778A3", "#040D17", "#0A1D33", "#0F2B4D", "#00BFFF"),
        ["Forest"] = ("#0B140D", "#122116", "#1B3020", "#09100B", "#2C4F34", "#EBF5ED", "#96B89E", "#58755E", "#09100B", "#122116", "#1B3020", "#228B22"),
        ["Sakura"] = ("#1A0F14", "#2B1922", "#3B222F", "#140B0F", "#593347", "#FFF0F6", "#E6A8C2", "#8C5972", "#140B0F", "#2B1922", "#3B222F", "#FFB7C5"),
        ["Ocean"] = ("#06131A", "#0C2230", "#123247", "#050F14", "#1E5478", "#E6F7FF", "#85C9EB", "#4A7C94", "#050F14", "#0C2230", "#123247", "#006994"),
        ["Autumn"] = ("#170E08", "#26170E", "#382214", "#120B06", "#593620", "#FFF3EB", "#E0B392", "#8A644A", "#120B06", "#26170E", "#382214", "#D2691E"),
        ["Desert"] = ("#19150E", "#262016", "#382F20", "#14110B", "#574831", "#FFF9EB", "#DECEB0", "#87785F", "#14110B", "#262016", "#382F20", "#DEB887"),
        ["Mint"] = ("#091410", "#10241D", "#17362B", "#070F0C", "#265745", "#E8FFF6", "#8FE3C2", "#528F77", "#070F0C", "#10241D", "#17362B", "#3EB489"),
        ["Sunset"] = ("#170C08", "#26140E", "#381E15", "#120A06", "#572F21", "#FFF2EB", "#E8AFA0", "#8F6154", "#120A06", "#26140E", "#381E15", "#FF6B35"),
        ["Coffee"] = ("#140E0A", "#211710", "#2E2017", "#100B08", "#473223", "#F5EFEB", "#C7B2A3", "#7A6759", "#100B08", "#211710", "#2E2017", "#6F4E37"),
        ["Amber"] = ("#171206", "#261E0A", "#382C0E", "#120E05", "#574415", "#FFFBEB", "#E3CE8F", "#8C7B4D", "#120E05", "#261E0A", "#382C0E", "#FFBF00"),
        ["Copper"] = ("#17100B", "#261A12", "#38271B", "#120D09", "#573C2A", "#FFF4EB", "#DEB89E", "#8A6B54", "#120D09", "#261A12", "#38271B", "#B87333"),
        ["Galaxy"] = ("#100A19", "#1A1029", "#27183D", "#0D0814", "#402766", "#F6EBFF", "#BE9EDB", "#6F5787", "#0D0814", "#1A1029", "#27183D", "#9B59B6"),
        ["Matrix"] = ("#020A04", "#051408", "#081F0C", "#020803", "#0E3615", "#DDFADA", "#62D674", "#2B7337", "#020803", "#051408", "#081F0C", "#00FF41"),
        ["Steel"] = ("#111417", "#1B2026", "#252C36", "#0D1012", "#394452", "#EDF3F7", "#A4B4C4", "#617180", "#0D1012", "#1B2026", "#252C36", "#708090"),
        ["Slate"] = ("#111019", "#1B1929", "#26233B", "#0D0C14", "#3D375E", "#EFEBF7", "#AEA6C9", "#675F82", "#0D0C14", "#1B1929", "#26233B", "#6A5ACD"),
        ["HighContrast"] = ("#000000", "#111111", "#222222", "#000000", "#FFFFFF", "#FFFFFF", "#E0E0E0", "#999999", "#000000", "#111111", "#111111", "#FFFFFF"),
        ["BloodMoon"] = ("#170505", "#260A0A", "#380E0E", "#120404", "#5C1717", "#FFE6E6", "#E88787", "#8A4949", "#120404", "#260A0A", "#380E0E", "#DC143C"),
        ["PastelDark"] = ("#16151F", "#201E2E", "#2B283D", "#12111A", "#433E61", "#F5F3FA", "#BAB4D4", "#716B87", "#12111A", "#201E2E", "#2B283D", "#B5A8FF"),
        ["Lavender"] = ("#14101A", "#201A2B", "#2D243D", "#100D14", "#483961", "#F6F0FA", "#C2AFD6", "#726185", "#100D14", "#201A2B", "#2D243D", "#967BB6"),
        ["Coral"] = ("#190D0D", "#291515", "#3D1F1F", "#140A0A", "#5E3030", "#FFF0F0", "#E8A5A5", "#8C5656", "#140A0A", "#291515", "#3D1F1F", "#FF6B6B"),
        ["Spring"] = ("#0E1410", "#16211A", "#1F2E24", "#0B100D", "#304A39", "#EBF7F0", "#9BC4A9", "#587A64", "#0B100D", "#16211A", "#1F2E24", "#7EC8E3"),
        ["Volcanic"] = ("#1A0A05", "#2B1109", "#3D180D", "#140804", "#612715", "#FFF0EB", "#E89F87", "#8C5441", "#140804", "#2B1109", "#3D180D", "#FF4500"),
        ["Cobalt"] = ("#050C1A", "#09142E", "#0E1E42", "#040A14", "#17316E", "#E6EFFF", "#8AABEB", "#47649E", "#040A14", "#09142E", "#0E1E42", "#0047AB"),
        ["Emerald"] = ("#07140B", "#0C2414", "#12331C", "#051009", "#1D522D", "#E6FFF0", "#8AE8A7", "#47965E", "#051009", "#0C2414", "#12331C", "#50C878"),
        ["Arctic"] = ("#0C161C", "#13232E", "#1A3240", "#091117", "#284D63", "#ECF8FF", "#9DD4ED", "#567F91", "#091117", "#13232E", "#1A3240", "#87CEEB"),
        ["MidnightPurple"] = ("#0F0817", "#1A0E29", "#26153B", "#0C0612", "#3E2261", "#F7EDFF", "#BE96E8", "#6D5187", "#0C0612", "#1A0E29", "#26153B", "#6A0DAD"),
        ["Golden"] = ("#171405", "#262109", "#38310E", "#121004", "#594E17", "#FFFEEB", "#E3DA84", "#8C8445", "#121004", "#262109", "#38310E", "#FFD700"),
        ["Custom"] = ("#0B0D14", "#141724", "#1A1D2E", "#10131E", "#262A3E", "#F4F6FC", "#949AB0", "#5E647D", "#0E101A", "#141724", "#1A1D2E", "#BEF47B"),
    };

    private void ApplyTheme(string theme)
    {
        _settings.Theme = theme;
        SaveSettings();

        if (!_themePalettes.TryGetValue(theme, out var pal))
            pal = _themePalettes["Dark"];

        string bg = pal.bg;
        string card = pal.card;
        string surface = pal.surface;
        string input = pal.input;
        string border = pal.border;
        string text = pal.text;
        string textSec = pal.textSec;
        string textMuted = pal.textMuted;
        string titleBar = pal.titleBar;
        string navBar = pal.navBar;
        string popup = pal.popup;
        string defaultAccent = pal.accent;

        if (!string.IsNullOrEmpty(_settings.CustomTitleBarColor)) titleBar = _settings.CustomTitleBarColor;
        if (!string.IsNullOrEmpty(_settings.CustomNavBarColor)) navBar = _settings.CustomNavBarColor;
        if (!string.IsNullOrEmpty(_settings.CustomAddressBarColor)) input = _settings.CustomAddressBarColor;
        if (!string.IsNullOrEmpty(_settings.CustomCardColor)) card = _settings.CustomCardColor;
        if (!string.IsNullOrEmpty(_settings.CustomPopupColor)) popup = _settings.CustomPopupColor;
        if (!string.IsNullOrEmpty(_settings.CustomBorderColor)) border = _settings.CustomBorderColor;
        if (!string.IsNullOrEmpty(_settings.CustomTextColor)) text = _settings.CustomTextColor;
        if (!string.IsNullOrEmpty(_settings.CustomTextSecondaryColor)) textSec = _settings.CustomTextSecondaryColor;

        try
        {
            var res = Application.Current?.Resources;
            if (res != null)
            {
                void SetBrushResource(string key, string hex)
                {
                    try
                    {
                        var col = (Color)ColorConverter.ConvertFromString(hex);
                        res[key] = new SolidColorBrush(col);
                    }
                    catch { }
                }
                void SetColorResource(string key, string hex)
                {
                    try
                    {
                        res[key] = (Color)ColorConverter.ConvertFromString(hex);
                    }
                    catch { }
                }

                SetColorResource("BgColor", bg);
                SetBrushResource("BgDarkBrush", bg);
                SetBrushResource("BgCardBrush", card);
                SetBrushResource("BgSurfaceBrush", surface);
                SetBrushResource("BgInputBrush", input);
                SetBrushResource("BorderBrush", border);
                SetBrushResource("TextPrimaryBrush", text);
                SetBrushResource("TextSecondaryBrush", textSec);
                SetBrushResource("TextMutedBrush", textMuted);
                SetBrushResource("TitleBarBrush", titleBar);
                SetBrushResource("NavBarBrush", navBar);
                SetBrushResource("BookmarksBarBrush", navBar);
                SetBrushResource("PopupBrush", popup);
                SetBrushResource("BgHoverBrush", surface);

                if (!string.IsNullOrEmpty(_settings.CustomGlowColor))
                {
                    try
                    {
                        var glowCol = (Color)ColorConverter.ConvertFromString(_settings.CustomGlowColor);
                        var glowGrad = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
                        glowGrad.GradientStops.Add(new GradientStop(Colors.Transparent, 0));
                        glowGrad.GradientStops.Add(new GradientStop(glowCol, 0.5));
                        glowGrad.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
                        res["GlowLine"] = glowGrad;
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(_settings.AccentColor))
                {
                    _settings.AccentColor = defaultAccent;
                }

                if (TitleBarGrid != null && res["TitleBarBrush"] is Brush tbBrush) TitleBarGrid.Background = tbBrush;
                if (NavBarGrid != null && (res["NavBarBrush"] ?? res["BgCardBrush"]) is Brush nbBrush) NavBarGrid.Background = nbBrush;
                if (AddressBox != null && res["BgInputBrush"] is Brush ibBrush) AddressBox.Background = ibBrush;
                if (StatusBarGrid != null && res["TitleBarBrush"] is Brush sbBrush) StatusBarGrid.Background = sbBrush;
                if (BookmarksBar != null && (res["NavBarBrush"] ?? res["BgCardBrush"]) is Brush bmbBrush) BookmarksBar.Background = bmbBrush;
                try { ApplyAllGradients(); } catch { }
            }
        }
        catch { }

        try
        {
            var merged = Application.Current?.Resources?.MergedDictionaries;
            if (merged != null)
            {
                for (int i = merged.Count - 1; i >= 2; i--)
                    merged.RemoveAt(i);
                if (merged.Count < 2)
                    try { merged.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Styles/ControlStyles.xaml") }); } catch { }
            }
        }
        catch { }

        ApplyAccentColor(_settings.AccentColor);
        UpdateThemeCards();
        ApplyThemeWallpaper(theme);
        if (MainBorder != null)
        {
            bool isLight = theme == "Light" || theme == "Win7";
            MainBorder.CornerRadius    = new CornerRadius(theme == "Win7" ? 4 : 12);
            MainBorder.BorderThickness = new Thickness(theme == "Win7" ? 1.5 : 1);
            if (theme == "Win7")
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x5C, 0x8F, 0xB8));
            else
                MainBorder.BorderBrush = Application.Current?.Resources["BorderBrush"] as Brush ?? Brushes.Transparent;
        }
        AnimateFadeIn(SettingsPage);
        var names = new Dictionary<string, string>
        {
            ["Dark"]       = "Nova Dark",
            ["Nord"]       = "Nord",
            ["Mocha"]      = "Mocha",
            ["Dracula"]    = "Dracula",
            ["OneDark"]    = "One Dark",
            ["TokyoNight"] = "Tokyo Night",
            ["Gruvbox"]    = "Gruvbox",
            ["Midnight"]   = "Midnight",
            ["RosePine"]   = "Rose Pine",
            ["Solarized"]  = "Solarized",
            ["Evening"]    = "Evening",
            ["Light"]      = "Светлая",
            ["Win7"]       = "Windows 7",
        };
        if (StatusText != null)
            StatusText.Text = $"Тема «{(names.TryGetValue(theme, out var n) ? n : theme)}» применена";
    }
    private void UpdateThemeCards() { }
    private void ThemeDark_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Dark");
    private void ThemeNord_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Nord");
    private void ThemeMocha_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Mocha");
    private void ThemeDracula_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Dracula");
    private void ThemeOneDark_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("OneDark");
    private void ThemeTokyo_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("TokyoNight");
    private void ThemeGruvbox_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Gruvbox");
    private void ThemeMidnight_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Midnight");
    private void ThemeRosePine_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("RosePine");
    private void ThemeSolarized_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Solarized");
    private void ThemeEvening_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Evening");
    private void ThemeLight_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Light");
    private void ThemeWin7_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Win7");
    private void ThemeCyberpunk_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Cyberpunk");
    private void ThemeNeonGreen_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("NeonGreen");
    private void ThemeNeonPink_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("NeonPink");
    private void ThemeNeonBlue_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("NeonBlue");
    private void ThemeForest_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Forest");
    private void ThemeSakura_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Sakura");
    private void ThemeOcean_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Ocean");
    private void ThemeAutumn_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Autumn");
    private void ThemeDesert_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Desert");
    private void ThemeMint_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Mint");
    private void ThemeSunset_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Sunset");
    private void ThemeCoffee_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Coffee");
    private void ThemeAmber_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Amber");
    private void ThemeCopper_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Copper");
    private void ThemeGalaxy_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Galaxy");
    private void ThemeMatrix_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Matrix");
    private void ThemeSteel_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Steel");
    private void ThemeSlate_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Slate");
    private void ThemeHighContrast_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("HighContrast");
    private void ThemeBloodMoon_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("BloodMoon");
    private void ThemePastelDark_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("PastelDark");
    private void ThemeLavender_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Lavender");
    private void ThemeCoral_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Coral");
    private void ThemeSpring_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Spring");
    private void ThemeVolcanic_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Volcanic");
    private void ThemeCobalt_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Cobalt");
    private void ThemeEmerald_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Emerald");
    private void ThemeArctic_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Arctic");
    private void ThemeMidnightPurple_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("MidnightPurple");
    private void ThemeGolden_Click(object sender, MouseButtonEventArgs e) => ApplyTheme("Golden");
    private void CloseSecurityBar_Click(object sender, RoutedEventArgs e)
    {
        SecurityInfoBar.Visibility = Visibility.Collapsed;
    }
    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = !MenuPopup.IsOpen;
    }
    private void MenuHome_Click(object sender, RoutedEventArgs e) { MenuPopup.IsOpen = false; Home_Click(sender, e); }
    private void MenuNewTab_Click(object sender, RoutedEventArgs e) { MenuPopup.IsOpen = false; CreateNewTab(); }
    private void MenuRefresh_Click(object sender, RoutedEventArgs e) { MenuPopup.IsOpen = false; Refresh_Click(sender, e); }
    private void MenuAddBookmark_Click(object sender, RoutedEventArgs e) { MenuPopup.IsOpen = false; AddBookmark_Click(sender, e); }
    private void MenuSettings_Click(object sender, RoutedEventArgs e) { MenuPopup.IsOpen = false; ShowSettingsTab(); }
    private void MenuSecurity_Click(object sender, RoutedEventArgs e) { MenuPopup.IsOpen = false; Security_Click(sender, e); }
    private void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        var info =
            "Nova Browser\n" +
            "Версия: 1.0 Beta 1\n\n" +
            "Быстрый, безопасный и кастомизируемый браузер\n" +
            "на основе Microsoft WebView2 (Chromium).\n\n" +
            "Это бета-версия. Возможны баги.\n" +
            "© 2025 Nova Team";
        MessageBox.Show(info, "О Nova Browser", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void MenuExtensions_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        ShowExtensionsStore();
    }
    private void ShowExtensionsStore()
    {
        var storeTab = _tabs.FirstOrDefault(t =>
            t.Url.Contains("chromewebstore.google.com") ||
            t.Url.Contains("nova://extensions"));
        if (storeTab != null) { SwitchToTab(storeTab); return; }
        var result = MessageBox.Show(
            "Выберите магазин расширений:\n\n" +
            "• Да  — Chrome Web Store\n" +
            "• Нет — Яндекс Браузер дополнения\n" +
            "• Отмена — отмена",
            "Магазин расширений",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        string? url = result switch
        {
            MessageBoxResult.Yes    => "https://chromewebstore.google.com",
            MessageBoxResult.No     => "https://addons.opera.com/ru/extensions/",
            _                       => null
        };
        if (url != null) CreateNewTab(url);
    }
    private void OpenChromeStore_Click(object sender, RoutedEventArgs e)
    { if (MenuPopup != null) MenuPopup.IsOpen = false; CreateNewTab("https://chromewebstore.google.com"); }
    private void OpenYandexStore_Click(object sender, RoutedEventArgs e)
        => CreateNewTab("https://addons.opera.com/ru/extensions/");
    private void OpenFirefoxStore_Click(object sender, RoutedEventArgs e)
    { if (MenuPopup != null) MenuPopup.IsOpen = false; CreateNewTab("https://addons.mozilla.org/ru/firefox/extensions/"); }
    private bool _focusMode = false;
    private void ToggleFocusMode()
    {
        _focusMode = !_focusMode;
        if (BookmarksBar != null)
            BookmarksBar.Visibility = _focusMode ? Visibility.Collapsed :
                (_settings.ShowBookmarksBar ? Visibility.Visible : Visibility.Collapsed);
        if (StatusBarGrid != null)
            StatusBarGrid.Visibility = _focusMode ? Visibility.Collapsed :
                (_settings.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed);
        AnimateStatusText(_focusMode ? "Режим фокуса ВКЛ" : "Режим фокуса ВЫКЛ");
    }
    private void MenuFocusMode_Click(object sender, RoutedEventArgs e)
    { MenuPopup.IsOpen = false; ToggleFocusMode(); }
    private void MenuDuplicateTab_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (_activeTab != null) CreateNewTab(_activeTab.Url);
    }
    private void MenuCopyUrl_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (_activeTab?.Url is string url && !string.IsNullOrEmpty(url))
        {
            try { Clipboard.SetText(url); AnimateStatusText("URL скопирован"); }
            catch { }
        }
    }
    private void MenuOpenExternal_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (_activeTab?.Url is string url && !string.IsNullOrEmpty(url))
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
    }
    private void MenuNewPrivateTab_Click(object sender, RoutedEventArgs e)
    { MenuPopup.IsOpen = false; CreatePrivateTab(); }
    private void MenuCloseRight_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (_activeTab == null) return;
        var idx = _tabs.IndexOf(_activeTab);
        foreach (var t in _tabs.Skip(idx + 1).ToList()) CloseTab(t);
    }
    private string _lastClosedUrl = "";
    private string _lastClosedTitle = "";
    private void RememberLastClosed(BrowserTab tab)
    {
        _lastClosedUrl   = tab.Url;
        _lastClosedTitle = tab.Title;
    }
    private void MenuReopenTab_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (!string.IsNullOrEmpty(_lastClosedUrl))
            CreateNewTab(_lastClosedUrl);
    }
    private void MenuMuteTab_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (_activeTab?.WebViewControl?.CoreWebView2 != null)
        {
            _activeTab.IsMuted = !_activeTab.IsMuted;
            _activeTab.WebViewControl.CoreWebView2.IsMuted = _activeTab.IsMuted;
            UpdateTabsUI();
            AnimateStatusText(_activeTab.IsMuted ? "Вкладка замучена" : "Звук включён");
        }
    }
    private void MenuPinTab_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (_activeTab != null) PinTab(_activeTab);
    }
    private void AccentColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            _settings.AccentColor = hex;
            SaveSettings();
            ApplyAccentColor(hex);
        }
    }
    private void LogoColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            if (LogoColorBox != null) LogoColorBox.Text = hex;
            _settings.LogoColor = hex;
            SaveSettings();
            ApplyLogoColor(hex);
        }
    }
    private void LogoColorApply_Click(object sender, RoutedEventArgs e)
    {
        var hex = LogoColorBox?.Text?.Trim() ?? "";
        if (!IsValidHex(hex)) { AnimateStatusText("Некорректный цвет"); return; }
        _settings.LogoColor = hex;
        SaveSettings();
        ApplyLogoColor(hex);
        AnimateStatusText("Цвет логотипа применён");
    }
    private void LogoColorReset_Click(object sender, RoutedEventArgs e)
    {
        _settings.LogoColor = "";
        if (LogoColorBox != null) LogoColorBox.Text = _settings.AccentColor;
        SaveSettings();
        ApplyLogoColor(_settings.AccentColor);
        AnimateStatusText("Цвет логотипа сброшен");
    }
    private CustomPopupPlacement[] MenuPopup_Placement(Size popupSize, Size targetSize, Point _)
    {
        double popupW = popupSize.Width > 0 ? popupSize.Width : 350;
        double offsetX = targetSize.Width - popupW;
        if (MenuScrollViewer != null)
            MenuScrollViewer.MaxHeight = 580;
        return new[] { new CustomPopupPlacement(
            new Point(offsetX, targetSize.Height + 4),
            PopupPrimaryAxis.Vertical) };
    }
    private void MenuScroll_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (MenuScrollViewer == null) return;
        MenuScrollViewer.ScrollToVerticalOffset(
            MenuScrollViewer.VerticalOffset - e.Delta / 3.0);
        e.Handled = true;
    }
    private void ResizeGrip_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        Height = Math.Max(MinHeight, Height + e.VerticalChange);
    }
    private void TabScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }
    private void HomeSearchBtn_Click(object sender, RoutedEventArgs e)
    {
        if (HomeSearchBox != null && !string.IsNullOrWhiteSpace(HomeSearchBox.Text))
            NavigateTo(HomeSearchBox.Text.Trim());
    }
    private void HomeSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(HomeSearchBox.Text))
        {
            var query = HomeSearchBox.Text.Trim();
            HomeSearchBox.Text = "";
            AddressBox.Text    = "";
            ExecuteNovaHtmlSearch(query);
            e.Handled = true;
        }
    }
    private void QuickLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string url)
            NavigateTo(url);
    }
    private void ApplyHomePageCustomization()
    {
        bool presetActive = _settings.WallpaperPresetIndex >= 0;
        bool wallpaperActive = _settings.ShowHomepageWallpaper
                                || !string.IsNullOrEmpty(_settings.LiveWallpaperPath)
                                || presetActive;
        if (HomePageBgBrush != null)
            HomePageBgBrush.Opacity = wallpaperActive ? 0 : 1;
        if (presetActive)
        {
            if (_themeBlobs.TryGetValue("Custom", out _))
                ApplyThemeWallpaper("Custom");
            else
            {
                var idx = _settings.WallpaperPresetIndex;
                if (idx >= 0 && idx < _wpPresets.Length)
                {
                    var (_, c1, c2) = _wpPresets[idx];
                    _themeBlobs["Custom"] = (c1, c2,
                        BlendHex(c1, c2, 0.5),
                        BlendHex(c2, _settings.AccentColor, 0.4));
                    ApplyThemeWallpaper("Custom");
                }
            }
        }
        else
        {
            StopBlobAnimation();
        }
        switch (_settings.HomeBackgroundStyle)
        {
            case "Image":
                HomeBlobCanvas.Visibility = Visibility.Collapsed;
                if (!string.IsNullOrEmpty(_settings.HomeBackgroundImage)
                    && System.IO.File.Exists(_settings.HomeBackgroundImage))
                {
                    try
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.UriSource   = new Uri(_settings.HomeBackgroundImage);
                        bmp.EndInit();
                        HomeBgImage.Source     = bmp;
                        HomeBgImage.Opacity    = _settings.HomeBgImageOpacity;
                        HomeBgImage.Visibility = Visibility.Visible;
                    }
                    catch { HomeBgImage.Visibility = Visibility.Collapsed; }
                }
                else
                {
                    HomeBgImage.Visibility = Visibility.Collapsed;
                }
                break;
            case "Solid":
                HomeBlobCanvas.Visibility = Visibility.Collapsed;
                HomeBgImage.Visibility    = Visibility.Collapsed;
                break;
            default:
                HomeBgImage.Visibility    = Visibility.Collapsed;
                HomeBlobCanvas.Visibility = wallpaperActive ? Visibility.Collapsed : Visibility.Visible;
                break;
        }
        HomeSearchBorder.Visibility = _settings.HomeShowSearchBar ? Visibility.Visible : Visibility.Collapsed;
        HomeQuickLinksPanel.Visibility = _settings.HomeShowQuickLinks ? Visibility.Visible : Visibility.Collapsed;
        RebuildHomeQuickLinks();
    }
    private void RebuildHomeQuickLinks()
    {
        if (HomeQuickLinksPanel == null) return;
        HomeQuickLinksPanel.Children.Clear();
        foreach (var link in (_settings.HomeQuickLinks ?? new()))
        {
            var btn = new Button
            {
                Content = link.Title,
                Tag     = link.Url,
                Style   = (Style)FindResource("HomeQuickLinkBtn")
            };
            btn.Click += QuickLink_Click;
            HomeQuickLinksPanel.Children.Add(btn);
        }
    }
    private void HomeBgChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Все файлы|*.*",
            Title  = "Выбрать фон главной страницы"
        };
        if (dlg.ShowDialog() == true)
        {
            _settings.HomeBackgroundImage = dlg.FileName;
            _settings.HomeBackgroundStyle = "Image";
            SaveSettings();
            if (HomeBgStyleCombo != null)
                foreach (ComboBoxItem item in HomeBgStyleCombo.Items)
                    if (item.Tag as string == "Image") { HomeBgStyleCombo.SelectedItem = item; break; }
            if (_isOnHomePage) ApplyHomePageCustomization();
            AnimateStatusText("Фон главной страницы обновлён");
        }
    }
    private void HomeBgStyle_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_settingsLoading) return;
        if (HomeBgStyleCombo.SelectedItem is ComboBoxItem ci && ci.Tag is string style)
        {
            _settings.HomeBackgroundStyle = style;
            SaveSettings();
            if (_isOnHomePage) ApplyHomePageCustomization();
        }
    }
    private void HomeBgOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settingsLoading) return;
        _settings.HomeBgImageOpacity = e.NewValue;
        SaveSettings();
        if (HomeBgImage != null) HomeBgImage.Opacity = e.NewValue;
    }
    private void ChkHomeShowSearch_Changed(object sender, RoutedEventArgs e)
    {
        if (_settingsLoading) return;
        _settings.HomeShowSearchBar = (sender as CheckBox)?.IsChecked == true;
        SaveSettings();
        if (_isOnHomePage) ApplyHomePageCustomization();
    }
    private void ChkHomeShowLinks_Changed(object sender, RoutedEventArgs e)
    {
        if (_settingsLoading) return;
        _settings.HomeShowQuickLinks = (sender as CheckBox)?.IsChecked == true;
        SaveSettings();
        if (_isOnHomePage) ApplyHomePageCustomization();
    }
    private void RefreshQuickLinksEditor()
    {
        if (QuickLinksEditList == null) return;
        QuickLinksEditList.Items.Clear();
        foreach (var link in (_settings.HomeQuickLinks ?? new()))
        {
            var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var text = new TextBlock
            {
                Text = $"{link.Title}  —  {link.Url}",
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(text, 0);
            var removeBtn = new Button
            {
                Content = "✕",
                Tag = link,
                Width = 28, Height = 28,
                Style = (Style)FindResource("NavButton")
            };
            removeBtn.Click += (s, ev) =>
            {
                var links = _settings.HomeQuickLinks;
                links?.Remove(link);
                SaveSettings();
                RefreshQuickLinksEditor();
                if (_isOnHomePage) RebuildHomeQuickLinks();
            };
            Grid.SetColumn(removeBtn, 1);
            row.Children.Add(text);
            row.Children.Add(removeBtn);
            QuickLinksEditList.Items.Add(row);
        }
    }
    private void AddQuickLink_Click(object sender, RoutedEventArgs e)
    {
        var title = QuickLinkTitleBox?.Text?.Trim();
        var url   = QuickLinkUrlBox?.Text?.Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url)) return;
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;
        var links = _settings.HomeQuickLinks;
        links?.Add(new Models.QuickLinkItem { Title = title, Url = url });
        SaveSettings();
        RefreshQuickLinksEditor();
        if (_isOnHomePage) RebuildHomeQuickLinks();
        if (QuickLinkTitleBox != null) QuickLinkTitleBox.Text = "";
        if (QuickLinkUrlBox != null) QuickLinkUrlBox.Text   = "";
    }
    private bool ShouldAnimate => _settings.ReduceAnimations == false;
    private static CubicEase     EaseOut    => new() { EasingMode = EasingMode.EaseOut };
    private static CubicEase     EaseInOut  => new() { EasingMode = EasingMode.EaseInOut };
    private static QuadraticEase EaseQuad   => new() { EasingMode = EasingMode.EaseOut };
    private static ElasticEase   EaseElastic=> new() { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 6 };
    private static BackEase      EaseBack   => new() { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
    private void AnimateFadeIn(UIElement el, double ms = 180)
    {
        if (!ShouldAnimate) { el.Opacity = 1; return; }
        el.Opacity = 0;
        el.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, Dur(ms)) { EasingFunction = EaseOut });
    }
    private void AnimateFadeOut(UIElement el, double ms = 160, Action? onDone = null)
    {
        if (!ShouldAnimate) { el.Opacity = 0; onDone?.Invoke(); return; }
        var a = new DoubleAnimation(1, 0, Dur(ms)) { EasingFunction = EaseOut };
        if (onDone != null) a.Completed += (_, _) => onDone();
        el.BeginAnimation(UIElement.OpacityProperty, a);
    }
    private void AnimateSlideIn(UIElement el, double ms = 200)
    {
        if (!ShouldAnimate) { el.RenderTransform = new TranslateTransform(); el.Opacity = 1; return; }
        var tt = new TranslateTransform(0, 14);
        el.RenderTransform = tt;
        el.Opacity = 0;
        tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(14, 0, Dur(ms)) { EasingFunction = EaseOut });
        el.BeginAnimation(UIElement.OpacityProperty,   new DoubleAnimation(0, 1, Dur(ms))   { EasingFunction = EaseOut });
    }
    private void AnimateSlideInFromLeft(UIElement el, double ms = 220)
    {
        if (!ShouldAnimate) { el.RenderTransform = new TranslateTransform(); el.Opacity = 1; return; }
        var tt = new TranslateTransform(-22, 0);
        el.RenderTransform = tt;
        el.Opacity = 0;
        tt.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-22, 0, Dur(ms)) { EasingFunction = EaseOut });
        el.BeginAnimation(UIElement.OpacityProperty,    new DoubleAnimation(0, 1, Dur(ms))   { EasingFunction = EaseOut });
    }
    private void AnimateSlideInFromRight(UIElement el, double ms = 220)
    {
        if (!ShouldAnimate) { el.RenderTransform = new TranslateTransform(); el.Opacity = 1; return; }
        var tt = new TranslateTransform(22, 0);
        el.RenderTransform = tt;
        el.Opacity = 0;
        tt.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(22, 0, Dur(ms)) { EasingFunction = EaseOut });
        el.BeginAnimation(UIElement.OpacityProperty,    new DoubleAnimation(0, 1, Dur(ms))  { EasingFunction = EaseOut });
    }
    private void AnimateScale(UIElement el, double from, double to, double ms = 150)
    {
        if (!ShouldAnimate) return;
        EnsureScale(el);
        var st = (ScaleTransform)el.RenderTransform;
        st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(from, to, Dur(ms)) { EasingFunction = EaseOut });
        st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(from, to, Dur(ms)) { EasingFunction = EaseOut });
    }
    private void AnimatePop(UIElement el)
    {
        if (!ShouldAnimate) { el.Opacity = 1; return; }
        AnimateScale(el, 0.93, 1.0, 200);
        AnimateFadeIn(el, 170);
    }
    private void AnimateTabSwitch(UIElement? el)
    {
        if (el == null) return;
        if (el is FrameworkElement fe) { fe.Width = double.NaN; fe.Height = double.NaN; }
        if (!ShouldAnimate) { el.Opacity = 1; return; }
        el.Opacity = 0;
        el.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, Dur(110)) { EasingFunction = EaseQuad });
    }
    private void AnimateDownloadToast(UIElement el)
    {
        if (!ShouldAnimate) { el.Opacity = 1; return; }
        var tt = new TranslateTransform(0, 24);
        el.RenderTransform = tt;
        el.Opacity = 0;
        tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(24, 0, Dur(300)) { EasingFunction = EaseBack });
        el.BeginAnimation(UIElement.OpacityProperty,    new DoubleAnimation(0, 1, Dur(200)));
    }
    private void AnimateAddressBarFocus(bool focused)
    {
        if (!ShouldAnimate || AddressBox == null) return;
        EnsureScale(AddressBox);
        var st  = (ScaleTransform)AddressBox.RenderTransform;
        var to  = focused ? 1.006 : 1.0;
        st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(st.ScaleX, to, Dur(200)) { EasingFunction = EaseOut });
        st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(st.ScaleY, to, Dur(200)) { EasingFunction = EaseOut });
    }
    private DispatcherTimer? _typingTimer;
    private bool _typingPulseActive;
    private void AnimateAddressBarKeystroke()
    {
        if (!ShouldAnimate || AddressBox == null) return;
        if (_typingPulseActive) return;
        _typingPulseActive = true;
        EnsureScale(AddressBox);
        var st   = (ScaleTransform)AddressBox.RenderTransform;
        var ease = EaseBack;
        st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.0, 1.004, Dur(60)) { EasingFunction = ease });
        st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, 1.004, Dur(60)) { EasingFunction = ease });
        _typingTimer?.Stop();
        _typingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(65) };
        _typingTimer.Tick += (_, _) =>
        {
            _typingTimer!.Stop();
            _typingPulseActive = false;
            if (AddressBox.RenderTransform is ScaleTransform s)
            {
                s.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(s.ScaleX, 1.0, Dur(120)) { EasingFunction = EaseOut });
                s.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(s.ScaleY, 1.0, Dur(120)) { EasingFunction = EaseOut });
            }
        };
        _typingTimer.Start();
    }
    private void AnimateNavigation()
    {
        if (!ShouldAnimate || AddressBox == null) return;
        AddressBox.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0.55, 1.0, Dur(180)) { EasingFunction = EaseOut });
    }
    private string          _pendingStatus  = "";
    private DispatcherTimer? _statusTimer;
    private int              _statusCharIdx;
    private void AnimateStatusText(string text, bool instant = false)
    {
        if (StatusText == null) return;
        if (!ShouldAnimate || instant || text.Length <= 3 || text == StatusText.Text)
        {
            StatusText.Text = text;
            return;
        }
        _pendingStatus = text;
        _statusCharIdx = 0;
        _statusTimer?.Stop();
        StatusText.Text = "";
        int interval = text.Length > 20 ? 14 : text.Length > 10 ? 20 : 28;
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(interval) };
        _statusTimer.Tick += (_, _) =>
        {
            if (_statusCharIdx <= _pendingStatus.Length)
                StatusText.Text = _pendingStatus[.._statusCharIdx++];
            else
                _statusTimer!.Stop();
        };
        _statusTimer.Start();
    }
    private void AnimateLoadingBarIn()
    {
        if (LoadingBar == null) return;
        LoadingBar.Visibility = Visibility.Visible;
        if (!ShouldAnimate) return;
        LoadingBar.Opacity = 0;
        LoadingBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Dur(140)));
    }
    private void AnimateLoadingBarOut()
    {
        if (LoadingBar == null) return;
        if (!ShouldAnimate) { LoadingBar.Visibility = Visibility.Collapsed; return; }
        var a = new DoubleAnimation(1, 0, Dur(280)) { EasingFunction = EaseOut };
        a.Completed += (_, _) => LoadingBar.Visibility = Visibility.Collapsed;
        LoadingBar.BeginAnimation(UIElement.OpacityProperty, a);
    }
    private void AnimatePopupOpen(FrameworkElement content)
    {
        if (!ShouldAnimate || content == null) return;
        var tg = new TransformGroup();
        var sc = new ScaleTransform(0.96, 0.96);
        var tt = new TranslateTransform(0, -6);
        tg.Children.Add(sc);
        tg.Children.Add(tt);
        content.RenderTransform       = tg;
        content.RenderTransformOrigin = new Point(0.5, 0);
        content.Opacity = 0;
        sc.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1.0, Dur(200)) { EasingFunction = EaseOut });
        sc.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1.0, Dur(200)) { EasingFunction = EaseOut });
        tt.BeginAnimation(TranslateTransform.YProperty,  new DoubleAnimation(-6, 0, Dur(180))    { EasingFunction = EaseOut });
        content.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Dur(160)));
    }
    private void AnimateTabActivate(Border tab)
    {
        if (!ShouldAnimate || tab == null) return;
        EnsureScale(tab, 0.5, 1.0);
        var st = (ScaleTransform)tab.RenderTransform;
        st.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.92, 1.0, Dur(280)) { EasingFunction = EaseElastic });
    }
    private void AnimateHomePageEntrance()
    {
        if (!ShouldAnimate) return;
        if (HomeLogoText != null)
        {
            var tt = new TranslateTransform(0, -18);
            HomeLogoText.RenderTransform = tt;
            HomeLogoText.Opacity = 0;
            tt.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(-18, 0, Dur(520)) { EasingFunction = EaseElastic });
            HomeLogoText.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Dur(280)));
        }
        if (HomeSearchBox != null)
        {
            var tt2 = new TranslateTransform(0, 10);
            HomeSearchBox.RenderTransform = tt2;
            HomeSearchBox.Opacity = 0;
            tt2.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(10, 0, Dur(400)) { EasingFunction = EaseOut, BeginTime = TimeSpan.FromMilliseconds(120) });
            HomeSearchBox.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, Dur(300)) { BeginTime = TimeSpan.FromMilliseconds(120) });
        }
    }
    private void AnimateSuggestItem(FrameworkElement item, int index)
    {
        if (!ShouldAnimate || item == null) return;
        var tt    = new TranslateTransform(0, 5);
        item.RenderTransform = tt;
        item.Opacity = 0;
        var delay = TimeSpan.FromMilliseconds(index * 22);
        tt.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(5, 0, Dur(130)) { EasingFunction = EaseOut, BeginTime = delay });
        item.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, Dur(110)) { BeginTime = delay });
    }
    private void AnimateButtonPress(Button btn)
    {
        if (!ShouldAnimate || btn == null) return;
        EnsureScale(btn);
        var st = (ScaleTransform)btn.RenderTransform;
        st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 0.87, Dur(70)));
        st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 0.87, Dur(70)));
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(72) };
        t.Tick += (_, _) =>
        {
            t.Stop();
            if (btn.RenderTransform is not ScaleTransform s) return;
            s.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(s.ScaleX, 1.0, Dur(220)) { EasingFunction = EaseBack });
            s.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(s.ScaleY, 1.0, Dur(220)) { EasingFunction = EaseBack });
        };
        t.Start();
    }
    private void AnimateNovaLogo()
    {
        if (!ShouldAnimate || LogoGlow == null) return;
        var pulse = new DoubleAnimation(0.3, 1.0, Dur(1800))
            { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever,
              EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
        LogoGlow.BeginAnimation(DropShadowEffect.OpacityProperty, pulse);
        if (LogoIcon != null)
        {
            EnsureScale(LogoIcon);
            var sc = (ScaleTransform)LogoIcon.RenderTransform;
            var sp = new DoubleAnimation(1.0, 1.1, Dur(1800))
                { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever,
                  EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, sp);
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, sp);
        }
    }
    private static Duration Dur(double ms) => new(TimeSpan.FromMilliseconds(ms));
    private static void EnsureScale(UIElement el, double cx = 0.5, double cy = 0.5)
    {
        if (el.RenderTransform is ScaleTransform existing && !existing.IsFrozen)
            return;
        double sx = 1, sy = 1;
        if (el.RenderTransform is ScaleTransform old)
        { sx = old.ScaleX; sy = old.ScaleY; }
        el.RenderTransform       = new ScaleTransform(sx, sy);
        el.RenderTransformOrigin = new Point(cx, cy);
    }
    private void AddToHistory(string title, string url)
    {
        if (_activeTab?.IsPrivate == true) return;
        if (string.IsNullOrWhiteSpace(url) || url == "about:blank" || IsNovaHost(url)) return;
        _settings.History ??= new System.Collections.Generic.List<NovaBrowser.Models.HistoryItem>();
        _settings.History.RemoveAll(h => h.Url == url);
        _settings.History.Insert(0, new NovaBrowser.Models.HistoryItem
        {
            Title     = string.IsNullOrWhiteSpace(title) ? url : title,
            Url       = url,
            VisitedAt = DateTime.Now
        });
        if (_settings.History.Count > 150)
            _settings.History.RemoveRange(150, _settings.History.Count - 150);
        _ = _settings.SaveHistoryAsync();
    }
    private void ShowHistoryPage()
    {
        HideAllPages();
        HideAllWebViews();
        HistoryPage.Visibility = Visibility.Visible;
        AnimateSlideIn(HistoryPage, 180);
        UpdateHistoryPage(HistorySearchBox?.Text ?? "");
    }
    private void UpdateHistoryPage(string filter = "")
    {
        HistoryListPanel.Children.Clear();
        _settings.History ??= new System.Collections.Generic.List<NovaBrowser.Models.HistoryItem>();
        var items = string.IsNullOrWhiteSpace(filter)
            ? _settings.History
            : _settings.History
                .Where(h => h.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)
                         || h.Url.Contains(filter,   StringComparison.OrdinalIgnoreCase))
                .ToList();
        if (items.Count == 0)
        {
            HistoryListPanel.Children.Add(new TextBlock
            {
                Text = "История пуста",
                Foreground = (Brush)FindResource("TextMutedBrush"),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0)
            });
            return;
        }
        string? lastDate = null;
        foreach (var item in items)
        {
            if (item == null) continue;
            var dateStr = item.VisitedAt.ToString("d MMMM yyyy");
            if (dateStr != lastDate)
            {
                lastDate = dateStr;
                HistoryListPanel.Children.Add(new TextBlock
                {
                    Text = dateStr,
                    Foreground = (Brush)FindResource("TextMutedBrush"),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 16, 0, 6)
                });
            }
            var row = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Cursor = Cursors.Hand
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var titleBlock = new TextBlock
            {
                Text = item.Title,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            var timeBlock = new TextBlock
            {
                Text = item.VisitedAt.ToString("HH:mm"),
                Foreground = (Brush)FindResource("TextMutedBrush"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 8, 0)
            };
            var delBtn = new Button
            {
                Content = "✕",
                Width = 24, Height = 24,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Cursor = Cursors.Hand,
                FontSize = 10
            };
            Grid.SetColumn(titleBlock, 0);
            Grid.SetColumn(timeBlock, 1);
            Grid.SetColumn(delBtn, 2);
            grid.Children.Add(titleBlock);
            grid.Children.Add(timeBlock);
            grid.Children.Add(delBtn);
            row.Child = grid;
            var localItem = item;
            delBtn.Click += (_, _) =>
            {
                _settings.History?.Remove(localItem);
                SaveSettings();
                UpdateHistoryPage(HistorySearchBox?.Text ?? "");
            };
            row.MouseLeftButtonDown += (_, _) => NavigateTo(localItem.Url);
            row.MouseEnter += (_, _) => row.Background = (Brush)FindResource("BgHoverBrush");
            row.MouseLeave += (_, _) => row.Background = Brushes.Transparent;
            HistoryListPanel.Children.Add(row);
        }
    }
    private void HistorySearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateHistoryPage(HistorySearchBox.Text);
    }
    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        _settings.History?.Clear();
        try {
            var histPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovaBrowser", "history.json");
            if (System.IO.File.Exists(histPath)) System.IO.File.Delete(histPath);
        } catch { }
        SaveSettings();
        UpdateHistoryPage();
        StatusText.Text = "История очищена";
    }
    public void ShowHistoryTab()
    {
        var existing = _tabs.FirstOrDefault(t => t.Url == "nova://history");
        if (existing != null)
        {
            SwitchToTab(existing);
            return;
        }
        var tab = new BrowserTab { Title = "История", Url = "nova://history" };
        _tabs.Add(tab);
        SwitchToTab(tab);
    }
    public void OpenHistoryTab() => ShowHistoryTab();
    public void ShowBookmarksTab()
    {
        var existing = _tabs.FirstOrDefault(t => t.Url == "nova://bookmarks");
        if (existing != null)
        {
            SwitchToTab(existing);
            return;
        }
        var tab = new BrowserTab { Title = "Закладки", Url = "nova://bookmarks" };
        _tabs.Add(tab);
        SwitchToTab(tab);
    }
    private void ShowBookmarksPage() => ShowBookmarksTab();
    private void CloseHistory_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab != null && _activeTab.Url == "nova://history")
            CloseTab(_activeTab);
        else
            HideAllPages();
    }
    private void CloseBookmarks_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab != null && _activeTab.Url == "nova://bookmarks")
            CloseTab(_activeTab);
        else
            HideAllPages();
    }
    private void MenuHistory_Click(object sender, RoutedEventArgs e)
    {
        if (MenuPopup != null) MenuPopup.IsOpen = false;
        ShowHistoryTab();
    }
    private void MenuBookmarks_Click(object sender, RoutedEventArgs e)
    {
        if (MenuPopup != null) MenuPopup.IsOpen = false;
        ShowBookmarksTab();
    }
    private void UpdateBookmarksPage()
    {
        BookmarksListPanel.Children.Clear();
        if (_settings.Bookmarks?.Count == 0 || _settings.Bookmarks == null)
        {
            BookmarksListPanel.Children.Add(new TextBlock
            {
                Text = "Нет закладок",
                Foreground = (Brush)FindResource("TextMutedBrush"),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0)
            });
            return;
        }
        foreach (var bm in (_settings.Bookmarks ?? new()).ToList())
        {
            var row = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Cursor = Cursors.Hand
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var info = new StackPanel();
            var bmTitleBlock = new TextBlock
            {
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            bmTitleBlock.Inlines.Add(new Run("\uE728 ") { FontFamily = new FontFamily("Segoe MDL2 Assets") });
            bmTitleBlock.Inlines.Add(new Run(bm.Title));
            info.Children.Add(bmTitleBlock);
            info.Children.Add(new TextBlock
            {
                Text = bm.Url,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            var delBtn = new Button
            {
                Content = "Удалить",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("AccentRedBrush"),
                Cursor = Cursors.Hand,
                FontSize = 11,
                Padding = new Thickness(8, 4, 8, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(info, 0);
            Grid.SetColumn(delBtn, 1);
            grid.Children.Add(info);
            grid.Children.Add(delBtn);
            row.Child = grid;
            var localBm = bm;
            delBtn.Click += (_, _) =>
            {
                _settings.Bookmarks?.Remove(localBm);
                SaveSettings();
                UpdateBookmarksBar();
                UpdateBookmarksPage();
            };
            row.MouseLeftButtonDown += (_, e) =>
            {
                if (FindVisualParent<Button>(e.OriginalSource as DependencyObject) != null) return;
                NavigateTo(localBm.Url);
            };
            row.MouseEnter += (_, _) => row.Background = (Brush)FindResource("BgHoverBrush");
            row.MouseLeave += (_, _) => row.Background = Brushes.Transparent;
            BookmarksListPanel.Children.Add(row);
        }
    }

    private void UpdateMuteButton()
    {
        if (MuteButton == null) return;
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) { MuteButton.Visibility = Visibility.Collapsed; return; }
        MuteButton.Visibility = Visibility.Visible;
        MuteButton.FontFamily = (FontFamily)(TryFindResource("IconFont") ?? new FontFamily("Segoe MDL2 Assets"));
        MuteButton.Content = wv.IsMuted ? "\uE74F" : "\uE767";
        MuteButton.Foreground = wv.IsMuted ? (Brush)FindResource("AccentOrangeBrush") : (Brush)FindResource("TextSecondaryBrush");
    }
    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        wv.IsMuted = !wv.IsMuted;
        UpdateMuteButton();
        StatusText.Text = wv.IsMuted ? "Звук выключен" : "Звук включён";
    }
    private void ToggleFind()
    {
        if (FindPopup != null)
        {
            FindPopup.IsOpen = !FindPopup.IsOpen;
            if (FindPopup.IsOpen)
            {
                FindBox.Focus();
                FindBox.SelectAll();
            }
            return;
        }
        if (FindBar.Visibility == Visibility.Visible)
        {
            FindClose_Click(this, new RoutedEventArgs());
        }
        else
        {
            FindBar.Visibility = Visibility.Visible;
            FindBox.Focus();
            FindBox.SelectAll();
            AnimatePop(FindBar);
        }
    }
    private async void FindBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null || string.IsNullOrEmpty(FindBox.Text)) { FindCount.Text = ""; return; }
        try
        {
            var escaped = FindBox.Text.Replace("\\", "\\\\").Replace("'", "\\'");
            var result = await wv.ExecuteScriptAsync(
                $"(function(){{ var r=0,s=window.getSelection(); try{{ r=document.designMode==='on'?0:0; }}catch(e){{}} " +
                $"window.find('{escaped}'); return document.querySelectorAll ? " +
                $"'searching' : '0'; }})()");
            FindCount.Text = "";
        }
        catch { FindCount.Text = ""; }
    }
    private void FindBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  { FindNext_Click(this, new RoutedEventArgs()); e.Handled = true; }
        if (e.Key == Key.Escape) { FindClose_Click(this, new RoutedEventArgs()); e.Handled = true; }
    }
    private async void FindNext_Click(object sender, RoutedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null || string.IsNullOrEmpty(FindBox.Text)) return;
        var escaped = FindBox.Text.Replace("\\", "\\\\").Replace("'", "\\'");
        await wv.ExecuteScriptAsync($"window.find('{escaped}', false, false, true)");
    }
    private async void FindPrev_Click(object sender, RoutedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null || string.IsNullOrEmpty(FindBox.Text)) return;
        var escaped = FindBox.Text.Replace("\\", "\\\\").Replace("'", "\\'");
        await wv.ExecuteScriptAsync($"window.find('{escaped}', false, true, true)");
    }
    private void FindClose_Click(object sender, RoutedEventArgs e)
    {
        FindBar.Visibility = Visibility.Collapsed;
        FindBox.Text = "";
        FindCount.Text = "";
        _activeTab?.WebViewControl?.Focus();
    }
    private async void PrintPage()
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) { StatusText.Text = "Нет страницы для печати"; return; }
        try
        {
            await wv.ExecuteScriptAsync("window.print()");
            StatusText.Text = "Диалог печати открыт";
        }
        catch { StatusText.Text = "Ошибка печати"; }
    }
    private async void ViewSource()
    {
        if (_activeTab == null) return;
        var curTab = _activeTab;
        string targetUrl = curTab.Url;
        string targetTitle = curTab.Title;
        string html = "";
        if (curTab.WebViewControl?.CoreWebView2 != null)
        {
            try
            {
                var rawJson = await curTab.WebViewControl.CoreWebView2.ExecuteScriptAsync("document.documentElement ? document.documentElement.outerHTML : ''");
                if (!string.IsNullOrEmpty(rawJson) && rawJson != "null")
                {
                    html = System.Text.Json.JsonSerializer.Deserialize<string>(rawJson) ?? "";
                }
            }
            catch { }
        }
        if (string.IsNullOrWhiteSpace(html) && !string.IsNullOrWhiteSpace(targetUrl) && targetUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                html = await client.GetStringAsync(targetUrl);
            }
            catch { }
        }
        if (string.IsNullOrWhiteSpace(html))
        {
            html = "<p>Исходный код страницы пуст или недоступен</p>";
        }
        var viewerHtml = BuildSourceViewerHtml(targetUrl, targetTitle, html);
        var sourceTab = new BrowserTab
        {
            Title = "Исходный код: " + (string.IsNullOrEmpty(targetTitle) ? targetUrl : targetTitle),
            Url = "nova://source"
        };
        _tabs.Add(sourceTab);
        SwitchToTab(sourceTab);
        var wv = await CreateWebViewForTab(sourceTab);
        if (wv?.CoreWebView2 != null)
        {
            wv.CoreWebView2.NavigateToString(viewerHtml);
        }
    }

    private static string BuildSourceViewerHtml(string url, string title, string sourceCode)
    {
        var encoded = System.Net.WebUtility.HtmlEncode(sourceCode);
        var lines = encoded.Split('\n');
        var sb = new System.Text.StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Исходный код: ")
          .Append(System.Net.WebUtility.HtmlEncode(title))
          .Append("</title><style>")
          .Append(":root{--bg:#0A0C14;--surface:#111420;--border:rgba(255,255,255,0.08);--accent:#BEF47B;--text:#F1F5F9;--text-sec:#94A3B8;}")
          .Append("*{box-sizing:border-box;margin:0;padding:0;}")
          .Append("body{background:var(--bg);color:var(--text);font-family:'Cascadia Code',Consolas,monospace;font-size:13px;line-height:1.6;}")
          .Append(".topbar{position:sticky;top:0;z-index:100;display:flex;align-items:center;justify-content:space-between;padding:10px 18px;background:rgba(17,20,32,0.95);backdrop-filter:blur(12px);border-bottom:1px solid var(--border);}")
          .Append(".url-pill{font-size:12px;color:var(--text-sec);max-width:60%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;background:rgba(255,255,255,0.05);padding:4px 10px;border-radius:6px;}")
          .Append(".actions{display:flex;gap:8px;align-items:center;}")
          .Append(".btn{background:rgba(255,255,255,0.08);color:var(--text);border:1px solid var(--border);padding:5px 12px;border-radius:6px;font-size:12px;cursor:pointer;transition:all 0.2s;font-family:inherit;}")
          .Append(".btn:hover{background:var(--accent);color:#07090D;border-color:var(--accent);}")
          .Append(".code-table{width:100%;border-collapse:collapse;table-layout:fixed;}")
          .Append(".code-table td{padding:0;vertical-align:top;}")
          .Append(".line-nums{width:55px;text-align:right;padding-right:14px;color:rgba(255,255,255,0.25);user-select:none;border-right:1px solid var(--border);background:rgba(0,0,0,0.15);}")
          .Append(".code-line{padding-left:14px;white-space:pre-wrap;word-break:break-all;color:#E2E8F0;}")
          .Append(".code-table tr:hover{background:rgba(190,244,123,0.04);}")
          .Append("</style></head><body>")
          .Append("<div class=\"topbar\"><div class=\"url-pill\">").Append(System.Net.WebUtility.HtmlEncode(url))
          .Append("</div><div class=\"actions\"><span style=\"font-size:12px;color:var(--text-sec);margin-right:8px;\">")
          .Append(lines.Length).Append(" строк</span>")
          .Append("<button class=\"btn\" onclick=\"navigator.clipboard.writeText(document.getElementById('rawCode').textContent).then(()=>{this.textContent='Скопировано!';setTimeout(()=>this.textContent='Копировать код',1500);})\">Копировать код</button>")
          .Append("</div></div><pre id=\"rawCode\" style=\"display:none;\">").Append(encoded).Append("</pre>")
          .Append("<table class=\"code-table\">");
        for (int i = 0; i < lines.Length; i++)
        {
            sb.Append("<tr><td class=\"line-nums\">").Append(i + 1).Append("</td><td class=\"code-line\">")
              .Append(string.IsNullOrEmpty(lines[i]) ? "&nbsp;" : lines[i])
              .Append("</td></tr>");
        }
        sb.Append("</table></body></html>");
        return sb.ToString();
    }
    private async void TakeScreenshot()
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) { StatusText.Text = "Нет страницы для скриншота"; return; }
        try
        {
            var folder = string.IsNullOrEmpty(_settings.DownloadFolder)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : _settings.DownloadFolder;
            var path = System.IO.Path.Combine(folder, $"Nova_Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            using var stream = System.IO.File.Create(path);
            await wv.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            StatusText.Text = $"Скриншот: {System.IO.Path.GetFileName(path)}";
            ShowDownloadToast(System.IO.Path.GetFileName(path));
        }
        catch (Exception ex) { StatusText.Text = $"Ошибка скриншота: {ex.Message}"; }
    }
    private void CopyUrl()
    {
        var url = _activeTab?.Url;
        if (string.IsNullOrEmpty(url)) return;
        Clipboard.SetText(url);
        AnimateStatusText("URL скопирован");
    }
    private async void CreatePrivateTab()
    {
        try
        {
            var tab = new BrowserTab
            {
                Title = "Приватный режим",
                Url = NovaPrivateUrl,
                IsPrivate = true
            };
            _tabs.Add(tab);
            _activeTab = tab;
            HideAllWebViews();

            var webView = new WebView2
            {
                Visibility = Visibility.Visible,
                Width = 0,
                Height = 0
            };
            WebViewContainer.Children.Add(webView);

            if (_webViewEnv == null && App.EnvTask != null)
                _webViewEnv = await App.EnvTask;

            if (_webViewEnv != null)
            {
                try
                {
                    var options = _webViewEnv.CreateCoreWebView2ControllerOptions();
                    options.IsInPrivateModeEnabled = true;
                    await webView.EnsureCoreWebView2Async(_webViewEnv, options);
                }
                catch
                {
                    await webView.EnsureCoreWebView2Async(_webViewEnv);
                }
            }
            else
            {
                await webView.EnsureCoreWebView2Async();
            }

            if (webView.CoreWebView2 != null)
            {
                var s = webView.CoreWebView2.Settings;
                s.IsScriptEnabled = _settings.JavaScriptEnabled;
                s.IsPasswordAutosaveEnabled = false;
                s.IsGeneralAutofillEnabled = false;
                s.AreDefaultContextMenusEnabled = true;
                s.IsBuiltInErrorPageEnabled = true;
                s.AreBrowserAcceleratorKeysEnabled = true;

                MapNovaHost(webView.CoreWebView2);
                webView.CoreWebView2.WebMessageReceived -= OnNovaWebMessage;
                webView.CoreWebView2.WebMessageReceived += OnNovaWebMessage;
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(GetCachedInjectionScript());
                WireAdBlocker(webView.CoreWebView2, tab);
                webView.CoreWebView2.BasicAuthenticationRequested += (_, authArgs) =>
                {
                    if (_settings.ProxyEnabled && !string.IsNullOrEmpty(_settings.ProxyUsername))
                    {
                        authArgs.Response.UserName = _settings.ProxyUsername;
                        authArgs.Response.Password = _settings.ProxyPassword ?? "";
                    }
                };

                webView.CoreWebView2.DocumentTitleChanged += (_, _) =>
                {
                    if (tab.IsPrivate && (string.IsNullOrEmpty(tab.Url) || IsNovaUrl(tab.Url) || tab.Url.Contains("private.html", StringComparison.OrdinalIgnoreCase)))
                        tab.Title = "Приватный режим";
                    else
                        tab.Title = webView.CoreWebView2.DocumentTitle;
                    UpdateTabsUI();
                };
                webView.CoreWebView2.IsDocumentPlayingAudioChanged += (_, _) =>
                {
                    tab.IsPlayingAudio = webView.CoreWebView2.IsDocumentPlayingAudio;
                    UpdateTabsUI();
                };
                webView.CoreWebView2.IsMutedChanged += (_, _) =>
                {
                    tab.IsMuted = webView.CoreWebView2.IsMuted;
                    UpdateTabsUI();
                };
                webView.CoreWebView2.NavigationStarting += (_, args) =>
                {
                    tab.IsLoading = true;
                    if (tab == _activeTab) AnimateLoadingBarIn();
                };
                webView.CoreWebView2.NavigationCompleted += (_, args) =>
                {
                    tab.IsLoading = false;
                    tab.Url = webView.Source?.ToString() ?? "";
                    tab.IsSecure = SecurityHelper.IsSecureConnection(tab.Url);

                    if (tab == _activeTab)
                    {
                        AnimateLoadingBarOut();
                        BackButton.IsEnabled = webView.CanGoBack;
                        ForwardButton.IsEnabled = webView.CanGoForward;
                        AddressBox.Text = IsNovaUrl(tab.Url) ? "" : tab.Url;
                        UpdateSecurityIndicator();
                    }
                };
                webView.CoreWebView2.NewWindowRequested += (_, args) =>
                {
                    args.Handled = true;
                    var target = args.Uri;
                    Dispatcher.Invoke(() =>
                    {
                        if (tab.IsPrivate)
                            NavigateTo(target);
                        else
                            CreateNewTab(target);
                    });
                };
            }

            tab.WebViewControl = webView;
            HideAllPages();
            webView.Width = double.NaN;
            webView.Height = double.NaN;
            _isOnHomePage = false;
            AddressBox.Text = "";
            ApplyIncognitoTheme(true);
            EnsurePrivateHtmlOnDisk();
            UpdateTabsUI();
            webView.CoreWebView2?.NavigateToString(PrivateHomeHtml.GetContent());
        }
        catch
        {
            CreateNewTab();
        }
    }

    private void ApplyIncognitoTheme(bool isIncognito)
    {
        try
        {
            if (isIncognito)
            {
                Application.Current.Resources["TitleBarBrush"] = new SolidColorBrush(Color.FromRgb(24, 28, 36));
                Application.Current.Resources["BgDarkBrush"] = new SolidColorBrush(Color.FromRgb(16, 18, 22));
                Application.Current.Resources["BgCardBrush"] = new SolidColorBrush(Color.FromRgb(28, 32, 42));
                Application.Current.Resources["BgSurfaceBrush"] = new SolidColorBrush(Color.FromRgb(36, 42, 54));
                Application.Current.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(209, 213, 219));
                Application.Current.Resources["GlowLine"] = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                if (PrivateIndicator != null) PrivateIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                var curAccent = _settings?.AccentColor ?? "#BEF47B";
                ApplyVisualThemeToBrowser(_settings?.Theme ?? "Dark", curAccent, _settings?.HtmlBackgroundColor);
                if (PrivateIndicator != null) PrivateIndicator.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }
    private DispatcherTimer? _downloadTimer;
    private void ShowDownloadToast(string filename)
    {
        if (DownloadBar == null) return;
        DownloadText.Text = $" {filename}";
        DownloadBar.Visibility = Visibility.Visible;
        AnimateDownloadToast(DownloadBar);
        _downloadTimer?.Stop();
        _downloadTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _downloadTimer.Tick += (_, _) => { DownloadBar.Visibility = Visibility.Collapsed; _downloadTimer.Stop(); };
        _downloadTimer.Start();
    }
    private void DownloadBarClose_Click(object sender, RoutedEventArgs e)
    {
        DownloadBar.Visibility = Visibility.Collapsed;
        _downloadTimer?.Stop();
    }
    private void MenuFind_Click(object sender, RoutedEventArgs e)        { MenuPopup.IsOpen = false; ToggleFind(); }
    private void MenuPrint_Click(object sender, RoutedEventArgs e)       { MenuPopup.IsOpen = false; PrintPage(); }
    private void MenuViewSource_Click(object sender, RoutedEventArgs e)  { MenuPopup.IsOpen = false; ViewSource(); }
    private void MenuScreenshot_Click(object sender, RoutedEventArgs e)  { MenuPopup.IsOpen = false; TakeScreenshot(); }
    private void MenuPrivateTab_Click(object sender, RoutedEventArgs e)  { MenuPopup.IsOpen = false; CreatePrivateTab(); }
    private DispatcherTimer? _clockTimer;
    private void StartClock()
    {
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
    }
    private void UpdateClock()
    {
    }
    private void ApplyStatusBarVisibility()
    {
        if (StatusBarGrid != null)
            StatusBarGrid.Visibility = _settings.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed;
        if (MemoryUsage != null)
            MemoryUsage.Visibility = _settings.ShowMemoryUsage ? Visibility.Visible : Visibility.Collapsed;
    }
    private void ApplyCompactMode()
    {
        var margin = _settings.CompactMode ? new Thickness(4, 2, 4, 2) : new Thickness(8, 5, 8, 5);
        try { Application.Current.Resources["NavBtnPadding"] = margin; } catch { }
    }
    private void ApplyWindowOpacity()
    {
        this.Opacity = _settings.WindowOpacity;
    }
    private void ApplyTabRadius()
    {
        UpdateTabsUI();
    }
    private void ApplyClockVisibility()
    {
    }
    private void TabRadius_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_settingsLoading) return;
        if (TabRadiusCombo.SelectedItem is ComboBoxItem ci && int.TryParse(ci.Tag?.ToString(), out int r))
        {
            _settings.TabRadius = r;
            SaveSettings();
            UpdateTabsUI();
        }
    }
    private void WindowOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settingsLoading) return;
        _settings.WindowOpacity = e.NewValue / 100.0;
        this.Opacity = _settings.WindowOpacity;
        if (WindowOpacityLabel != null) WindowOpacityLabel.Text = $"{(int)e.NewValue}%";
        SaveSettings();
    }
    private void WallpaperFit_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_settingsLoading) return;
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem ci)
        {
            _settings.WallpaperFit = ci.Tag?.ToString() ?? "UniformToFill";
            SaveSettings();
            if (Enum.TryParse<Stretch>(_settings.WallpaperFit, out var stretch))
                WallpaperImage.Stretch = stretch;
        }
    }
    private void NewTabBg_Changed(object sender, TextChangedEventArgs e)
    {
        if (_settingsLoading) return;
        var hex = NewTabBgBox.Text.Trim();
        _settings.NewTabBackground = hex;
        SaveSettings();
        try
        {
            if (!string.IsNullOrEmpty(hex))
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                NewTabBgPreview.Background = new SolidColorBrush(color);
                WallpaperLayer.Background = new SolidColorBrush(color);
            }
            else
            {
                NewTabBgPreview.Background = (Brush)FindResource("BgDarkBrush");
                WallpaperLayer.Background = (Brush)FindResource("BgDarkBrush");
            }
        }
        catch { }
    }
    private void CustomCss_Changed(object sender, TextChangedEventArgs e)
    {
        if (_settingsLoading) return;
        _settings.CustomCss = CustomCssBox.Text;
        SaveSettings();
        AnimateStatusText("CSS сохранён");
    }
    private void CustomBlocked_Changed(object sender, TextChangedEventArgs e)
    {
        if (_settingsLoading) return;
        _settings.CustomBlockedDomains = CustomBlockedBox.Text;
        SaveSettings();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Tab && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0)
        {
            CycleTab((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == 0 ? 1 : -1);
            e.Handled = true;
            return;
        }
        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 &&
            e.Key >= Key.D1 && e.Key <= Key.D9)
        {
            int n = e.Key - Key.D1;
            int idx = n == 8 ? _tabs.Count - 1 : Math.Min(n, _tabs.Count - 1);
            if (idx >= 0) SwitchToTab(_tabs[idx]);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.F11) { ToggleFullscreen(); e.Handled = true; return; }
        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.OemPlus)
        { ZoomActiveTab(+10); e.Handled = true; return; }
        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.OemMinus)
        { ZoomActiveTab(-10); e.Handled = true; return; }
        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.D0)
        { ZoomActiveTab(0); e.Handled = true; return; }
        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) != 0 && e.Key == Key.Left)
        { Back_Click(this, new RoutedEventArgs()); e.Handled = true; return; }
        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) != 0 && e.Key == Key.Right)
        { Forward_Click(this, new RoutedEventArgs()); e.Handled = true; return; }
        if ((e.KeyboardDevice.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) ==
            (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.N)
        { CreatePrivateTab(); e.Handled = true; return; }
        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.M)
        { Mute_Click(this, new RoutedEventArgs()); e.Handled = true; return; }
        if (e.Key == Key.F12) { ToggleDevTools(); e.Handled = true; return; }
        if (e.Key == Key.Escape && _isFullscreen) { ToggleFullscreen(); e.Handled = true; return; }
        if (e.Key == Key.Escape)
        {
            if (TabSearchOverlay.Visibility == Visibility.Visible)
            { TabSearchOverlay.Visibility = Visibility.Collapsed; e.Handled = true; return; }
            if (CommandPalette.Visibility == Visibility.Visible)
            { CommandPalette.Visibility = Visibility.Collapsed; e.Handled = true; return; }
        }
    }
    private void CycleTab(int direction)
    {
        if (_tabs.Count < 2 || _activeTab == null) return;
        int idx = (_tabs.IndexOf(_activeTab) + direction + _tabs.Count) % _tabs.Count;
        SwitchToTab(_tabs[idx]);
    }
    private bool _isFullscreen;
    private WindowStyle _preFullscreenStyle;
    private WindowState _preFullscreenState;
    private Thickness   _preFullscreenMargin;
    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _preFullscreenStyle  = WindowStyle;
            _preFullscreenState  = WindowState;
            _preFullscreenMargin = MainBorder != null ? MainBorder.Margin : new Thickness(6);
            if (TitleBarGrid  != null) TitleBarGrid.Visibility  = Visibility.Collapsed;
            if (NavBarGrid    != null) NavBarGrid.Visibility    = Visibility.Collapsed;
            if (BookmarksBar  != null) BookmarksBar.Visibility  = Visibility.Collapsed;
            if (StatusBarGrid != null) StatusBarGrid.Visibility = Visibility.Collapsed;
            if (MainBorder != null)
            {
                MainBorder.BorderThickness = new Thickness(0);
                MainBorder.CornerRadius    = new CornerRadius(0);
                MainBorder.Margin          = new Thickness(0);
                try { MainBorder.Effect = null; } catch { }
            }
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Normal;
            Topmost = true;
            Left   = 0; Top    = 0;
            Width  = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            _isFullscreen = true;
        }
        else
        {
            Topmost = false;
            var radius = _settings.Theme == "Win7" ? 4 : 12;
            if (MainBorder != null)
            {
                MainBorder.BorderThickness = new Thickness(_isMaximized ? 0 : 1);
                MainBorder.CornerRadius    = new CornerRadius(_isMaximized ? 0 : radius);
                MainBorder.Margin          = _isMaximized ? new Thickness(0) : new Thickness(6);
                if (!_isMaximized)
                {
                    try { MainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                          { BlurRadius = 24, ShadowDepth = 0, Opacity = 0.6,
                            Color = System.Windows.Media.Colors.Black }; } catch { }
                }
            }
            if (TitleBarGrid != null) TitleBarGrid.Visibility = Visibility.Visible;
            if (NavBarGrid   != null) NavBarGrid.Visibility   = Visibility.Visible;
            ApplyBookmarksBarVisibility();
            ApplyStatusBarVisibility();
            WindowStyle = _preFullscreenStyle;
            WindowState = _preFullscreenState;
            _isFullscreen = false;
        }
    }
    private void MenuFullscreen_Click(object sender, RoutedEventArgs e)
    { MenuPopup.IsOpen = false; ToggleFullscreen(); }
    private void ZoomActiveTab(int delta)
    {
        if (_activeTab?.WebViewControl == null) return;
        double current = (int)Math.Round(_activeTab.WebViewControl.ZoomFactor * 100);
        double next = delta == 0 ? _settings.DefaultZoom : Math.Clamp(current + delta, 25, 500);
        _activeTab.WebViewControl.ZoomFactor = next / 100.0;
        _activeTab.ZoomLevel = next;
        UpdateZoomLabel();
    }
    private void UpdateZoomLabel()
    {
        if (_activeTab?.WebViewControl == null || ZoomLabel == null) return;
        var pct = (int)Math.Round(_activeTab.WebViewControl.ZoomFactor * 100);
        ZoomLabel.Text = $"{pct}%";
    }
    private void ZoomIn_Click(object sender, RoutedEventArgs e)   => ZoomActiveTab(+10);
    private void ZoomOut_Click(object sender, RoutedEventArgs e)  => ZoomActiveTab(-10);
    private void ZoomReset_Click(object sender, RoutedEventArgs e)=> ZoomActiveTab(0);
    private void MenuZoom_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        UpdateZoomLabel();
        ZoomPopup.IsOpen = true;
    }
    private bool _suppressSuggest;
    private DispatcherTimer? _suggestDebounceTimer;
    private void AddressBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (AddressBox.IsKeyboardFocused) AnimateAddressBarKeystroke();
        if (_suppressSuggest || !AddressBox.IsKeyboardFocused) { SuggestPopup.IsOpen = false; return; }
        var q = AddressBox.Text.Trim();
        if (q.Length < 2) { SuggestPopup.IsOpen = false; return; }
        _suggestDebounceTimer?.Stop();
        _suggestDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _suggestDebounceTimer.Tick += (_, _) =>
        {
            _suggestDebounceTimer?.Stop();
            PerformSuggest(q);
        };
        _suggestDebounceTimer.Start();
    }
    private void PerformSuggest(string q)
    {
        if (_suppressSuggest || !AddressBox.IsKeyboardFocused || AddressBox.Text.Trim() != q) return;
        var results = new List<SuggestItem>();
        if (_settings.History != null)
            results.AddRange(
                _settings.History
                    .Where(h => h.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                h.Url.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .Take(6)
                    .Select(h => new SuggestItem { Title = h.Title, Url = h.Url, Icon = "[H]" }));
        if (_settings.Bookmarks?.Count > 0)
            results.AddRange(
                _settings.Bookmarks
                    .Where(b => b.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                b.Url.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .Take(3)
                    .Select(b => new SuggestItem { Title = b.Title, Url = b.Url, Icon = "" }));
        results.Add(new SuggestItem
        {
            Title = $"Искать: {q}",
            Url   = NovaHomeUrl + "#/search?q=" + Uri.EscapeDataString(q),
            Icon  = "[?]"
        });
        var suggestItems = results.DistinctBy(r => r.Url).Take(8).ToList();
        SuggestList.ItemsSource = suggestItems;
        SuggestPopup.IsOpen = suggestItems.Count > 0;
    }
    private void Suggest_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void Suggest_Click(object sender, MouseButtonEventArgs e)
    {
        if (SuggestList.SelectedItem is SuggestItem item)
        {
            _suppressSuggest = true;
            SuggestPopup.IsOpen = false;
            _suppressSuggest = false;
            if (item.Url.Contains("#/search?q="))
            {
                var q = AddressBox.Text.Trim();
                ExecuteNovaHtmlSearch(q);
            }
            else
            {
                AddressBox.Text = item.Url;
                NavigateTo(item.Url);
            }
        }
    }
    private string GetCachedInjectionScript()
    {
        var accountsJson = _settings.UniversalPasswordAutofill && _settings.SavedPasswords != null && _settings.SavedPasswords.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(_settings.SavedPasswords.Select(p => new { login = p.Login, password = p.Password, site = p.Site }))
            : "[]";
        var key = $"{_settings.BlockCookieBanners}{_settings.BlockTrackers}{_settings.BlockAds}" +
                  $"{_settings.BlockFingerprinting}{_settings.BlockWebRTC}{_settings.BlockCanvas}" +
                  $"{_settings.BlockAutoplay}{_settings.BlockCryptomining}{_settings.SendDoNotTrack}" +
                  $"{_settings.LazyLoadImages}{_settings.CustomCss?.GetHashCode()}{accountsJson.GetHashCode()}{_settings.AccentColor}" +
                  $"{_settings.EnableSiteStealthIsolation}{_settings.StealthIsolationDomains}{_settings.StealthBlockGeo}" +
                  $"{_settings.StealthBlockFingerprint}{_settings.StealthBlockWebRTC}{_settings.StealthBlockSensorsAndHardware}";
        if (_cachedInjectionScript != null && _lastInjectionKey == key)
            return _cachedInjectionScript;
        _cachedInjectionScript = SecurityHelper.GetInjectionScript(
            blockCookies:          _settings.BlockCookieBanners,
            blockTrackers:         _settings.BlockTrackers,
            blockAds:              _settings.BlockAds,
            blockFingerprint:      _settings.BlockFingerprinting,
            blockWebRTC:           _settings.BlockWebRTC,
            blockCanvas:           _settings.BlockCanvas,
            blockAutoplay:         _settings.BlockAutoplay,
            blockCryptomining:     _settings.BlockCryptomining,
            sendDNT:               _settings.SendDoNotTrack,
            lazyLoadImages:        _settings.LazyLoadImages,
            customCss:             _settings.CustomCss ?? string.Empty,
            savedAccountsJson:     accountsJson,
            accentColor:           _settings.AccentColor ?? "#BEF47B",
            enableStealth:         _settings.EnableSiteStealthIsolation,
            stealthDomains:        _settings.StealthIsolationDomains ?? "max.gosuslugi.ru, gosuslugi.ru",
            stealthBlockGeo:       _settings.StealthBlockGeo,
            stealthBlockFp:        _settings.StealthBlockFingerprint,
            stealthBlockWebRtc:    _settings.StealthBlockWebRTC,
            stealthBlockSensors:   _settings.StealthBlockSensorsAndHardware);
        _lastInjectionKey = key;
        return _cachedInjectionScript;
    }
    private void RebuildCustomDomainCache()
    {
        var raw = _settings.CustomBlockedDomains ?? "";
        if (_cachedCustomDomains != null && _lastCustomDomainsRaw == raw) return;
        _lastCustomDomainsRaw = raw;
        _cachedCustomDomains = new HashSet<string>(
            raw.Split(new[] { '\n', '\r', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(d => d.Trim().ToLowerInvariant())
               .Where(d => d.Length > 0),
            StringComparer.OrdinalIgnoreCase);
    }
    private void WireAdBlocker(
        Microsoft.Web.WebView2.Core.CoreWebView2 core,
        BrowserTab tab)
    {
        core.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
        RebuildCustomDomainCache();
        core.WebResourceRequested += (_, args) =>
        {
            var uri = args.Request.Uri;
            if (string.IsNullOrEmpty(uri)) return;
            var host = GetHostFast(uri);
            if (host == null) return;
            if (host.EndsWith("googlevideo.com", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("ytimg.com", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("vimeocdn.com", StringComparison.OrdinalIgnoreCase) ||
                SecurityHelper.IsWhitelisted(host)) return;

            if (_settings.EnableSiteStealthIsolation && SecurityHelper.IsStealthDomain(uri, _settings.StealthIsolationDomains))
            {
                if (_settings.StealthStripTrackingHeaders)
                {
                    if (args.Request.Headers.Contains("Referer"))
                        args.Request.Headers.RemoveHeader("Referer");
                    args.Request.Headers.SetHeader("DNT", "1");
                    args.Request.Headers.SetHeader("Sec-GPC", "1");
                }
            }

            bool shouldBlock = false;
            if (_settings.AntiLeakProtection && SecurityHelper.IsExfiltrationUrl(uri))
            {
                shouldBlock = true;
            }

            if (!shouldBlock && (_settings.BlockAds || _settings.BlockTrackers))
                shouldBlock = SecurityHelper.IsBlockedUrl(uri, host);

            if (!shouldBlock && _cachedCustomDomains != null)
            {
                if (_cachedCustomDomains.Contains(host)) shouldBlock = true;
                else
                {
                    var dot = host.IndexOf('.');
                    while (!shouldBlock && dot >= 0)
                    {
                        var parent = host[(dot + 1)..];
                        if (_cachedCustomDomains.Contains(parent)) shouldBlock = true;
                        dot = parent.IndexOf('.');
                        if (dot < 0) break;
                        host = parent;
                    }
                }
            }
            if (shouldBlock)
            {
                try
                {
                    var empty = new MemoryStream();
                    args.Response = core.Environment.CreateWebResourceResponse(
                        empty, 204, "No Content", "Content-Type: text/plain\r\nAccess-Control-Allow-Origin: *\r\nCache-Control: no-store");
                }
                catch { }
            }
        };
    }
    private static string? GetHostFast(string url)
    {
        try
        {
            var s = url.IndexOf("://", StringComparison.Ordinal);
            if (s < 0) return null;
            s += 3;
            var e = url.IndexOfAny(new[] { '/', ':', '?' }, s);
            var host = e < 0 ? url[s..] : url[s..e];
            return host.ToLowerInvariant();
        }
        catch { return null; }
    }
    internal void TrimBackgroundRam()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Optimized, false);
            try { EmptyWorkingSet(Process.GetCurrentProcess().Handle); } catch { }

            if (_webViewEnv != null)
            {
                try
                {
                    var procInfos = _webViewEnv.GetProcessInfos();
                    if (procInfos != null)
                    {
                        foreach (var info in procInfos)
                        {
                            if (info != null && info.ProcessId > 0)
                            {
                                try
                                {
                                    using var p = Process.GetProcessById(info.ProcessId);
                                    if (!p.HasExited)
                                        EmptyWorkingSet(p.Handle);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }

            foreach (var tab in _tabs)
            {
                if (tab != _activeTab && !tab.IsPlayingAudio && tab.WebViewControl?.CoreWebView2 != null)
                {
                    var url = tab.Url ?? "";
                    if (!url.Contains("spotify", StringComparison.OrdinalIgnoreCase) &&
                        !url.Contains("youtube", StringComparison.OrdinalIgnoreCase) &&
                        !url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            _ = tab.WebViewControl.CoreWebView2.CallDevToolsProtocolMethodAsync("Memory.forciblyPurgeCache", "{}");
                            _ = tab.WebViewControl.CoreWebView2.TrySuspendAsync();
                        }
                        catch { }
                    }
                }
            }
        }
        catch { }
    }
    internal void TrimAllRamAggressive() => TrimBackgroundRam();
    private static string SanitizeFolderName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (Array.IndexOf(invalid, c) < 0 && c != '/' && c != '\\')
                sb.Append(c);
            else
                sb.Append('_');
        }
        return sb.ToString().Trim();
    }
    private void WireDownloads(Microsoft.Web.WebView2.Core.CoreWebView2 core)
    {
        core.DownloadStarting += (_, args) =>
        {
            var dl       = args.DownloadOperation;
            var filename = System.IO.Path.GetFileName(dl.ResultFilePath);
            if (string.IsNullOrWhiteSpace(filename)) filename = "file";
            var folder = string.IsNullOrWhiteSpace(_settings.DownloadFolder)
                ? System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                : _settings.DownloadFolder;
            try   { System.IO.Directory.CreateDirectory(folder); }
            catch { folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
            string dest = BuildDestPath(folder, filename);
            if (_settings.AskDownloadLocation)
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title            = "Сохранить файл",
                    FileName         = System.IO.Path.GetFileName(dest),
                    InitialDirectory = folder,
                };
                if (dlg.ShowDialog() == true)
                    dest = dlg.FileName;
            }
            args.ResultFilePath = dest;
            args.Handled        = true;
            AddDownloadItem(dl, System.IO.Path.GetFileName(dest));
        };
    }
    private static string BuildDestPath(string folder, string filename)
    {
        var dest = System.IO.Path.Combine(folder, filename);
        if (!System.IO.File.Exists(dest)) return dest;
        var name = System.IO.Path.GetFileNameWithoutExtension(filename);
        var ext  = System.IO.Path.GetExtension(filename);
        var n    = 1;
        while (System.IO.File.Exists(dest))
            dest = System.IO.Path.Combine(folder, $"{name} ({n++}){ext}");
        return dest;
    }
    private void SetActiveWebViewVisibleForOverlay(bool overlayOpen)
    {
        if (_activeTab?.WebViewControl == null) return;
        if (overlayOpen)
        {
            _suppressedWebViewForOverlay = _activeTab.WebViewControl;
            _activeTab.WebViewControl.Visibility = Visibility.Collapsed;
        }
        else if (_suppressedWebViewForOverlay != null)
        {
            _suppressedWebViewForOverlay.Visibility = Visibility.Visible;
            _suppressedWebViewForOverlay = null;
        }
    }
    private WebView2? _suppressedWebViewForOverlay;
    private void AddDownloadItem(Microsoft.Web.WebView2.Core.CoreWebView2DownloadOperation dl, string filename)
    {
        if (DownloadsPopup != null) DownloadsPopup.IsOpen = true;
        if (DownloadsPanel != null) DownloadsPanel.Visibility = Visibility.Visible;
        if (DownloadsEmptyHint != null) DownloadsEmptyHint.Visibility = Visibility.Collapsed;
        _downloadedFiles.Insert(0, new DownloadedFileInfo
        {
            Name = filename,
            Path = System.IO.Path.Combine(!string.IsNullOrWhiteSpace(_settings.DownloadFolder) && Directory.Exists(_settings.DownloadFolder) ? _settings.DownloadFolder : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", filename),
            TotalBytes = (long)(dl.TotalBytesToReceive ?? 0),
            Time = DateTime.Now,
            Uri = dl.Uri ?? ""
        });
        UpdateFullDownloadsPage(DownloadsSearchBox?.Text ?? "");

        bool runImmediately = _settings.AutoOpenDownloads;

        var item = new Border
        {
            Background = (Brush)FindResource("BgCardBrush"),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 8),
            BorderThickness = new Thickness(1),
            BorderBrush = (Brush)FindResource("BorderBrush")
        };

        var sp = new StackPanel();

        var topGrid = new Grid();
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameBlock = new TextBlock
        {
            Text = filename,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(nameBlock, 0);
        topGrid.Children.Add(nameBlock);

        var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(actionsPanel, 1);

        var btnRunWhenDone = new Button
        {
            Content = runImmediately ? "⚡ Запуск" : "▷ Запуск",
            Style = (Style)FindResource("NavButton"),
            FontSize = 11,
            Height = 24,
            Padding = new Thickness(6, 0, 6, 0),
            Margin = new Thickness(4, 0, 2, 0),
            Cursor = Cursors.Hand,
            Foreground = runImmediately ? (Application.Current?.Resources["AccentBrush"] as Brush ?? Brushes.Cyan) : (Brush)FindResource("TextMutedBrush")
        };
        btnRunWhenDone.Click += (_, _) =>
        {
            runImmediately = !runImmediately;
            btnRunWhenDone.Content = runImmediately ? "⚡ Запуск" : "▷ Запуск";
            btnRunWhenDone.Foreground = runImmediately ? (Application.Current?.Resources["AccentBrush"] as Brush ?? Brushes.Cyan) : (Brush)FindResource("TextMutedBrush");
        };
        actionsPanel.Children.Add(btnRunWhenDone);

        var btnCancel = new Button
        {
            Content = "",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            Style = (Style)FindResource("NavButton"),
            FontSize = 10,
            Width = 24,
            Height = 24,
            Margin = new Thickness(2, 0, 0, 0),
            Cursor = Cursors.Hand,
            Foreground = (Brush)FindResource("TextMutedBrush")
        };
        btnCancel.Click += (_, _) =>
        {
            try { dl.Cancel(); } catch { }
        };
        actionsPanel.Children.Add(btnCancel);

        var btnRunNow = new Button
        {
            Content = "",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            Style = (Style)FindResource("NavButton"),
            FontSize = 12,
            Width = 26,
            Height = 26,
            Margin = new Thickness(4, 0, 2, 0),
            Cursor = Cursors.Hand,
            Visibility = Visibility.Collapsed,
            Foreground = (Application.Current?.Resources["AccentBrush"] as Brush ?? Brushes.Cyan)
        };
        actionsPanel.Children.Add(btnRunNow);

        var btnFolder = new Button
        {
            Content = "",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            Style = (Style)FindResource("NavButton"),
            FontSize = 12,
            Width = 26,
            Height = 26,
            Margin = new Thickness(2, 0, 0, 0),
            Cursor = Cursors.Hand,
            Visibility = Visibility.Collapsed,
            Foreground = (Brush)FindResource("TextPrimaryBrush")
        };
        actionsPanel.Children.Add(btnFolder);

        topGrid.Children.Add(actionsPanel);
        sp.Children.Add(topGrid);

        var progress = new System.Windows.Controls.ProgressBar
        {
            Height = 4,
            Margin = new Thickness(0, 8, 0, 4),
            Background = (Brush)FindResource("BorderBrush"),
            Foreground = (Application.Current?.Resources["AccentBrush"] as Brush ?? Brushes.Cyan),
            BorderThickness = new Thickness(0),
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        sp.Children.Add(progress);

        var statusBlock = new TextBlock
        {
            Text = "Подготовка к загрузке...",
            Foreground = (Brush)FindResource("TextMutedBrush"),
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        };
        sp.Children.Add(statusBlock);

        item.Child = sp;
        DownloadsList.Children.Insert(0, item);

        dl.BytesReceivedChanged += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                if ((dl.TotalBytesToReceive ?? 0) > 0)
                {
                    progress.Value = dl.BytesReceived * 100.0 / (double)(dl.TotalBytesToReceive ?? 1);
                    var received = dl.BytesReceived / 1024.0 / 1024.0;
                    var total = (double)(dl.TotalBytesToReceive ?? 0) / 1024.0 / 1024.0;
                    statusBlock.Text = $"{received:F1} / {total:F1} МБ · {progress.Value:F0}%";
                }
            });
        };

        dl.StateChanged += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                switch (dl.State)
                {
                    case Microsoft.Web.WebView2.Core.CoreWebView2DownloadState.Completed:
                        progress.Value = 100;
                        var resPath = dl.ResultFilePath ?? "";
                        bool isMalicious = false;
                        string threatName = "";
                        string threatDetails = "";
                        if (_settings.AntiStealerProtection && !string.IsNullOrEmpty(resPath))
                        {
                            var scan = SecurityHelper.ScanForStealers(resPath);
                            if (scan.IsMalicious)
                            {
                                isMalicious = true;
                                threatName = scan.ThreatName;
                                threatDetails = scan.Details;
                            }
                        }

                        if (isMalicious)
                        {
                            progress.Foreground = Application.Current?.Resources["AccentRedBrush"] as Brush ?? Brushes.Red;
                            statusBlock.Text = $"🛡️ Заблокировано: {threatName}";
                            statusBlock.Foreground = Application.Current?.Resources["AccentRedBrush"] as Brush ?? Brushes.Red;
                            btnCancel.Visibility = Visibility.Collapsed;
                            btnRunWhenDone.Visibility = Visibility.Collapsed;
                            btnRunNow.Visibility = Visibility.Collapsed;
                            btnFolder.Visibility = Visibility.Collapsed;

                            try
                            {
                                var quarantinePath = resPath + ".quarantine";
                                if (!System.IO.File.Exists(quarantinePath))
                                {
                                    System.IO.File.Move(resPath, quarantinePath);
                                    resPath = quarantinePath;
                                }
                            }
                            catch { }

                            ShowStealerAlertModal(System.IO.Path.GetFileName(dl.ResultFilePath ?? filename), threatName, threatDetails, resPath);
                        }
                        else
                        {
                            progress.Foreground = Application.Current?.Resources["AccentGreenBrush"] as Brush ?? Brushes.Green;
                            statusBlock.Text = $"Завершено · {System.IO.Path.GetFileName(resPath)}";
                            btnCancel.Visibility = Visibility.Collapsed;
                            btnRunWhenDone.Visibility = Visibility.Collapsed;

                            btnRunNow.Visibility = Visibility.Visible;
                            btnRunNow.Click += (_, _) =>
                            {
                                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(resPath) { UseShellExecute = true }); }
                                catch { }
                            };

                            btnFolder.Visibility = Visibility.Visible;
                            btnFolder.Click += (_, _) =>
                            {
                                try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + resPath + "\""); }
                                catch { }
                            };

                            if (runImmediately && !string.IsNullOrEmpty(resPath))
                            {
                                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(resPath) { UseShellExecute = true }); }
                                catch { }
                            }
                            if (_settings.ShowDownloadBadge)
                                ShowDownloadToast(filename);
                        }
                        break;

                    case Microsoft.Web.WebView2.Core.CoreWebView2DownloadState.Interrupted:
                        progress.Foreground = Application.Current?.Resources["AccentRedBrush"] as Brush ?? Brushes.Red;
                        statusBlock.Text = dl.InterruptReason == Microsoft.Web.WebView2.Core.CoreWebView2DownloadInterruptReason.UserCanceled
                            ? "Загрузка отменена"
                            : $"Ошибка: {dl.InterruptReason}";
                        btnCancel.Visibility = Visibility.Collapsed;
                        btnRunWhenDone.Visibility = Visibility.Collapsed;
                        break;
                }
            });
        };
    }

    public class DownloadedFileInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long TotalBytes { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;
        public string Uri { get; set; } = "";
        public bool Completed { get; set; } = true;
    }
    private readonly List<DownloadedFileInfo> _downloadedFiles = new();

    public void ShowDownloadsTab()
    {
        var existing = _tabs.FirstOrDefault(t => t.Url == "nova://downloads");
        if (existing != null)
        {
            SwitchToTab(existing);
            return;
        }
        var tab = new BrowserTab
        {
            Title = "Загрузки",
            Url = "nova://downloads"
        };
        _tabs.Add(tab);
        SwitchToTab(tab);
    }
    public void OpenDownloadsTab() => ShowDownloadsTab();

    private void OpenFullDownloadsTab_Click(object sender, RoutedEventArgs e)
    {
        if (DownloadsPopup != null) DownloadsPopup.IsOpen = false;
        ShowDownloadsTab();
    }

    private void CloseDownloadsPage_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab != null && _activeTab.Url == "nova://downloads")
            CloseTab(_activeTab);
        else
            HideAllPages();
    }

    private void OpenDownloadsFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = !string.IsNullOrWhiteSpace(_settings.DownloadFolder) && Directory.Exists(_settings.DownloadFolder)
                ? _settings.DownloadFolder
                : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(folder))
                Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch { }
    }

    private void ClearAllDownloadsHistory_Click(object sender, RoutedEventArgs e)
    {
        _downloadedFiles.Clear();
        if (DownloadsList != null) DownloadsList.Children.Clear();
        if (DownloadsEmptyHint != null) DownloadsEmptyHint.Visibility = Visibility.Visible;
        UpdateFullDownloadsPage();
    }

    private void DownloadsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateFullDownloadsPage(DownloadsSearchBox?.Text ?? "");
    }

    private void UpdateFullDownloadsPage(string filter = "")
    {
        if (FullDownloadsListPanel == null) return;
        FullDownloadsListPanel.Children.Clear();
        var items = string.IsNullOrWhiteSpace(filter)
            ? _downloadedFiles.ToList()
            : _downloadedFiles.Where(d => d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                          d.Path.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        if (FullDownloadsEmptyHint != null)
            FullDownloadsEmptyHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var item in items)
        {
            var card = new Border
            {
                Background = (Brush)FindResource("BgCardBrush"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 10),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)FindResource("BorderBrush")
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconTb = new TextBlock
            {
                Text = "\uE896",
                FontFamily = (FontFamily)(TryFindResource("IconFont") ?? new FontFamily("Segoe MDL2 Assets")),
                FontSize = 22,
                Foreground = (Brush)FindResource("AccentBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };
            Grid.SetColumn(iconTb, 0);
            g.Children.Add(iconTb);

            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var nameTb = new TextBlock
            {
                Text = item.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimaryBrush")
            };
            var sizeStr = item.TotalBytes > 1024 * 1024 ? $"{(item.TotalBytes / (1024.0 * 1024.0)):F1} MB" : $"{(item.TotalBytes / 1024.0):F0} KB";
            var subTb = new TextBlock
            {
                Text = $"{sizeStr}  •  {item.Time:dd.MM.yyyy HH:mm}  •  {item.Path}",
                FontSize = 11,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Margin = new Thickness(0, 3, 0, 0)
            };
            sp.Children.Add(nameTb);
            sp.Children.Add(subTb);
            Grid.SetColumn(sp, 1);
            g.Children.Add(sp);

            var btnSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var openBtn = new Button
            {
                Content = "Открыть",
                Style = (Style)FindResource("NavButton"),
                Width = double.NaN,
                MinWidth = 80,
                Padding = new Thickness(12, 6, 12, 6),
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            openBtn.Click += (_, _) =>
            {
                try { if (File.Exists(item.Path)) Process.Start(new ProcessStartInfo { FileName = item.Path, UseShellExecute = true }); } catch { }
            };
            var folderBtn = new Button
            {
                Content = "В папке",
                Style = (Style)FindResource("NavButton"),
                Width = double.NaN,
                MinWidth = 80,
                Padding = new Thickness(12, 6, 12, 6),
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            folderBtn.Click += (_, _) =>
            {
                try
                {
                    if (File.Exists(item.Path))
                        Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{item.Path}\"", UseShellExecute = true });
                    else
                        OpenDownloadsFolder_Click(this, new RoutedEventArgs());
                }
                catch { }
            };
            var delBtn = new Button
            {
                Content = "✕",
                Style = (Style)FindResource("NavButton"),
                Width = 32,
                Height = 32,
                Foreground = (Brush)FindResource("AccentRedBrush")
            };
            delBtn.Click += (_, _) =>
            {
                _downloadedFiles.Remove(item);
                UpdateFullDownloadsPage(DownloadsSearchBox?.Text ?? "");
            };
            btnSp.Children.Add(openBtn);
            btnSp.Children.Add(folderBtn);
            btnSp.Children.Add(delBtn);
            Grid.SetColumn(btnSp, 2);
            g.Children.Add(btnSp);

            card.Child = g;
            FullDownloadsListPanel.Children.Add(card);
        }
    }

    private void DownloadsNavBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            e.Handled = true;
            if (DownloadsPopup != null) DownloadsPopup.IsOpen = false;
            ShowDownloadsTab();
        }
    }

    private DateTime _downloadsPopupClosedTime = DateTime.MinValue;
    private void MenuDownloads_Click(object sender, RoutedEventArgs e)
    {
        if (MenuPopup != null) MenuPopup.IsOpen = false;
        if (DownloadsPopup == null) return;
        if (DownloadsPanel != null) DownloadsPanel.Visibility = Visibility.Visible;
        if ((DateTime.Now - _downloadsPopupClosedTime).TotalMilliseconds < 300)
        {
            DownloadsPopup.IsOpen = false;
            return;
        }
        DownloadsPopup.IsOpen = !DownloadsPopup.IsOpen;
    }

    private void MenuDevTools_Click(object sender, RoutedEventArgs e)
    {
        if (MenuPopup != null) MenuPopup.IsOpen = false;
        ToggleDevTools();
    }

    private void CloseDownloadsPanel_Click(object sender, RoutedEventArgs e)
    {
        if (DownloadsPopup != null) DownloadsPopup.IsOpen = false;
    }

    private void ClearDownloads_Click(object sender, RoutedEventArgs e)
    {
        if (DownloadsList != null) DownloadsList.Children.Clear();
        if (DownloadsEmptyHint != null) DownloadsEmptyHint.Visibility = Visibility.Visible;
        if (DownloadsPopup != null) DownloadsPopup.IsOpen = false;
        _downloadedFiles.Clear();
        UpdateFullDownloadsPage();
    }

    private void HandleWebMessageFromPage(string json, BrowserTab tab)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("type", out var typeProp)) return;
            var msgType = typeProp.GetString();
            if (msgType == "novaPromptSavePassword")
            {
                var login = doc.RootElement.TryGetProperty("login", out var l) ? l.GetString() ?? "" : "";
                var pass = doc.RootElement.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";
                var site = doc.RootElement.TryGetProperty("site", out var s) ? s.GetString() ?? "" : "";
                if (!string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(pass))
                {
                    Dispatcher.Invoke(() => ShowSavePasswordPrompt(login, pass, site));
                }
            }
            else if (msgType == "navigate")
            {
                var u = doc.RootElement.TryGetProperty("url", out var el) ? el.GetString() : null;
                if (!string.IsNullOrEmpty(u)) Dispatcher.Invoke(() => NavigateTo(u));
            }
            else if (msgType == "open_tab")
            {
                var u = doc.RootElement.TryGetProperty("url", out var el) ? el.GetString() : null;
                if (!string.IsNullOrEmpty(u)) Dispatcher.Invoke(() => CreateNewTab(u));
            }

            else if (msgType == "open_settings")
            {
                Dispatcher.Invoke(OpenSettingsTabSafe);
            }
        }
        catch { }
    }

    private Border? _savePasswordBanner;
    private void ShowSavePasswordPrompt(string login, string password, string site)
    {
        try
        {
            var root = MainBorder?.Child as Grid;
            if (root == null) return;

            if (_savePasswordBanner != null && root.Children.Contains(_savePasswordBanner))
            {
                root.Children.Remove(_savePasswordBanner);
                _savePasswordBanner = null;
            }

            var banner = new Border
            {
                Background = Application.Current?.Resources["BgSurfaceBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(20, 23, 34)),
                BorderBrush = Application.Current?.Resources["AccentBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(190, 244, 123)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 10, 16, 10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 52, 20, 0)
            };
            try { banner.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 24, ShadowDepth = 6, Opacity = 0.5, Color = Colors.Black }; } catch { }
            Panel.SetZIndex(banner, 1500);
            Grid.SetRowSpan(banner, 6);

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var icon = new TextBlock
            {
                Text = "🔑",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            sp.Children.Add(icon);

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
            var title = new TextBlock
            {
                Text = "Сохранить пароль?",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Application.Current?.Resources["TextPrimaryBrush"] as Brush ?? Brushes.White
            };
            textStack.Children.Add(title);

            var desc = new TextBlock
            {
                Text = $"Пользователь: {login} ({(string.IsNullOrEmpty(site) ? "Все сайты" : site)})",
                FontSize = 11,
                Foreground = Application.Current?.Resources["TextMutedBrush"] as Brush ?? Brushes.Gray
            };
            textStack.Children.Add(desc);
            sp.Children.Add(textStack);

            var btnSave = new Button
            {
                Content = "Сохранить",
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.SemiBold,
                Background = Application.Current?.Resources["AccentBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(190, 244, 123)),
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0)
            };
            btnSave.Click += (_, _) =>
            {
                _settings.SavedPasswords ??= new();
                if (!_settings.SavedPasswords.Any(p => p.Login == login && p.Password == password))
                {
                    _settings.SavedPasswords.Add(new SavedPasswordItem
                    {
                        Site = string.IsNullOrEmpty(site) ? "*" : site,
                        Login = login,
                        Password = password,
                        Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                    });
                }
                _settings.SavedAccounts ??= new();
                if (!_settings.SavedAccounts.Any(a => a.Login == login))
                {
                    _settings.SavedAccounts.Add(new SavedAccountItem
                    {
                        Service = string.IsNullOrEmpty(site) ? "Веб-сайт" : site,
                        Login = login,
                        Email = login,
                        Password = password,
                        AddedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                    });
                }
                _settings.Save();
                if (_savePasswordBanner != null && root.Children.Contains(_savePasswordBanner))
                {
                    root.Children.Remove(_savePasswordBanner);
                    _savePasswordBanner = null;
                }
                PushAdBlockerStateToAllTabs();
                ShowToast("Пароль сохранён и доступен для автозаполнения");
            };
            sp.Children.Add(btnSave);

            var btnCancel = new Button
            {
                Content = "Не сейчас",
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                Foreground = Application.Current?.Resources["TextPrimaryBrush"] as Brush ?? Brushes.White,
                BorderBrush = Application.Current?.Resources["BorderBrush"] as Brush ?? Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            btnCancel.Click += (_, _) =>
            {
                if (_savePasswordBanner != null && root.Children.Contains(_savePasswordBanner))
                {
                    root.Children.Remove(_savePasswordBanner);
                    _savePasswordBanner = null;
                }
            };
            sp.Children.Add(btnCancel);

            banner.Child = sp;
            _savePasswordBanner = banner;
            root.Children.Add(banner);
        }
        catch { }
    }

    private Border? _stealerAlertModal;
    private void ShowStealerAlertModal(string filename, string threatName, string details, string quarantinePath)
    {
        try
        {
            var root = MainBorder?.Child as Grid;
            if (root == null) return;

            if (_stealerAlertModal != null && root.Children.Contains(_stealerAlertModal))
            {
                root.Children.Remove(_stealerAlertModal);
                _stealerAlertModal = null;
            }

            var overlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Panel.SetZIndex(overlay, 2500);
            Grid.SetRowSpan(overlay, 6);

            var card = new Border
            {
                Width = 520,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Application.Current?.Resources["BgCardBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(26, 16, 20)),
                BorderBrush = Application.Current?.Resources["AccentRedBrush"] as Brush ?? Brushes.Red,
                BorderThickness = new Thickness(2)
            };
            try { card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 32, ShadowDepth = 8, Opacity = 0.7, Color = Colors.Red }; } catch { }

            var stack = new StackPanel();

            var head = new TextBlock
            {
                Text = "🛡️ Nova Shield: Обнаружена угроза безопасности!",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = Application.Current?.Resources["AccentRedBrush"] as Brush ?? Brushes.Red,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(head);

            var fileInfo = new TextBlock
            {
                Text = $"Файл: {filename}",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Application.Current?.Resources["TextPrimaryBrush"] as Brush ?? Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            };
            stack.Children.Add(fileInfo);

            var threatInfo = new TextBlock
            {
                Text = $"Тип угрозы: {threatName}",
                FontSize = 13,
                Foreground = Application.Current?.Resources["AccentAmberBrush"] as Brush ?? Brushes.Orange,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(threatInfo);

            var descBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 15, 10, 12)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 60, 60)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 18)
            };
            var descText = new TextBlock
            {
                Text = details + "\n\nNova Browser заблокировал автоматический запуск файла и изолировал его с расширением .quarantine. Рекомендуется удалить данный файл для предотвращения утечки Discord токенов, сессий Minecraft и паролей.",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
                Foreground = Application.Current?.Resources["TextPrimaryBrush"] as Brush ?? Brushes.White
            };
            descBox.Child = descText;
            stack.Children.Add(descBox);

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var btnDelete = new Button
            {
                Content = "Удалить вредоносный файл",
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.Bold,
                Background = Application.Current?.Resources["AccentRedBrush"] as Brush ?? Brushes.Red,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnDelete.Click += (_, _) =>
            {
                try
                {
                    if (System.IO.File.Exists(quarantinePath))
                        System.IO.File.Delete(quarantinePath);
                }
                catch { }
                if (_stealerAlertModal != null && root.Children.Contains(_stealerAlertModal))
                {
                    root.Children.Remove(_stealerAlertModal);
                    _stealerAlertModal = null;
                }
                ShowToast("Вредоносный файл удалён");
            };
            btnRow.Children.Add(btnDelete);

            var btnKeepQuarantine = new Button
            {
                Content = "Оставить в карантине",
                Padding = new Thickness(14, 8, 14, 8),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                Foreground = Application.Current?.Resources["TextPrimaryBrush"] as Brush ?? Brushes.White,
                BorderBrush = Application.Current?.Resources["BorderBrush"] as Brush ?? Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            btnKeepQuarantine.Click += (_, _) =>
            {
                if (_stealerAlertModal != null && root.Children.Contains(_stealerAlertModal))
                {
                    root.Children.Remove(_stealerAlertModal);
                    _stealerAlertModal = null;
                }
            };
            btnRow.Children.Add(btnKeepQuarantine);

            stack.Children.Add(btnRow);
            card.Child = stack;
            overlay.Child = card;
            _stealerAlertModal = overlay;
            root.Children.Add(overlay);
        }
        catch { }
    }

    public async Task<bool> InstallExtensionAsync(string path)
    {
        try
        {
            if (_activeTab?.WebViewControl?.CoreWebView2?.Profile == null) return false;
            var profile = _activeTab.WebViewControl.CoreWebView2.Profile;
            await profile.AddBrowserExtensionAsync(path);
            _settings.InstalledExtensionPaths ??= new();
            if (!_settings.InstalledExtensionPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                _settings.InstalledExtensionPaths.Add(path);
                _settings.Save();
            }
            return true;
        }
        catch { return false; }
    }

    public async Task<IReadOnlyList<Microsoft.Web.WebView2.Core.CoreWebView2BrowserExtension>?> GetInstalledExtensionsAsync()
    {
        try
        {
            if (_activeTab?.WebViewControl?.CoreWebView2?.Profile == null) return null;
            return await _activeTab.WebViewControl.CoreWebView2.Profile.GetBrowserExtensionsAsync();
        }
        catch { return null; }
    }

    public async Task ClearBrowsingDataChromeAsync(Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds kinds, DateTime? startTime = null)
    {
        try
        {
            if (_activeTab?.WebViewControl?.CoreWebView2?.Profile != null)
            {
                var profile = _activeTab.WebViewControl.CoreWebView2.Profile;
                if (startTime.HasValue)
                {
                    await profile.ClearBrowsingDataAsync(kinds, startTime.Value, DateTime.MaxValue);
                }
                else
                {
                    await profile.ClearBrowsingDataAsync(kinds);
                }
            }
        }
        catch { }
    }

    public void PushAdBlockerStateToAllTabs()
    {
        try
        {
            _cachedInjectionScript = null;
            var script = GetCachedInjectionScript();
            RebuildCustomDomainCache();
            foreach (var tab in _tabs)
            {
                if (tab?.WebViewControl?.CoreWebView2 != null)
                {
                    try
                    {
                        _ = tab.WebViewControl.CoreWebView2.ExecuteScriptAsync(script);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private async void ToggleReaderMode()
    {
        if (ReaderOverlay.Visibility == Visibility.Visible)
        {
            ReaderOverlay.Visibility = Visibility.Collapsed;
            return;
        }
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) { StatusText.Text = "Нет страницы для режима чтения"; return; }
        AnimateStatusText("Извлечение текста...");
        try
        {
            const string script = @"(function(){
  var title = document.title || '';
  var sel = ['article','[role=""main""]','main','.article-body','.post-content',
             '.entry-content','.story-body','.article__content','.content-body',
             '#article-body','#content','#main-content'];
  var el = null;
  for(var i=0;i<sel.length;i++){el=document.querySelector(sel[i]);if(el)break;}
  if(!el) el = document.body;
  var clone = el.cloneNode(true);
  var rm = ['nav','header','footer','aside','script','style','noscript','iframe',
            '.ad','.ads','.advertisement','.sidebar','.widget','.comments',
            '#comments','[aria-label=""advertisement""]'];
  rm.forEach(function(s){
    try{clone.querySelectorAll(s).forEach(function(n){n.remove();});}catch(e){}
  });
  var parts = [];
  clone.querySelectorAll('h1,h2,h3,h4,p,blockquote,ul,ol,li').forEach(function(n){
    var t = (n.innerText||n.textContent||'').trim();
    if(!t) return;
    if(n.tagName==='H1') parts.push('H1|'+t);
    else if(n.tagName==='H2') parts.push('H2|'+t);
    else if(n.tagName==='H3'||n.tagName==='H4') parts.push('H3|'+t);
    else if(n.tagName==='BLOCKQUOTE') parts.push('BQ|'+t);
    else if(n.tagName==='LI') parts.push('LI|'+t);
    else if(t.length>30) parts.push('P|'+t);
  });
  return JSON.stringify({title:title, parts:parts});
})()";
            var raw = await wv.ExecuteScriptAsync(script);
            var inner = System.Text.Json.JsonSerializer.Deserialize<string>(raw) ?? "{}";
            using var doc = System.Text.Json.JsonDocument.Parse(inner);
            var title = doc.RootElement.GetProperty("title").GetString() ?? "";
            var partsEl = doc.RootElement.GetProperty("parts");
            var parts = new List<(string kind, string text)>();
            foreach (var p in partsEl.EnumerateArray())
            {
                var s = p.GetString() ?? "";
                var pipe = s.IndexOf('|');
                if (pipe > 0)
                    parts.Add((s[..pipe], s[(pipe + 1)..]));
            }
            if (parts.Count == 0)
            {
                StatusText.Text = "Не удалось извлечь текст — страница не поддерживает режим чтения";
                return;
            }
            BuildReaderDocument(title, parts);
            ReaderOverlay.Visibility = Visibility.Visible;
            AnimateSlideIn(ReaderOverlay);
            AnimateStatusText("Режим чтения");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка режима чтения: {ex.Message}";
        }
    }
    private void BuildReaderDocument(string title, List<(string kind, string text)> parts)
    {
        var doc = new System.Windows.Documents.FlowDocument
        {
            PagePadding = new Thickness(0),
            LineHeight  = double.NaN,
        };
        ApplyReaderTheme(doc);
        if (!string.IsNullOrEmpty(title))
        {
            var tp = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(title))
            {
                FontSize   = ReaderFontSizeSlider != null ? ReaderFontSizeSlider.Value * 1.5 : 28,
                FontWeight = FontWeights.Bold,
                FontFamily = GetReaderFont(),
                Margin     = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(tp);
        }
        foreach (var (kind, text) in parts)
        {
            System.Windows.Documents.Block block;
            switch (kind)
            {
                case "H1":
                case "H2":
                    block = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text))
                    {
                        FontSize   = ReaderFontSizeSlider != null ? ReaderFontSizeSlider.Value * 1.3 : 24,
                        FontWeight = FontWeights.SemiBold,
                        FontFamily = GetReaderFont(),
                        Margin     = new Thickness(0, 18, 0, 6)
                    };
                    break;
                case "H3":
                    block = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text))
                    {
                        FontSize   = ReaderFontSizeSlider != null ? ReaderFontSizeSlider.Value * 1.1 : 20,
                        FontWeight = FontWeights.SemiBold,
                        FontFamily = GetReaderFont(),
                        Margin     = new Thickness(0, 14, 0, 4)
                    };
                    break;
                case "BQ":
                    block = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text))
                    {
                        FontSize   = ReaderFontSizeSlider?.Value ?? 18,
                        FontStyle  = FontStyles.Italic,
                        FontFamily = GetReaderFont(),
                        Padding    = new Thickness(20, 8, 20, 8),
                        Margin     = new Thickness(0, 8, 0, 8),
                        Foreground = new SolidColorBrush(Color.FromArgb(180, 150, 150, 200))
                    };
                    break;
                case "LI":
                    block = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("• " + text))
                    {
                        FontSize   = ReaderFontSizeSlider?.Value ?? 18,
                        FontFamily = GetReaderFont(),
                        Margin     = new Thickness(16, 2, 0, 2)
                    };
                    break;
                default:
                    block = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text))
                    {
                        FontSize   = ReaderFontSizeSlider?.Value ?? 18,
                        FontFamily = GetReaderFont(),
                        Margin     = new Thickness(0, 0, 0, 14)
                    };
                    break;
            }
            doc.Blocks.Add(block);
        }
        ReaderContent.Document = doc;
    }
    private FontFamily GetReaderFont()
    {
        if (ReaderFontCombo?.SelectedItem is ComboBoxItem ci)
            return new FontFamily(ci.Tag?.ToString() ?? "Georgia");
        return new FontFamily("Georgia");
    }
    private void ApplyReaderTheme(System.Windows.Documents.FlowDocument doc)
    {
        var tag = (ReaderThemeCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "dark";
        switch (tag)
        {
            case "light":
                ReaderOverlay.Background = Brushes.White;
                doc.Foreground = Brushes.Black;
                break;
            case "sepia":
                ReaderOverlay.Background = new SolidColorBrush(Color.FromRgb(0xFB, 0xF0, 0xD9));
                doc.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x3F, 0x35));
                break;
            default:
                ReaderOverlay.Background = (Brush)FindResource("BgDarkBrush");
                doc.Foreground = (Brush)FindResource("TextPrimaryBrush");
                break;
        }
        if (ReaderContent != null)
        {
            ReaderContent.Background = Brushes.Transparent;
            ReaderContent.Foreground = doc.Foreground;
        }
    }
    private void CloseReader_Click(object sender, RoutedEventArgs e)
        => ReaderOverlay.Visibility = Visibility.Collapsed;
    private void MenuReader_Click(object sender, RoutedEventArgs e)
    { MenuPopup.IsOpen = false; ToggleReaderMode(); }
    private void MenuPiP_Click(object sender, RoutedEventArgs e)
    { MenuPopup.IsOpen = false; TogglePictureInPicture(); }
    private void ReaderFont_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ReaderContent?.Document == null) return;
        var font = GetReaderFont();
        foreach (var block in ReaderContent.Document.Blocks)
        {
            if (block is System.Windows.Documents.Paragraph p)
                p.FontFamily = font;
        }
    }
    private void ReaderTheme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ReaderOverlay == null || ReaderContent?.Document == null) return;
        ApplyReaderTheme(ReaderContent.Document);
    }
    private void ReaderFontSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ReaderContent?.Document == null) return;
        var size = e.NewValue;
        foreach (var block in ReaderContent.Document.Blocks)
        {
            if (block is System.Windows.Documents.Paragraph p &&
                p.Margin.Top < 10)
                p.FontSize = size;
        }
    }
    private void PinTab(BrowserTab tab)
    {
        tab.IsPinned = !tab.IsPinned;
        if (tab.IsPinned)
        {
            _tabs.Remove(tab);
            int lastPinned = _tabs.FindLastIndex(t => t.IsPinned);
            _tabs.Insert(lastPinned + 1, tab);
        }
        UpdateTabsUI();
        StatusText.Text = tab.IsPinned ? "Вкладка закреплена" : "Вкладка откреплена";
    }

    private void SaveSession()
    {
        if (_isFullResetting) return;
        if (!_settings.RestoreLastSession) return;
        var validUrls = new List<string>();
        foreach (var t in _tabs)
        {
            if (t.IsPrivate) continue;

            if (t.Url == "nova://settings" || t.Url == "nova://downloads" || t.Url == "nova://history")
            {
                validUrls.Add(t.Url);
                continue;
            }

            if (t.Url.StartsWith("nova://search?q=", StringComparison.OrdinalIgnoreCase))
            {
                validUrls.Add(t.Url);
                continue;
            }

            var live = t.WebViewControl?.Source?.ToString();
            var target = (!string.IsNullOrWhiteSpace(live) && live != "about:blank") ? live : t.Url;

            if (string.IsNullOrWhiteSpace(target) || target == "about:blank")
            {
                validUrls.Add("nova://newtab");
                continue;
            }

            if (IsNovaHost(target))
            {
                if (target.Contains("#/search?q="))
                {
                    var qMatch = System.Text.RegularExpressions.Regex.Match(target, @"[?&]q=([^&]*)");
                    if (qMatch.Success)
                    {
                        var q = Uri.UnescapeDataString(qMatch.Groups[1].Value);
                        validUrls.Add($"nova://search?q={Uri.EscapeDataString(q)}");
                        continue;
                    }
                }
                validUrls.Add("nova://newtab");
                continue;
            }

            if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                validUrls.Add(target);
            }
        }

        _settings.LastSession = validUrls;
        _settings.LastSessionActiveIndex = _activeTab != null ? Math.Max(0, _tabs.IndexOf(_activeTab)) : 0;
        SaveSettings();
    }

    private async void RestoreSession()
    {
        if (!_settings.RestoreLastSession || _settings.LastSession == null || _settings.LastSession.Count == 0)
        {
            CreateNewTab();
            return;
        }
        var validUrls = _settings.LastSession
            .Where(u => !string.IsNullOrWhiteSpace(u) && u != "about:blank")
            .ToList();
        if (validUrls.Count == 0)
        {
            CreateNewTab();
            return;
        }

        int activeIdx = Math.Clamp(_settings.LastSessionActiveIndex, 0, validUrls.Count - 1);
        for (int i = 0; i < validUrls.Count; i++)
        {
            bool isAct = (i == activeIdx);
            await CreateNewTabAsync(validUrls[i], isAct);
        }

        if (_tabs.Count > 0)
        {
            var tabToActivate = (activeIdx >= 0 && activeIdx < _tabs.Count) ? _tabs[activeIdx] : _tabs[0];
            SwitchToTab(tabToActivate);
        }
    }
    private static readonly Dictionary<string, (string, string, string, string)> _themeBlobs = new()
    {
        ["Dark"]          = ("#BEF47B", "#7B2FFF", "#00FFB3", "#FF6B6B"),
        ["Nord"]          = ("#88C0D0", "#5E81AC", "#A3BE8C", "#B48EAD"),
        ["Mocha"]         = ("#CBA6F7", "#F38BA8", "#89DCEB", "#A6E3A1"),
        ["Dracula"]       = ("#BD93F9", "#FF79C6", "#8BE9FD", "#50FA7B"),
        ["OneDark"]       = ("#61AFEF", "#C678DD", "#98C379", "#E06C75"),
        ["TokyoNight"]    = ("#7AA2F7", "#BB9AF7", "#73DACA", "#F7768E"),
        ["Gruvbox"]       = ("#D79921", "#CC241D", "#689D6A", "#458588"),
        ["Midnight"]      = ("#00B4FF", "#8855FF", "#00FFCC", "#FF4488"),
        ["RosePine"]      = ("#EBBCBA", "#C4A7E7", "#9CCFD8", "#EB6F92"),
        ["Solarized"]     = ("#268BD2", "#2AA198", "#859900", "#D33682"),
        ["Evening"]       = ("#E0A070", "#C070E0", "#70C0E0", "#E07090"),
        ["Light"]         = ("#0078D4", "#744DA9", "#00B294", "#E74856"),
        ["Win7"]          = ("#1E90FF", "#4169E1", "#00BFFF", "#87CEEB"),
        ["Cyberpunk"]     = ("#FF2079", "#00F0FF", "#FFE600", "#BF00FF"),
        ["NeonGreen"]     = ("#39FF14", "#00FF7F", "#7FFF00", "#ADFF2F"),
        ["NeonPink"]      = ("#FF10F0", "#FF69B4", "#FF1493", "#FF6EB4"),
        ["NeonBlue"]      = ("#00BFFF", "#1E90FF", "#00FFFF", "#7B68EE"),
        ["Forest"]        = ("#228B22", "#6B8E23", "#90EE90", "#2E8B57"),
        ["Sakura"]        = ("#FFB7C5", "#FF69B4", "#FFC0CB", "#FF91A4"),
        ["Ocean"]         = ("#006994", "#00CED1", "#48D1CC", "#40E0D0"),
        ["Autumn"]        = ("#D2691E", "#CD853F", "#DAA520", "#B8860B"),
        ["Desert"]        = ("#DEB887", "#D2691E", "#F4A460", "#C19A6B"),
        ["Mint"]          = ("#98FF98", "#3EB489", "#00FA9A", "#7FFFD4"),
        ["Sunset"]        = ("#FF6B35", "#FF8C42", "#FFC65B", "#FF4500"),
        ["Coffee"]        = ("#6F4E37", "#A0522D", "#DEB887", "#D2691E"),
        ["Amber"]         = ("#FFBF00", "#FF8C00", "#FFD700", "#FFA500"),
        ["Copper"]        = ("#B87333", "#DA8A67", "#CB6D51", "#AD6F3B"),
        ["Galaxy"]        = ("#9B59B6", "#3498DB", "#E74C3C", "#2ECC71"),
        ["Matrix"]        = ("#00FF41", "#008F11", "#003B00", "#00FF41"),
        ["Steel"]         = ("#708090", "#B0C4DE", "#4682B4", "#778899"),
        ["Slate"]         = ("#6A5ACD", "#7B68EE", "#9370DB", "#483D8B"),
        ["HighContrast"]  = ("#FFFFFF", "#FFFF00", "#00FF00", "#FF0000"),
        ["BloodMoon"]     = ("#8B0000", "#DC143C", "#FF4444", "#FF6666"),
        ["PastelDark"]    = ("#B5A8FF", "#FFB3DE", "#A8FFD8", "#FFD8A8"),
        ["Lavender"]      = ("#967BB6", "#C39BD3", "#7D3C98", "#A569BD"),
        ["Coral"]         = ("#FF6B6B", "#FF8E53", "#FFA07A", "#FF7F50"),
        ["Spring"]        = ("#7EC8E3", "#A8D8A8", "#F9E4B7", "#F5A7A7"),
        ["Volcanic"]      = ("#FF4500", "#FF6347", "#FF8C00", "#DC143C"),
        ["Cobalt"]        = ("#0047AB", "#0077BB", "#00AADD", "#0033FF"),
        ["Emerald"]       = ("#50C878", "#2ECC71", "#27AE60", "#1ABC9C"),
        ["Arctic"]        = ("#B0E0E6", "#87CEEB", "#ADD8E6", "#E0FFFF"),
        ["MidnightPurple"]= ("#6A0DAD", "#9B30FF", "#7B2FBE", "#4B0082"),
        ["Golden"]        = ("#FFD700", "#FFA500", "#DAA520", "#B8860B"),
        ["Custom"]        = ("#BEF47B", "#7B2FFF", "#00FFB3", "#FF6B6B"),
    };
    private DispatcherTimer? _blobTimer;
    private double _blobT = 0;
    private void ApplyThemeWallpaper(string theme)
    {
        if (!_themeBlobs.TryGetValue(theme, out var colors))
            colors = _themeBlobs["Dark"];
        var (c1, c2, c3, c4) = colors;
        SetBlobColor(Blob1, Blob1Stop1, c1);
        SetBlobColor(Blob2, Blob2Stop1, c2);
        SetBlobColor(Blob3, Blob3Stop1, c3);
        SetBlobColor(Blob4, Blob4Stop1, c4);
        if (WallpaperLayer != null && WallpaperLayer.Visibility == Visibility.Visible)
            StartBlobAnimation();
        else
            StopBlobAnimation();
    }
    private static void SetBlobColor(System.Windows.Shapes.Ellipse? blob, GradientStop? stop, string hex)
    {
        if (blob == null || stop == null) return;
        try { stop.Color = (Color)ColorConverter.ConvertFromString(hex); } catch { }
    }
    private void StartBlobAnimation()
    {
        if (WallpaperLayer == null || WallpaperLayer.Visibility != Visibility.Visible)
        {
            StopBlobAnimation();
            return;
        }
        _blobTimer?.Stop();
        _blobT = 0;
        _blobTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _blobTimer.Tick += AnimateBlobs;
        _blobTimer.Start();
    }
    private void AnimateBlobs(object? sender, EventArgs e)
    {
        if (WallpaperLayer == null || WallpaperLayer.Visibility != Visibility.Visible)
        {
            StopBlobAnimation();
            return;
        }
        if (Blob1 == null || AnimWallpaperCanvas == null) return;
        _blobT += 0.008;
        var w = AnimWallpaperCanvas.ActualWidth;
        var h = AnimWallpaperCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;
        Canvas.SetLeft(Blob1, w * 0.5 * (1 + 0.55 * Math.Sin(_blobT * 0.7))  - 300);
        Canvas.SetTop (Blob1, h * 0.5 * (1 + 0.50 * Math.Cos(_blobT * 0.5))  - 300);
        Canvas.SetLeft(Blob2, w * 0.5 * (1 + 0.60 * Math.Cos(_blobT * 0.9 + 1.0)) - 250);
        Canvas.SetTop (Blob2, h * 0.5 * (1 + 0.55 * Math.Sin(_blobT * 0.6 + 2.0)) - 250);
        Canvas.SetLeft(Blob3, w * 0.5 * (1 + 0.45 * Math.Sin(_blobT * 1.1 + 3.0)) - 225);
        Canvas.SetTop (Blob3, h * 0.5 * (1 + 0.60 * Math.Cos(_blobT * 0.8 + 1.5)) - 225);
        Canvas.SetLeft(Blob4, w * 0.5 * (1 + 0.65 * Math.Cos(_blobT * 0.6 + 4.0)) - 150);
        Canvas.SetTop (Blob4, h * 0.5 * (1 + 0.45 * Math.Sin(_blobT * 1.2 + 0.5)) - 150);
        Blob1.Opacity = 0.15 + 0.07 * Math.Sin(_blobT * 1.3);
        Blob2.Opacity = 0.12 + 0.06 * Math.Cos(_blobT * 0.9 + 1.0);
        Blob3.Opacity = 0.10 + 0.05 * Math.Sin(_blobT * 1.1 + 2.0);
        Blob4.Opacity = 0.08 + 0.05 * Math.Cos(_blobT * 1.5 + 3.0);
    }
    private void StopBlobAnimation()
    {
        _blobTimer?.Stop();
        _blobTimer = null;
    }

    private void ToggleDevTools()
    {
        if (DevToolsDockPanel == null) return;
        bool isVisible = DevToolsDockPanel.Visibility == Visibility.Visible;
        if (isVisible)
        {
            DevToolsDockPanel.Visibility = Visibility.Collapsed;
            if (DevToolsSplitter != null) DevToolsSplitter.Visibility = Visibility.Collapsed;
        }
        else
        {
            DevToolsDockPanel.Visibility = Visibility.Visible;
            if (DevToolsSplitter != null) DevToolsSplitter.Visibility = Visibility.Visible;
            if (DtConsoleView != null && DtConsoleView.Visibility == Visibility.Visible)
            {
                DtConsoleInput?.Focus();
            }
            AppendDevToolsConsoleLog("Nova DevTools активирован. Подключено к текущей вкладке: " + (_activeTab?.Title ?? "about:blank"), "info");
        }
    }

    private void DevToolsTabConsole_Click(object sender, RoutedEventArgs e)
    {
        SetDevToolsView(DtConsoleView);
        DtConsoleInput?.Focus();
    }

    private void DevToolsTabElements_Click(object sender, RoutedEventArgs e)
    {
        SetDevToolsView(DtElementsView);
        DtRefreshElements_Click(sender, e);
    }

    private void DevToolsTabNetwork_Click(object sender, RoutedEventArgs e)
    {
        SetDevToolsView(DtNetworkView);
    }

    private void DevToolsTabStorage_Click(object sender, RoutedEventArgs e)
    {
        SetDevToolsView(DtStorageView);
        DtRefreshStorage_Click(sender, e);
    }

    private void DevToolsTabSecurity_Click(object sender, RoutedEventArgs e)
    {
        SetDevToolsView(DtSecurityView);
        UpdateDevToolsSecurityView();
    }

    private void SetDevToolsView(FrameworkElement? activeView)
    {
        if (DtConsoleView != null) DtConsoleView.Visibility = Visibility.Collapsed;
        if (DtElementsView != null) DtElementsView.Visibility = Visibility.Collapsed;
        if (DtNetworkView != null) DtNetworkView.Visibility = Visibility.Collapsed;
        if (DtStorageView != null) DtStorageView.Visibility = Visibility.Collapsed;
        if (DtSecurityView != null) DtSecurityView.Visibility = Visibility.Collapsed;
        if (activeView != null) activeView.Visibility = Visibility.Visible;
    }

    private void DevToolsUndock_Click(object sender, RoutedEventArgs e)
    {
        if (DevToolsDockPanel != null) DevToolsDockPanel.Visibility = Visibility.Collapsed;
        if (DevToolsSplitter != null) DevToolsSplitter.Visibility = Visibility.Collapsed;
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv != null)
        {
            wv.Settings.AreDevToolsEnabled = true;
            try { wv.OpenDevToolsWindow(); } catch { }
        }
    }

    private void DevToolsClose_Click(object sender, RoutedEventArgs e)
    {
        if (DevToolsDockPanel != null) DevToolsDockPanel.Visibility = Visibility.Collapsed;
        if (DevToolsSplitter != null) DevToolsSplitter.Visibility = Visibility.Collapsed;
    }

    private void DtClearConsole_Click(object sender, RoutedEventArgs e)
    {
        DtConsoleLogList?.Children.Clear();
    }

    private void DtConsoleFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DtConsoleLogList == null) return;
        var q = DtConsoleFilter?.Text.Trim().ToLowerInvariant() ?? "";
        foreach (UIElement child in DtConsoleLogList.Children)
        {
            if (child is FrameworkElement fe)
            {
                if (string.IsNullOrEmpty(q))
                    fe.Visibility = Visibility.Visible;
                else
                {
                    var tbText = (child as TextBlock)?.Text ?? "";
                    fe.Visibility = tbText.ToLowerInvariant().Contains(q) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
    }

    private void DtConsoleInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DtConsoleRun_Click(sender, e);
            e.Handled = true;
        }
    }

    private async void DtConsoleRun_Click(object sender, RoutedEventArgs e)
    {
        var input = DtConsoleInput?.Text;
        if (string.IsNullOrWhiteSpace(input)) return;
        DtConsoleInput!.Text = "";
        AppendDevToolsConsoleLog("> " + input, "prompt");
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null)
        {
            AppendDevToolsConsoleLog("Ошибка: активная страница не готова к выполнению JavaScript", "error");
            return;
        }
        try
        {
            var res = await wv.ExecuteScriptAsync(input);
            AppendDevToolsConsoleLog(res ?? "undefined", "result");
        }
        catch (Exception ex)
        {
            AppendDevToolsConsoleLog(ex.Message, "error");
        }
    }

    private void AppendDevToolsConsoleLog(string msg, string kind)
    {
        if (DtConsoleLogList == null) return;
        var tb = new TextBlock
        {
            Text = $"[{DateTime.Now:HH:mm:ss}] {msg}",
            FontFamily = new FontFamily("Cascadia Code, Consolas, monospace"),
            FontSize = 12,
            Margin = new Thickness(0, 2, 0, 2),
            TextWrapping = TextWrapping.Wrap
        };
        switch (kind)
        {
            case "prompt":
                tb.Foreground = (Brush)FindResource("AccentBrush");
                tb.FontWeight = FontWeights.SemiBold;
                break;
            case "error":
                tb.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                break;
            case "result":
                tb.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                break;
            case "info":
                tb.Foreground = new SolidColorBrush(Color.FromRgb(147, 197, 253));
                break;
            default:
                tb.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                break;
        }
        DtConsoleLogList.Children.Add(tb);
        DtConsoleScroller?.ScrollToEnd();
    }

    private async void DtRefreshElements_Click(object sender, RoutedEventArgs e)
    {
        if (DtElementsTree == null) return;
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null)
        {
            DtElementsTree.Text = "Активная страница не загружена.";
            return;
        }
        try
        {
            var raw = await wv.ExecuteScriptAsync("(function(){ return window.__novaSelectedEl ? window.__novaSelectedEl.outerHTML : (document.documentElement ? document.documentElement.outerHTML : ''); })()");
            var html = System.Text.Json.JsonSerializer.Deserialize<string>(raw ?? "") ?? "";
            DtElementsTree.Text = string.IsNullOrEmpty(html) ? "DOM дерево пусто." : html;
            if (DtElementsStatus != null) DtElementsStatus.Text = "Дерево DOM обновлено (" + DateTime.Now.ToString("HH:mm:ss") + ")";
        }
        catch (Exception ex)
        {
            DtElementsTree.Text = "Ошибка получения DOM: " + ex.Message;
        }
    }

    private async void DtInspectElement_Click(object sender, RoutedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        try
        {
            var script = @"
(function() {
    if (window.__novaInspectCleanup) {
        window.__novaInspectCleanup();
        return 'disabled';
    }
    var overlay = document.getElementById('__nova_inspect_overlay');
    if (!overlay) {
        overlay = document.createElement('div');
        overlay.id = '__nova_inspect_overlay';
        overlay.style.position = 'fixed';
        overlay.style.pointerEvents = 'none';
        overlay.style.zIndex = '2147483647';
        overlay.style.background = 'rgba(190, 244, 123, 0.25)';
        overlay.style.border = '2px solid #BEF47B';
        overlay.style.boxSizing = 'border-box';
        document.body.appendChild(overlay);
    }
    overlay.style.display = 'block';

    function onMove(ev) {
        var el = document.elementFromPoint(ev.clientX, ev.clientY);
        if (!el || el.id === '__nova_inspect_overlay') return;
        var r = el.getBoundingClientRect();
        overlay.style.left = r.left + 'px';
        overlay.style.top = r.top + 'px';
        overlay.style.width = r.width + 'px';
        overlay.style.height = r.height + 'px';
    }

    function onClick(ev) {
        ev.preventDefault();
        ev.stopPropagation();
        var el = document.elementFromPoint(ev.clientX, ev.clientY);
        if (el && el.id !== '__nova_inspect_overlay') {
            window.__novaSelectedEl = el;
            cleanup();
            var sel = el.tagName.toLowerCase() + (el.id ? '#' + el.id : '') + (el.className && typeof el.className === 'string' && el.className.trim() ? '.' + el.className.trim().split(/\s+/).join('.') : '');
            window.chrome.webview.postMessage(JSON.stringify({ type: 'nova_element_inspected', selector: sel, html: el.outerHTML }));
        }
    }

    function cleanup() {
        if (overlay) overlay.style.display = 'none';
        document.removeEventListener('mousemove', onMove, true);
        document.removeEventListener('click', onClick, true);
        window.__novaInspectCleanup = null;
    }

    window.__novaInspectCleanup = cleanup;
    document.addEventListener('mousemove', onMove, true);
    document.addEventListener('click', onClick, true);
    return 'enabled';
})();";
            var res = await wv.ExecuteScriptAsync(script);
            if (DtElementsStatus != null)
            {
                DtElementsStatus.Text = res?.Contains("enabled") == true
                    ? "Режим выбора активен: наведите курсор на элемент на веб-странице и нажмите ЛКМ"
                    : "Режим инспекции отключен";
            }
        }
        catch (Exception ex)
        {
            if (DtElementsStatus != null) DtElementsStatus.Text = "Ошибка инспектора: " + ex.Message;
        }
    }

    private async void DtApplyHtml_Click(object sender, RoutedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        var html = DtElementsTree?.Text ?? "";
        if (string.IsNullOrEmpty(html)) return;
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(html);
            var script = $@"(function() {{
    var newHtml = {json};
    if (window.__novaSelectedEl) {{
        window.__novaSelectedEl.outerHTML = newHtml;
        return 'element_updated';
    }} else {{
        document.documentElement.outerHTML = newHtml;
        return 'document_updated';
    }}
}})();";
            var res = await wv.ExecuteScriptAsync(script);
            if (DtElementsStatus != null)
                DtElementsStatus.Text = "Изменения применены к DOM (" + DateTime.Now.ToString("HH:mm:ss") + ")";
            AnimateStatusText("DOM обновлен");
        }
        catch (Exception ex)
        {
            if (DtElementsStatus != null) DtElementsStatus.Text = "Ошибка применения: " + ex.Message;
        }
    }

    private async void DtDeleteElement_Click(object sender, RoutedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        try
        {
            var script = @"(function() {
    if (window.__novaSelectedEl) {
        window.__novaSelectedEl.remove();
        window.__novaSelectedEl = null;
        return true;
    }
    return false;
})();";
            var res = await wv.ExecuteScriptAsync(script);
            if (res?.Contains("true") == true)
            {
                if (DtElementsTree != null) DtElementsTree.Text = "";
                if (DtElementsStatus != null) DtElementsStatus.Text = "Элемент удален из дерева DOM.";
                AnimateStatusText("Элемент удален");
            }
            else
            {
                if (DtElementsStatus != null) DtElementsStatus.Text = "Сначала выберите элемент для удаления.";
            }
        }
        catch (Exception ex)
        {
            if (DtElementsStatus != null) DtElementsStatus.Text = "Ошибка: " + ex.Message;
        }
    }

    private async void DtToggleVisibility_Click(object sender, RoutedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        try
        {
            var script = @"(function() {
    if (window.__novaSelectedEl) {
        var cur = window.__novaSelectedEl.style.display;
        if (cur === 'none') {
            window.__novaSelectedEl.style.display = window.__novaPrevDisplay || '';
            return 'visible';
        } else {
            window.__novaPrevDisplay = cur;
            window.__novaSelectedEl.style.display = 'none';
            return 'hidden';
        }
    }
    return 'none';
})();";
            var res = await wv.ExecuteScriptAsync(script);
            if (res?.Contains("hidden") == true)
            {
                if (DtElementsStatus != null) DtElementsStatus.Text = "Элемент скрыт (display: none).";
                AnimateStatusText("Элемент скрыт");
            }
            else if (res?.Contains("visible") == true)
            {
                if (DtElementsStatus != null) DtElementsStatus.Text = "Элемент отображен.";
                AnimateStatusText("Элемент отображен");
            }
            else
            {
                if (DtElementsStatus != null) DtElementsStatus.Text = "Сначала выберите элемент.";
            }
        }
        catch (Exception ex)
        {
            if (DtElementsStatus != null) DtElementsStatus.Text = "Ошибка: " + ex.Message;
        }
    }

    private async void DtFindSelector_Click(object sender, RoutedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        var sel = DtElementSelectorInput?.Text.Trim();
        if (string.IsNullOrEmpty(sel)) return;
        try
        {
            var jsonSel = System.Text.Json.JsonSerializer.Serialize(sel);
            var script = $@"(function() {{
    try {{
        var el = document.querySelector({jsonSel});
        if (el) {{
            window.__novaSelectedEl = el;
            el.scrollIntoView({{ behavior: 'smooth', block: 'center' }});
            return JSON.stringify({{ ok: true, html: el.outerHTML }});
        }}
        return JSON.stringify({{ ok: false, msg: 'Элемент не найден' }});
    }} catch(e) {{
        return JSON.stringify({{ ok: false, msg: e.message }});
    }}
}})();";
            var raw = await wv.ExecuteScriptAsync(script);
            var parsed = System.Text.Json.JsonSerializer.Deserialize<string>(raw ?? "") ?? "";
            using var doc = System.Text.Json.JsonDocument.Parse(parsed);
            if (doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean())
            {
                var html = doc.RootElement.GetProperty("html").GetString();
                if (DtElementsTree != null) DtElementsTree.Text = html ?? "";
                if (DtElementsStatus != null) DtElementsStatus.Text = $"Найден элемент по селектору '{sel}'";
            }
            else
            {
                var msg = doc.RootElement.TryGetProperty("msg", out var mp) ? mp.GetString() : "Не найден";
                if (DtElementsStatus != null) DtElementsStatus.Text = $"Селектор '{sel}': {msg}";
            }
        }
        catch (Exception ex)
        {
            if (DtElementsStatus != null) DtElementsStatus.Text = "Ошибка поиска: " + ex.Message;
        }
    }

    private void DtElementSelectorInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DtFindSelector_Click(sender, e);
            e.Handled = true;
        }
    }

    private void DtCopyElements_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(DtElementsTree?.Text))
        {
            Clipboard.SetText(DtElementsTree.Text);
            AnimateStatusText("Разметка DOM скопирована в буфер");
        }
    }

    private void DtClearNetwork_Click(object sender, RoutedEventArgs e)
    {
        DtNetworkList?.Children.Clear();
    }

    private async void DtRefreshStorage_Click(object sender, RoutedEventArgs e)
    {
        if (DtStorageContent == null) return;
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) { DtStorageContent.Text = "Нет данных."; return; }
        try
        {
            var js = @"(function(){
                var ls = {};
                for (var i = 0; i < localStorage.length; i++) {
                    var k = localStorage.key(i);
                    ls[k] = localStorage.getItem(k);
                }
                return JSON.stringify({ localStorage: ls, cookie: document.cookie }, null, 2);
            })()";
            var raw = await wv.ExecuteScriptAsync(js);
            var json = System.Text.Json.JsonSerializer.Deserialize<string>(raw ?? "") ?? "";
            DtStorageContent.Text = json;
        }
        catch (Exception ex)
        {
            DtStorageContent.Text = "Ошибка чтения хранилища: " + ex.Message;
        }
    }

    private async void DtClearStorage_Click(object sender, RoutedEventArgs e)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        try
        {
            await wv.ExecuteScriptAsync("localStorage.clear(); sessionStorage.clear();");
            DtRefreshStorage_Click(sender, e);
            AnimateStatusText("Хранилище очищено");
        }
        catch { }
    }

    private void UpdateDevToolsSecurityView()
    {
        if (DtSecurityContent == null) return;
        DtSecurityContent.Children.Clear();
        var isHttps = _activeTab?.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true;
        var title = new TextBlock
        {
            Text = isHttps ? "Подключение защищено (HTTPS / TLS)" : "Незащищенное подключение",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = isHttps ? (Brush)FindResource("AccentGreenBrush") : (Brush)FindResource("AccentOrangeBrush"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        DtSecurityContent.Children.Add(title);
        var desc = new TextBlock
        {
            Text = $"Адрес: {_activeTab?.Url}\nЗащита от рекламы: включена\nЗащита от фишинга и стилеров: активна\nИзоляция сайта: {(_settings.EnableSiteStealthIsolation ? "включена" : "выключена")}",
            Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            FontSize = 12,
            LineHeight = 20
        };
        DtSecurityContent.Children.Add(desc);
    }
    private async void TogglePictureInPicture()
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        try
        {
            await wv.ExecuteScriptAsync(@"
(function(){
    var videos = document.querySelectorAll('video');
    var v = Array.from(videos).find(v => !v.paused && v.readyState > 1);
    if (!v) v = videos[0];
    if (!v) { console.log('Nova: no video found'); return; }
    if (document.pictureInPictureElement)
        document.exitPictureInPicture().catch(()=>{});
    else
        v.requestPictureInPicture().catch(e => console.log('Nova PiP:', e));
})();");
            AnimateStatusText("Picture-in-Picture включён");
        }
        catch { AnimateStatusText("PiP не поддерживается на этой странице"); }
    }
    private async void SmartZoomAtPoint(double x, double y)
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        try
        {
            var result = await wv.ExecuteScriptAsync($@"
(function(){{
    var el = document.elementFromPoint({x}, {y});
    while (el && el !== document.body) {{
        var style = window.getComputedStyle(el);
        var w = el.offsetWidth;
        if (w > 200 && (el.tagName === 'ARTICLE' || el.tagName === 'MAIN' ||
            el.tagName === 'SECTION' || el.className.includes('content') ||
            el.className.includes('article'))) {{
            var rect = el.getBoundingClientRect();
            return JSON.stringify({{x: rect.left, w: rect.width}});
        }}
        el = el.parentElement;
    }}
    return null;
}})();");
            if (result != "null" && result != null)
            {
                ZoomActiveTab(+25);
                AnimateStatusText("Умный зум ");
            }
        }
        catch { }
    }
    private bool _compactTabsEnabled;
    private void ToggleCompactTabs()
    {
        _compactTabsEnabled = !_compactTabsEnabled;
        foreach (UIElement child in TabsPanel.Children)
        {
            if (child is Border b && b.Tag is BrowserTab t && !t.IsPinned)
            {
                b.MaxWidth = _compactTabsEnabled ? 120 : 220;
                b.MinWidth = _compactTabsEnabled ? 40  : 80;
            }
        }
        AnimateStatusText(_compactTabsEnabled ? "Компактные вкладки " : "Стандартные вкладки ");
    }
    private async void ScrollToTop()
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv != null)
            await wv.ExecuteScriptAsync("window.scrollTo({top:0,behavior:'smooth'});");
    }
    private async void ScrollToBottom()
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv != null)
            await wv.ExecuteScriptAsync("window.scrollTo({top:document.body.scrollHeight,behavior:'smooth'});");
    }
    private async void CopyPlainText()
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        try
        {
            var text = await wv.ExecuteScriptAsync(
                "window.getSelection().toString()");
            text = text?.Trim('"').Replace("\\n", "\n").Replace("\\t", "\t");
            if (!string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
                AnimateStatusText("Скопировано без форматирования ");
            }
        }
        catch { }
    }
    private async void ShowPageInfo()
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        try
        {
            var info = await wv.ExecuteScriptAsync(@"
JSON.stringify({
    title: document.title,
    url: location.href,
    charset: document.characterSet,
    size: document.documentElement.innerHTML.length,
    images: document.images.length,
    links: document.links.length,
    scripts: document.scripts.length
})");
            AnimateStatusText($"Страница: {_activeTab?.Title}  |  {_activeTab?.Url}");
        }
        catch { }
    }
    private void OpenTabSearch()
    {
        TabSearchOverlay.Visibility = Visibility.Visible;
        TabSearchBox.Text = "";
        TabSearchBox.Focus();
        RebuildTabSearchResults("");
    }
    private void CloseTabSearch_Click(object sender, RoutedEventArgs e)
        => TabSearchOverlay.Visibility = Visibility.Collapsed;
    private void TabSearch_TextChanged(object sender, TextChangedEventArgs e)
        => RebuildTabSearchResults(TabSearchBox.Text.Trim());
    private void TabSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { TabSearchOverlay.Visibility = Visibility.Collapsed; e.Handled = true; }
        if (e.Key == Key.Enter)
        {
            if (TabSearchResults.Children.Count > 0 &&
                TabSearchResults.Children[0] is Border b)
                b.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    { RoutedEvent = UIElement.MouseLeftButtonUpEvent });
            e.Handled = true;
        }
    }
    private void RebuildTabSearchResults(string query)
    {
        TabSearchResults.Children.Clear();
        var matches = string.IsNullOrEmpty(query)
            ? _tabs
            : _tabs.Where(t =>
                t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Url.Contains(query, StringComparison.OrdinalIgnoreCase));
        foreach (var tab in matches)
        {
            var row = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = Cursors.Hand,
                Background = tab == _activeTab
                    ? new SolidColorBrush(Color.FromArgb(42, (Application.Current?.Resources["AccentColor"] is Color _ac4 ? _ac4.R : (byte)0), (Application.Current?.Resources["AccentColor"] is Color _ac5 ? _ac5.G : (byte)212), (Application.Current?.Resources["AccentColor"] is Color _ac6 ? _ac6.B : (byte)255)))
                    : Brushes.Transparent
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            if (tab.FaviconImage != null)
            {
                var img = new Image { Source = tab.FaviconImage, Width = 16, Height = 16,
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                sp.Children.Add(img);
            }
            else
            {
                sp.Children.Add(new TextBlock
                {
                    Text = tab.IsSecure ? "\uE72E" : "\uE774",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0),
                    Foreground = (Brush)FindResource("TextMutedBrush")
                });
            }
            var info = new StackPanel();
            info.Children.Add(new TextBlock
            {
                Text = tab.Title.Length > 50 ? tab.Title[..49] + "…" : tab.Title,
                FontSize = 13, FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("TextPrimaryBrush")
            });
            info.Children.Add(new TextBlock
            {
                Text = tab.Url.Length > 60 ? tab.Url[..59] + "…" : tab.Url,
                FontSize = 11, Foreground = (Brush)FindResource("TextMutedBrush")
            });
            sp.Children.Add(info);
            row.Child = sp;
            var capturedTab = tab;
            row.MouseLeftButtonUp += (_, _) =>
            {
                TabSearchOverlay.Visibility = Visibility.Collapsed;
                SwitchToTab(capturedTab);
            };
            row.MouseEnter += (_, _) => row.Background = (Brush)FindResource("BgHoverBrush");
            row.MouseLeave += (_, _) => row.Background = capturedTab == _activeTab
                ? new SolidColorBrush(Color.FromArgb(42, (Application.Current?.Resources["AccentColor"] is Color _ac4 ? _ac4.R : (byte)0), (Application.Current?.Resources["AccentColor"] is Color _ac5 ? _ac5.G : (byte)212), (Application.Current?.Resources["AccentColor"] is Color _ac6 ? _ac6.B : (byte)255))) : Brushes.Transparent;
            TabSearchResults.Children.Add(row);
        }
        if (!TabSearchResults.Children.OfType<UIElement>().Any())
            TabSearchResults.Children.Add(new TextBlock
            {
                Text = "Вкладки не найдены", Foreground = (Brush)FindResource("TextMutedBrush"),
                FontSize = 13, Margin = new Thickness(12, 8, 12, 8)
            });
    }
    private static readonly (string icon, string label, string action)[] _commands =
    {
        ("",        "Приватная вкладка",          "private"),
        ("",        "Режим чтения",               "reader"),
        ("",        "Picture-in-Picture (видео)",  "pip"),
        ("\uE728",  "Добавить закладку",           "bookmark"),
        ("",        "История",                    "history"),
        ("\uE728",  "Закладки",                   "bookmarks"),
        ("\uE896",  "Загрузки",                   "downloads"),
        ("",        "Найти на странице",           "find"),
        ("\uE749",  "Печать",                    "print"),
        ("",        "Скриншот",                  "screenshot"),
        ("\uE713",  "Настройки",                 "settings"),
        ("\uE72C",  "Обновить страницу",          "reload"),
        ("\uE80F",  "Домашняя страница",          "home"),
        ("",        "Увеличить",                 "zoomin"),
        ("",        "Уменьшить",                 "zoomout"),
        ("",        "Сбросить масштаб",          "zoomreset"),
        ("",        "Закрепить вкладку",          "pin"),
        ("\uE74F",  "Замутить вкладку",           "mute"),
        ("\uE8C8",  "Копировать URL",             "copyurl"),
        ("\uE8C8",  "Копировать текст (без форм.)","copytext"),
        ("",        "Открыть в новой вкладке",    "newtab"),
        ("\uE740",  "На весь экран",             "fullscreen"),
        ("",        "Прокрутить вверх",          "scrolltop"),
        ("",        "Прокрутить вниз",           "scrollbottom"),
        ("",        "Инфо о странице",            "pageinfo"),
        ("",        "Компактные вкладки",        "compact"),
        ("",        "DevTools",                  "devtools"),
    };
    private void OpenCommandPalette()
    {
        CommandPalette.Visibility = Visibility.Visible;
        CommandBox.Text = "";
        CommandBox.Focus();
        RebuildCommandResults("");
    }
    private void CloseCommandPalette_Click(object sender, RoutedEventArgs e)
        => CommandPalette.Visibility = Visibility.Collapsed;
    private void Command_TextChanged(object sender, TextChangedEventArgs e)
        => RebuildCommandResults(CommandBox.Text.Trim());
    private void Command_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { CommandPalette.Visibility = Visibility.Collapsed; e.Handled = true; }
        if (e.Key == Key.Enter)
        {
            if (CommandResults.Children.Count > 0 &&
                CommandResults.Children[0] is Border b)
                b.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    { RoutedEvent = UIElement.MouseLeftButtonUpEvent });
            e.Handled = true;
        }
    }
    private void RebuildCommandResults(string q)
    {
        CommandResults.Children.Clear();
        var cmds = string.IsNullOrEmpty(q)
            ? _commands
            : _commands.Where(c => c.label.Contains(q, StringComparison.OrdinalIgnoreCase));
        foreach (var (icon, label, action) in cmds)
        {
            var row = BuildCommandRow(icon, label, "", () =>
            {
                CommandPalette.Visibility = Visibility.Collapsed;
                ExecuteCommand(action);
            });
            CommandResults.Children.Add(row);
        }
        if (!string.IsNullOrEmpty(q))
        {
            var histHits = _settings.History
                .Where(h => h.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            h.Url.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Take(5);
            foreach (var h in histHits)
            {
                var url = h.Url;
                var row = BuildCommandRow("", h.Title, h.Url, () =>
                {
                    CommandPalette.Visibility = Visibility.Collapsed;
                    NavigateTo(url);
                });
                CommandResults.Children.Add(row);
            }
            var bmHits = _settings.Bookmarks
                .Where(b => b.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            b.Url.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Take(5);
            foreach (var bm in bmHits)
            {
                var url = bm.Url;
                var row = BuildCommandRow("\uE728", bm.Title, bm.Url, () =>
                {
                    CommandPalette.Visibility = Visibility.Collapsed;
                    NavigateTo(url);
                });
                CommandResults.Children.Add(row);
            }
        }
    }
    private Border BuildCommandRow(string icon, string label, string sub, Action onClick)
    {
        var row = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 1, 0, 1),
            Cursor = Cursors.Hand,
            Background = Brushes.Transparent
        };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = icon, FontSize = 14, Width = 26,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        var info = new StackPanel();
        info.Children.Add(new TextBlock
        {
            Text = label, FontSize = 13,
            Foreground = (Brush)FindResource("TextPrimaryBrush")
        });
        if (!string.IsNullOrEmpty(sub))
            info.Children.Add(new TextBlock
            {
                Text = sub.Length > 55 ? sub[..54] + "…" : sub,
                FontSize = 10, Foreground = (Brush)FindResource("TextMutedBrush")
            });
        sp.Children.Add(info);
        row.Child = sp;
        row.MouseLeftButtonUp += (_, _) => onClick();
        row.MouseEnter += (_, _) => row.Background = (Brush)FindResource("BgHoverBrush");
        row.MouseLeave += (_, _) => row.Background = Brushes.Transparent;
        return row;
    }
    private void ExecuteCommand(string action)
    {
        switch (action)
        {
            case "private":    CreatePrivateTab(); break;
            case "reader":     ToggleReaderMode(); break;
            case "bookmark":   AddBookmark_Click(this, new RoutedEventArgs()); break;
            case "history":    ShowHistoryTab(); break;
            case "bookmarks":  ShowBookmarksTab(); break;
            case "find":       ToggleFind(); break;
            case "print":      PrintPage(); break;
            case "screenshot": TakeScreenshot(); break;
            case "settings":   ShowSettingsTab(); break;
            case "reload":     Refresh_Click(this, new RoutedEventArgs()); break;
            case "home":       Home_Click(this, new RoutedEventArgs()); break;
            case "zoomin":     ZoomActiveTab(+10); break;
            case "zoomout":    ZoomActiveTab(-10); break;
            case "zoomreset":  ZoomActiveTab(0); break;
            case "pin":        if (_activeTab != null) PinTab(_activeTab); break;
            case "mute":       Mute_Click(this, new RoutedEventArgs()); break;
            case "copyurl":    CopyUrl(); break;
            case "copytext":   CopyPlainText(); break;
            case "newtab":     CreateNewTab(); break;
            case "fullscreen": ToggleFullscreen(); break;
            case "scrolltop":  ScrollToTop(); break;
            case "scrollbottom": ScrollToBottom(); break;
            case "pageinfo":   ShowPageInfo(); break;
            case "compact":    ToggleCompactTabs(); break;
            case "pip":        TogglePictureInPicture(); break;
            case "devtools":   ToggleDevTools(); break;
        }
    }
    private void TabSearchOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == TabSearchOverlay)
            TabSearchOverlay.Visibility = Visibility.Collapsed;
    }
    private void CommandPalette_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == CommandPalette)
            CommandPalette.Visibility = Visibility.Collapsed;
    }
    private string? _customThemeWallpaperPath;
    private string? _liveWallpaperPath;
    private static readonly Dictionary<string, string[]> _presets = new()
    {
        ["coal"]   = ["#0E0E0E","#1A1A1A","#222222","#2A2A2A","#333333","#EEEEEE","#FF6B35"],
        ["ocean"]  = ["#020B18","#041630","#072040","#0A2A50","#1A4080","#C8E8FF","#00C8FF"],
        ["forest"] = ["#0A1208","#131F10","#1A2B15","#22361B","#2E4A23","#D4F0C8","#5CC84A"],
        ["sakura"] = ["#120808","#1F0F0F","#2B1515","#361B1B","#502828","#FFE4E8","#FF6B8A"],
        ["neon"]   = ["#050008","#0A000F","#100018","#180025","#280040","#E8C8FF","#CC00FF"],
        ["coffee"] = ["#100A06","#1C1208","#261A0C","#33220F","#4A3318","#F0E0D0","#C87A3A"],
    };
    private void CustomPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string key) return;
        if (!_presets.TryGetValue(key, out var p)) return;
        if (CustomBgBox     != null) CustomBgBox.Text     = p[0];
        if (CustomCardBox   != null) CustomCardBox.Text   = p[1];
        if (CustomSurfaceBox!= null) CustomSurfaceBox.Text= p[2];
        if (CustomBorderBox != null) CustomBorderBox.Text = p[4];
        if (CustomTextBox   != null) CustomTextBox.Text   = p[5];
        if (CustomAccentBox != null) CustomAccentBox.Text = p[6];
    }
    private static readonly (string, string, string)[] _wpPresets =
    {
        ("Звёзды",   "#0A0A1A", "#1A0A2E"),
        ("Океан",    "#001828", "#003850"),
        ("Галактика","#1A0030", "#4B0082"),
        ("Лес",      "#0A1A0A", "#1A3A1A"),
        ("Закат",    "#1A0808", "#3A1010"),
        ("Матрица",  "#080808", "#001A00"),
    };
    private void WallpaperPreset_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border b || b.Tag is not string tagStr) return;
        if (!int.TryParse(tagStr, out int idx) || idx < 0 || idx >= _wpPresets.Length) return;
        _customThemeWallpaperPath = $"preset:{idx}";
        if (CustomWpPathLabel != null)
            CustomWpPathLabel.Text = $"Пресет: {_wpPresets[idx].Item1}";
        var previews = new[] { WpPreview0, WpPreview1, WpPreview2, WpPreview3, WpPreview4, WpPreview5 };
        for (int i = 0; i < previews.Length; i++)
        {
            if (previews[i] == null) continue;
            previews[i]!.BorderThickness = new System.Windows.Thickness(i == idx ? 2 : 1);
            previews[i]!.Opacity = i == idx ? 1.0 : 0.55;
            try { previews[i]!.BorderBrush = i == idx
                ? (Application.Current?.Resources["AccentBrush"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Cyan)
                : (Application.Current?.Resources["BorderBrush"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Gray); }
            catch { }
        }
    }
    private void CustomWpChooseFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Выберите обои",
            Filter = "Все медиа (*.jpg;*.png;*.webp;*.gif;*.mp4;*.webm;*.mov)|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif;*.mp4;*.webm;*.mov|Изображения и GIF (*.jpg;*.png;*.webp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif|Видео (*.mp4;*.webm;*.mov)|*.mp4;*.webm;*.mov;*.avi;*.mkv|Все файлы|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        _customThemeWallpaperPath = dlg.FileName;
        _liveWallpaperPath = null;
        if (LiveWpPathLabel != null) LiveWpPathLabel.Text = "Видео не выбрано";
        if (CustomWpPathLabel != null)
            CustomWpPathLabel.Text = System.IO.Path.GetFileName(dlg.FileName);
        _settings.CustomWallpaperPath   = dlg.FileName;
        _settings.ShowHomepageWallpaper = true;
        _settings.LiveWallpaperPath     = "";
        _wallpaperCache      = null;
        _wallpaperCachedPath = "";
        SaveSettings();
        LoadWallpaper();
        var previews = new[] { WpPreview0, WpPreview1, WpPreview2, WpPreview3, WpPreview4, WpPreview5 };
        foreach (var p in previews)
        {
            if (p == null) continue;
            p.BorderThickness = new System.Windows.Thickness(1);
            p.Opacity = 0.55;
            try { p.BorderBrush = Application.Current?.Resources["BorderBrush"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Gray; } catch { }
        }
        AnimateStatusText("Обои установлены ");
    }
    private void CustomWpClear_Click(object sender, RoutedEventArgs e)
    {
        _customThemeWallpaperPath = null;
        _settings.ShowHomepageWallpaper = false;
        _settings.CustomWallpaperPath   = "";
        _settings.WallpaperPresetIndex  = -1;
        _wallpaperCache      = null;
        _wallpaperCachedPath = "";
        SaveSettings();
        LoadWallpaper();
        if (CustomWpPathLabel != null)
            CustomWpPathLabel.Text = "Файл не выбран";
        var previews = new[] { WpPreview0, WpPreview1, WpPreview2, WpPreview3, WpPreview4, WpPreview5 };
        foreach (var p in previews)
        {
            if (p == null) continue;
            p.BorderThickness = new System.Windows.Thickness(1);
            p.Opacity = 0.55;
            try { p.BorderBrush = Application.Current?.Resources["BorderBrush"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Gray; } catch { }
        }
        AnimateStatusText("Обои убраны");
    }
    private void LiveWpChooseFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Выберите видео для живых обоев",
            Filter = "Видео|*.mp4;*.webm;*.avi;*.mov;*.mkv|Все файлы|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        _liveWallpaperPath = dlg.FileName;
        if (LiveWpPathLabel != null)
            LiveWpPathLabel.Text = System.IO.Path.GetFileName(dlg.FileName);
        _customThemeWallpaperPath = null;
        if (CustomWpPathLabel != null) CustomWpPathLabel.Text = "Файл не выбран";
        var previews = new[] { WpPreview0, WpPreview1, WpPreview2, WpPreview3, WpPreview4, WpPreview5 };
        foreach (var p in previews)
        {
            if (p == null) continue;
            p.BorderThickness = new System.Windows.Thickness(1);
            p.Opacity = 0.55;
        }
    }
    private void LiveWpClear_Click(object sender, RoutedEventArgs e)
    {
        _liveWallpaperPath = null;
        if (LiveWpPathLabel != null)
            LiveWpPathLabel.Text = "Видео не выбрано";
        StopLiveWallpaper();
        _settings.LiveWallpaperPath = "";
        SaveSettings();
    }
    private void StartLiveWallpaper(string path)
    {
        try
        {
            if (LiveWallpaperVideo == null || !System.IO.File.Exists(path)) return;
            LiveWallpaperVideo.Source   = new Uri(path);
            LiveWallpaperVideo.IsMuted  = true;
            LiveWallpaperVideo.Visibility = Visibility.Visible;
            LiveWallpaperVideo.Play();
            WallpaperImage.Visibility = Visibility.Collapsed;
        }
        catch { }
    }
    private void StopLiveWallpaper()
    {
        try
        {
            if (LiveWallpaperVideo == null) return;
            LiveWallpaperVideo.Stop();
            LiveWallpaperVideo.Source     = null!;
            LiveWallpaperVideo.Visibility = Visibility.Collapsed;
        }
        catch { }
    }
    private void LiveWallpaper_MediaEnded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (LiveWallpaperVideo?.Source == null) return;
            var fadeOut = new DoubleAnimation(1.0, 0.85, TimeSpan.FromMilliseconds(80));
            LiveWallpaperVideo.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            LiveWallpaperVideo.Position = TimeSpan.FromMilliseconds(1);
            LiveWallpaperVideo.Play();
            var fadeIn = new DoubleAnimation(0.85, 1.0, TimeSpan.FromMilliseconds(220))
            {
                BeginTime = TimeSpan.FromMilliseconds(60)
            };
            LiveWallpaperVideo.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
        catch { }
    }
    private void CustomTheme_ColorChanged(object sender, TextChangedEventArgs e)
    {
        TrySetBorderBg(CustomBgBox?.Text,        CustomBgPreview);
        TrySetBorderBg(CustomCardBox?.Text,      CustomCardPreview);
        TrySetBorderBg(CustomSurfaceBox?.Text,   CustomSurfacePreview);
        TrySetBorderBg(CustomAccentBox?.Text,    CustomAccentPreview);
        TrySetBorderBg(CustomTextBox?.Text,      CustomTextPreview);
        TrySetBorderBg(CustomBorderBox?.Text,    CustomBorderPreview);
        TrySetBorderBg(CustomNavBarBox?.Text,    CustomNavBarBoxPreview);
        TrySetBorderBg(CustomHoverBox?.Text,     CustomHoverBoxPreview);
        TrySetBorderBg(CustomTabActiveBox?.Text, CustomTabActiveBoxPreview);
        TrySetBorderBg(CustomMutedTextBox?.Text, CustomMutedTextBoxPreview);
        TrySetBorderBg(CustomGreenBox?.Text,     CustomGreenBoxPreview);
        TrySetBorderBg(CustomRedBox?.Text,       CustomRedBoxPreview);
        UpdateCustomPreviewBar();
    }
    private static void TrySetBorderBg(string? hex, Border? b)
    {
        if (b == null || string.IsNullOrEmpty(hex)) return;
        if (!hex.StartsWith("#") || hex.Length < 4) return;
        try { b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); } catch { }
    }
    private static bool TryParseColor(string? hex, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length < 4) return false;
        try { color = (Color)ColorConverter.ConvertFromString(hex); return true; }
        catch { return false; }
    }
    private void UpdateCustomPreviewBar()
    {
        if (CustomThemePreviewBar == null) return;
        if (TryParseColor(CustomBgBox?.Text, out var bg))
            CustomThemePreviewBar.Background = new SolidColorBrush(bg);
        if (PrevTabBorder != null && TryParseColor(CustomCardBox?.Text, out var card))
            PrevTabBorder.Background = new SolidColorBrush(card);
        if (PrevSurfaceBorder != null && TryParseColor(CustomSurfaceBox?.Text, out var surface))
            PrevSurfaceBorder.Background = new SolidColorBrush(surface);
        if (PrevTabText != null && TryParseColor(CustomTextBox?.Text, out var textColor))
            PrevTabText.Foreground = new SolidColorBrush(textColor);
        if (PrevAccentBorder != null && PrevAccentText != null && TryParseColor(CustomAccentBox?.Text, out var accent))
        {
            PrevAccentBorder.Background = new SolidColorBrush(accent);
            double lum = (accent.R * 0.299 + accent.G * 0.587 + accent.B * 0.114) / 255.0;
            PrevAccentText.Foreground = new SolidColorBrush(lum > 0.55 ? Colors.Black : Colors.White);
        }
        if (PrevSurfaceBorder != null && TryParseColor(CustomBorderBox?.Text, out var borderColor))
            PrevSurfaceBorder.BorderBrush = new SolidColorBrush(borderColor);
    }
    private void ApplyCustomTheme_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string Sanitize(string? val, string fallback)
            {
                var v = (val ?? "").Trim();
                if (!v.StartsWith("#")) v = "#" + v;
                if (v.Length > 1 && v.Length < 7) v = "#" + v[1..].PadRight(6, '0');
                return IsValidHex(v) ? v.ToUpperInvariant() : fallback;
            }
            var bg        = Sanitize(CustomBgBox?.Text,        "#0C0C0C");
            var card      = Sanitize(CustomCardBox?.Text,      "#141414");
            var surface   = Sanitize(CustomSurfaceBox?.Text,   "#1A1A1A");
            var border    = Sanitize(CustomBorderBox?.Text,    "#333333");
            var text      = Sanitize(CustomTextBox?.Text,      "#F0F0F0");
            var accent    = Sanitize(CustomAccentBox?.Text,    "#BEF47B");
            var navbar    = Sanitize(CustomNavBarBox?.Text,    bg);
            var hover     = Sanitize(CustomHoverBox?.Text,     card);
            var tabactive = Sanitize(CustomTabActiveBox?.Text, surface);
            var mutedtext = Sanitize(CustomMutedTextBox?.Text, "#909090");
            var green     = Sanitize(CustomGreenBox?.Text,     "#10B981");
            var red       = Sanitize(CustomRedBox?.Text,       "#EF4444");
            var hoverColor    = DarkenOrLighten(card, 0.15);
            var titleBarColor = DarkenOrLighten(bg, -0.05);
            var mutedColor    = BlendHex(text, bg, 0.3);
            var textSec       = BlendHex(text, bg, 0.65);
            var popup         = DarkenOrLighten(card, 0.05);
            var allColors = new[] { bg, card, surface, border, text, accent,
                hoverColor, titleBarColor, mutedColor, textSec, popup };
            foreach (var col in allColors)
                if (!IsValidHex(col))
                {
                    AnimateStatusText($"Некорректный цвет: {col}");
                    return;
                }
            var themeDict = Application.Current.Resources.MergedDictionaries.Count > 0
                ? Application.Current.Resources.MergedDictionaries[0]
                : Application.Current.Resources;
            void SetBrush(string key, string hex)
            {
                try
                {
                    var col = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                    var brush = new System.Windows.Media.SolidColorBrush(col);
                    if (themeDict.Contains(key)) themeDict.Remove(key);
                    themeDict.Add(key, brush);
                }
                catch { }
            }
            void SetColor(string key, string hex)
            {
                try
                {
                    var col = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                    if (themeDict.Contains(key)) themeDict.Remove(key);
                    themeDict.Add(key, col);
                }
                catch { }
            }
            SetColor("AccentColor",        accent);
            SetBrush("AccentBrush",        accent);
            SetBrush("BgDarkBrush",        bg);
            SetBrush("BgCardBrush",        card);
            SetBrush("BgSurfaceBrush",     surface);
            SetBrush("BgHoverBrush",       hover);
            SetBrush("TitleBarBrush",      navbar);
            SetBrush("BookmarksBarBrush",  navbar);
            SetBrush("PopupBrush",         surface);
            SetBrush("BorderBrush",        border);
            SetBrush("TextPrimaryBrush",   text);
            SetBrush("TextSecondaryBrush", mutedtext);
            SetBrush("TextMutedBrush",     mutedtext);
            SetBrush("AccentGreenBrush",   green);
            SetBrush("AccentOrangeBrush",  "#F59E0B");
            SetBrush("AccentRedBrush",     red);
            _settings.AccentColor = accent;
            ApplyAccentColor(accent);
            _settings.Theme = "Custom";
            if (!string.IsNullOrEmpty(_customThemeWallpaperPath))
            {
                if (_customThemeWallpaperPath.StartsWith("preset:") &&
                    int.TryParse(_customThemeWallpaperPath["preset:".Length..], out int presetIdx) &&
                    presetIdx >= 0 && presetIdx < _wpPresets.Length)
                {
                    _settings.ShowHomepageWallpaper = false;
                    _settings.WallpaperPresetIndex  = presetIdx;
                    var (_, c1, c2) = _wpPresets[presetIdx];
                    _themeBlobs["Custom"] = (c1, c2,
                        BlendHex(c1, c2, 0.5),
                        BlendHex(c2, accent, 0.4));
                }
                else if (System.IO.File.Exists(_customThemeWallpaperPath))
                {
                    _settings.CustomWallpaperPath   = _customThemeWallpaperPath;
                    _settings.ShowHomepageWallpaper = true;
                    _settings.WallpaperPresetIndex  = -1;
                    LoadWallpaper();
                }
            }
            if (!string.IsNullOrEmpty(_liveWallpaperPath) && System.IO.File.Exists(_liveWallpaperPath))
            {
                _settings.LiveWallpaperPath     = _liveWallpaperPath;
                _settings.ShowHomepageWallpaper = false;
                _settings.WallpaperPresetIndex  = -1;
                StartLiveWallpaper(_liveWallpaperPath);
            }
            else if (string.IsNullOrEmpty(_liveWallpaperPath))
            {
                _settings.LiveWallpaperPath = "";
                StopLiveWallpaper();
            }
            SaveSettings();
            UpdateThemeCards();
            ApplyThemeWallpaper("Custom");
            ApplyHomePageCustomization();
            AnimateStatusText("Кастомная тема применена ");
        }
        catch (Exception ex)
        {
            AnimateStatusText($"Ошибка темы: {ex.Message}");
        }
    }
    private static bool IsValidCustomThemeFile(string path)
    {
        try
        {
            var content = System.IO.File.ReadAllText(path);
            var matches = System.Text.RegularExpressions.Regex.Matches(
                content, @"Color=""(#[0-9A-Fa-f]+)""");
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var hex = m.Groups[1].Value;
                if (hex.Length != 7 && hex.Length != 9) return false;
                try { ColorConverter.ConvertFromString(hex); }
                catch { return false; }
            }
            return matches.Count > 0;
        }
        catch { return false; }
    }
    private static bool IsValidHex(string? hex)
        => !string.IsNullOrEmpty(hex)
           && hex.StartsWith("#")
           && (hex.Length == 7 || hex.Length == 9)
           && hex.Skip(1).All(c => "0123456789ABCDEFabcdef".Contains(c));
    private static string DarkenOrLighten(string hex, double amount)
    {
        try
        {
            var clean = hex.TrimStart('#');
            if (clean.Length != 6) return hex;
            var c = (Color)ColorConverter.ConvertFromString("#" + clean);
            double brightness = (c.R * 0.299 + c.G * 0.587 + c.B * 0.114) / 255.0;
            double factor = brightness < 0.5 ? 1.0 + Math.Abs(amount) : 1.0 - Math.Abs(amount);
            int r = (int)Math.Clamp(Math.Round(c.R * factor), 0, 255);
            int g = (int)Math.Clamp(Math.Round(c.G * factor), 0, 255);
            int b = (int)Math.Clamp(Math.Round(c.B * factor), 0, 255);
            return "#" + r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }
        catch { return hex; }
    }
    private static string BlendHex(string hex1, string hex2, double t)
    {
        try
        {
            var c1 = hex1.TrimStart('#');
            var c2 = hex2.TrimStart('#');
            if (c1.Length != 6 || c2.Length != 6) return hex1;
            var a = (Color)ColorConverter.ConvertFromString("#" + c1);
            var b = (Color)ColorConverter.ConvertFromString("#" + c2);
            t = Math.Clamp(t, 0.0, 1.0);
            int r = (int)Math.Round(a.R * t + b.R * (1 - t));
            int g = (int)Math.Round(a.G * t + b.G * (1 - t));
            int bl= (int)Math.Round(a.B * t + b.B * (1 - t));
            return "#" + r.ToString("X2") + g.ToString("X2") + bl.ToString("X2");
        }
        catch { return hex1; }
    }
    private void ShowKeyboardShortcutsBtn_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
@"Горячие клавиши Nova Browser:
Ctrl+T           Новая вкладка
Ctrl+W           Закрыть вкладку
Ctrl+Shift+T     Восстановить закрытую вкладку
Ctrl+L / F6      Адресная строка
Ctrl+R / F5      Обновить
Ctrl+F           Поиск на странице
Ctrl+H           История
Ctrl+J           Загрузки
Ctrl+D           Закладка
Ctrl+P           Печать
Ctrl+N           Новое окно
Ctrl+Shift+N     Инкогнито
F11              Полный экран
F12              Инструменты разработчика",
            "Горячие клавиши Nova Browser",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void ApplyA11ySettings()
    {
        var wv = _activeTab?.WebViewControl?.CoreWebView2;
        if (wv == null) return;
        bool highContrast = ChkHighContrast?.IsChecked == true;
        bool forceDark    = ChkForceDarkSites?.IsChecked == true;
        if (highContrast || forceDark)
        {
            var css = highContrast
                ? "body{filter:contrast(1.5)!important}"
                : "html{filter:invert(0.9) hue-rotate(180deg)!important}img,video{filter:invert(1) hue-rotate(180deg)!important}";
            _ = wv.ExecuteScriptAsync(
                $"var s=document.getElementById('_nova_a11y');if(!s){{s=document.createElement('style');s.id='_nova_a11y';document.head.appendChild(s);}}s.textContent={System.Text.Json.JsonSerializer.Serialize(css)};");
        }
    }
    private void CustomThemeFieldPicker_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as System.Windows.Controls.Button;
        var tag = btn?.Tag?.ToString() ?? "";
        System.Windows.Controls.TextBox? target = tag switch
        {
            "bg"        => CustomBgBox,
            "card"      => CustomCardBox,
            "accent"    => CustomAccentBox,
            "text"      => CustomTextBox,
            "border"    => CustomBorderBox,
            "surface"   => CustomSurfaceBox,
            "navbar"    => CustomNavBarBox,
            "hover"     => CustomHoverBox,
            "tabactive" => CustomTabActiveBox,
            "mutedtext" => CustomMutedTextBox,
            "green"     => CustomGreenBox,
            "red"       => CustomRedBox,
            _           => null
        };
        if (target == null) return;
        var dlg = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
        };
        try
        {
            var hex = (target.Text ?? "").Trim().TrimStart('#');
            if (hex.Length == 6)
            {
                int r = Convert.ToInt32(hex[..2], 16);
                int g = Convert.ToInt32(hex[2..4], 16);
                int b = Convert.ToInt32(hex[4..6], 16);
                dlg.Color = System.Drawing.Color.FromArgb(r, g, b);
            }
        }
        catch { }
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = dlg.Color;
            target.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    private Window? _firstRunDialog;
    public void ShowFirstRunSetupModal()
    {
        try
        {
            if (_firstRunDialog != null && _firstRunDialog.IsVisible) return;

            var dlg = new Window
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                AllowsTransparency = true,
                WindowStyle = WindowStyle.None,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                Width = 540,
                SizeToContent = SizeToContent.Height
            };
            _firstRunDialog = dlg;
            dlg.Closed += (_, _) => _firstRunDialog = null;

            var card = new Border
            {
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(28),
                Background = new SolidColorBrush(Color.FromRgb(18, 22, 32)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(190, 244, 123)),
                BorderThickness = new Thickness(1)
            };
            try { card.Effect = new DropShadowEffect { BlurRadius = 32, ShadowDepth = 8, Opacity = 0.7, Color = Colors.Black }; } catch { }
            card.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) dlg.DragMove(); };

            var stack = new StackPanel();

            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var title = new TextBlock
            {
                Text = "Добро пожаловать в Nova Browser!",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(title, 0);
            headerGrid.Children.Add(title);

            var btnClose = new Button
            {
                Content = "✕",
                Width = 28,
                Height = 28,
                FontSize = 13,
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 154, 176)),
                BorderThickness = new Thickness(0)
            };
            btnClose.Click += (_, _) =>
            {
                _settings.IsFirstRun = false;
                _settings.Save();
                dlg.Close();
            };
            Grid.SetColumn(btnClose, 1);
            headerGrid.Children.Add(btnClose);
            stack.Children.Add(headerGrid);

            var sub = new TextBlock
            {
                Text = "Быстрая начальная настройка параметров для комфортной работы:",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 154, 176)),
                Margin = new Thickness(0, 0, 0, 18)
            };
            stack.Children.Add(sub);

            var lblEngine = new TextBlock { Text = "Поисковая система:", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 4) };
            stack.Children.Add(lblEngine);
            var cbEngine = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 14) };
            cbEngine.Items.Add(new ComboBoxItem { Content = "Nova", IsSelected = true });
            cbEngine.Items.Add(new ComboBoxItem { Content = "Google" });
            cbEngine.Items.Add(new ComboBoxItem { Content = "Яндекс" });
            cbEngine.Items.Add(new ComboBoxItem { Content = "DuckDuckGo" });
            cbEngine.Items.Add(new ComboBoxItem { Content = "Bing" });
            stack.Children.Add(cbEngine);

            var chkSession = new CheckBox
            {
                Content = "Восстанавливать открытые вкладки при запуске браузера",
                IsChecked = true,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(chkSession);

            var chkAds = new CheckBox
            {
                Content = "Включить блокировку рекламы и трекеров (AdBlocker)",
                IsChecked = true,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(chkAds);

            var chkStealer = new CheckBox
            {
                Content = "Защита от стиллеров",
                IsChecked = true,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 22)
            };
            stack.Children.Add(chkStealer);

            var btnStart = new Button
            {
                Content = "Начать работу в Nova Browser",
                Height = 38,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(190, 244, 123)),
                Foreground = new SolidColorBrush(Color.FromRgb(12, 20, 4)),
                BorderThickness = new Thickness(0)
            };
            btnStart.Click += (_, _) =>
            {
                if (cbEngine.SelectedItem is ComboBoxItem cbi)
                    _settings.SearchEngine = cbi.Content?.ToString() ?? "Nova";
                _settings.RestoreLastSession = chkSession.IsChecked == true;
                _settings.BlockAds = chkAds.IsChecked == true;
                _settings.BlockTrackers = chkAds.IsChecked == true;
                _settings.AntiStealerProtection = chkStealer.IsChecked == true;
                _settings.IsFirstRun = false;
                _settings.Save();

                dlg.Close();
            };
            stack.Children.Add(btnStart);

            dlg.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    _settings.IsFirstRun = false;
                    _settings.Save();
                    dlg.Close();
                }
            };

            card.Child = stack;
            dlg.Content = card;
            dlg.ShowDialog();
        }
        catch { }
    }

}
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    public RelayCommand(Action<object?> execute)
    {
        _execute = execute;
    }
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter);
}
public class SuggestItem
{
    public string Icon  { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url   { get; set; } = "";
}
