#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NovaBrowser.Helpers;
using NovaBrowser.Models;

namespace NovaBrowser;

public partial class MainWindow
{
    bool _htmlHomeBusy;
    bool _searchSettingsInjected;
    internal static Action? HtmlHomeReadyOnce;

    internal const string NovaVirtualHost = "nova.example";
    internal const string NovaHomeUrl = "https://nova.example/index.html";
    internal const string NovaPrivateUrl = "https://nova.example/private.html";
    internal static string NovaPrivateHtmlPath => System.IO.Path.Combine(NovaAssetsDir, "private.html");
    internal static bool NovaHomeAvailable => true;

    internal static string NovaAssetsDir => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NovaBrowser", "Assets", "Home");

    internal static string NovaHomeHtmlPath => System.IO.Path.Combine(NovaAssetsDir, "nova-home.html");
    internal static string NovaHomeIndexPath => System.IO.Path.Combine(NovaAssetsDir, "index.html");

    internal static bool IsNovaHost(string? uri)
    {
        if (string.IsNullOrEmpty(uri)) return false;
        if (uri.Equals("nova://settings", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("nova://settings/", StringComparison.OrdinalIgnoreCase) ||
            uri.Equals("nova://downloads", StringComparison.OrdinalIgnoreCase) ||
            uri.Equals("nova://history", StringComparison.OrdinalIgnoreCase) ||
            uri.Equals("nova://bookmarks", StringComparison.OrdinalIgnoreCase))
            return false;
        if (uri.StartsWith("data:text/html", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase))
            return true;
        if (uri.StartsWith("https://nova.example", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("http://nova.example", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("https://nova.app", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("http://nova.app", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("nova://home", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("nova://newtab", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("nova://private", StringComparison.OrdinalIgnoreCase))
            return true;
        if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
            (uri.Contains("nova-home.html", StringComparison.OrdinalIgnoreCase) ||
             uri.Contains("private.html", StringComparison.OrdinalIgnoreCase) ||
             uri.Contains("NovaBrowser", StringComparison.OrdinalIgnoreCase)))
            return true;
        return false;
    }

    internal static bool IsNovaUrl(string? uri)
    {
        if (string.IsNullOrEmpty(uri)) return true;
        if (uri.Equals("nova://settings", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("nova://settings/", StringComparison.OrdinalIgnoreCase))
            return true;
        return IsNovaHost(uri);
    }

    internal static string GetStaticWallpaperForHtml(string? choice = null)
    {
        if (string.IsNullOrWhiteSpace(choice))
        {
            try
            {
                var s = BrowserSettings.Load();
                choice = s?.HomeBackgroundImage;
            }
            catch { }
        }
        if (string.IsNullOrWhiteSpace(choice)) choice = "wallpaper-default.jpg";

        if (choice.Equals("linear", StringComparison.OrdinalIgnoreCase)) return "linear";

        if (choice.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            choice.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            choice.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return choice;
        }

        if (choice.Equals("wallpaper-default.jpg", StringComparison.OrdinalIgnoreCase))
            return "wallpaper-default.jpg";

        if (File.Exists(choice))
        {
            try
            {
                var ext = System.IO.Path.GetExtension(choice).ToLowerInvariant();
                if (ext is ".mp4" or ".webm" or ".mov" or ".m4v" or ".ogg")
                {
                    Directory.CreateDirectory(NovaAssetsDir);
                    var destVideo = System.IO.Path.Combine(NovaAssetsDir, "video-wallpaper" + ext);
                    if (!File.Exists(destVideo) || new FileInfo(destVideo).Length != new FileInfo(choice).Length)
                    {
                        File.Copy(choice, destVideo, true);
                    }
                    return "video-wallpaper" + ext;
                }
                var bytes = File.ReadAllBytes(choice);
                var mime = ext switch
                {
                    ".png"  => "image/png",
                    ".webp" => "image/webp",
                    ".gif"  => "image/gif",
                    _       => "image/jpeg"
                };
                return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }
            catch { }
        }

        return "wallpaper-default.jpg";
    }

    internal string GetWallpaperForHtml()
    {
        return GetStaticWallpaperForHtml(_settings?.HomeBackgroundImage);
    }

    internal static void ExtractNovaHome(string? accent = null, string? bg = null, string? wp = null, double? dim = null)
    {
        try
        {
            Directory.CreateDirectory(NovaAssetsDir);
            var acc = !string.IsNullOrEmpty(accent) ? accent : "#BEF47B";
            var bgc = !string.IsNullOrEmpty(bg) ? bg : "#07090d";
            var wallpaper = wp ?? GetStaticWallpaperForHtml();
            var dimming = dim ?? 0.35;

            var content = NovaHomeHtml.GetThemedContent(acc, bgc, wallpaper, dimming);
            try
            {
                if (File.Exists(NovaHomeIndexPath))
                {
                    File.SetAttributes(NovaHomeIndexPath, FileAttributes.Normal);
                    File.Delete(NovaHomeIndexPath);
                }
            }
            catch { }
            File.WriteAllText(NovaHomeIndexPath, content, System.Text.Encoding.UTF8);

            try
            {
                if (File.Exists(NovaHomeHtmlPath))
                {
                    File.SetAttributes(NovaHomeHtmlPath, FileAttributes.Normal);
                    File.Delete(NovaHomeHtmlPath);
                }
            }
            catch { }
            File.WriteAllText(NovaHomeHtmlPath, content, System.Text.Encoding.UTF8);
            EnsurePrivateHtmlOnDisk();

            var binAssetsHome = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Home");
            if (Directory.Exists(binAssetsHome))
            {
                try { File.WriteAllText(System.IO.Path.Combine(binAssetsHome, "index.html"), content, System.Text.Encoding.UTF8); } catch { }
                try { File.WriteAllText(System.IO.Path.Combine(binAssetsHome, "nova-home.html"), content, System.Text.Encoding.UTF8); } catch { }
            }

            var destWp = System.IO.Path.Combine(NovaAssetsDir, "wallpaper-default.jpg");
            var localWp = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Home", "wallpaper-default.jpg");
            var localWpRoot = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wallpaper-default.jpg");
            if (File.Exists(localWp)) File.Copy(localWp, destWp, true);
            else if (File.Exists(localWpRoot)) File.Copy(localWpRoot, destWp, true);
        }
        catch { }
    }

        internal static void EnsurePrivateHtmlOnDisk()
    {
        try
        {
            Directory.CreateDirectory(NovaAssetsDir);
            var privContent = PrivateHomeHtml.GetContent();
            try
            {
                if (File.Exists(NovaPrivateHtmlPath))
                {
                    File.SetAttributes(NovaPrivateHtmlPath, FileAttributes.Normal);
                    File.Delete(NovaPrivateHtmlPath);
                }
            }
            catch { }
            File.WriteAllText(NovaPrivateHtmlPath, privContent, System.Text.Encoding.UTF8);

            var binAssetsHome = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Home");
            if (Directory.Exists(binAssetsHome))
            {
                try { File.WriteAllText(System.IO.Path.Combine(binAssetsHome, "private.html"), privContent, System.Text.Encoding.UTF8); } catch { }
            }
        }
        catch { }
    }

    internal void MapNovaHost(CoreWebView2? core)
    {
        if (core == null) return;
        try
        {
            core.SetVirtualHostNameToFolderMapping(
                NovaVirtualHost,
                NovaAssetsDir,
                CoreWebView2HostResourceAccessKind.Allow);
        }
        catch { }

        try
        {
            core.AddWebResourceRequestedFilter("https://nova.example/*", CoreWebView2WebResourceContext.All);
            core.AddWebResourceRequestedFilter("http://nova.example/*", CoreWebView2WebResourceContext.All);
            core.AddWebResourceRequestedFilter("https://nova.app/*", CoreWebView2WebResourceContext.All);
            core.AddWebResourceRequestedFilter("http://nova.app/*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested -= OnNovaWebResourceRequested;
            core.WebResourceRequested += OnNovaWebResourceRequested;
        }
        catch { }
    }

    void OnNovaWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        try
        {
            var uri = e.Request.Uri;
            if (string.IsNullOrEmpty(uri)) return;

            if (uri.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                uri.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                uri.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                uri.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                uri.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                uri.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                uri.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                uri.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (uri.Contains("private.html", StringComparison.OrdinalIgnoreCase) ||
                uri.EndsWith("/private", StringComparison.OrdinalIgnoreCase))
            {
                if (sender is CoreWebView2 core)
                {
                    var privHtml = PrivateHomeHtml.GetContent();
                    var bytes = System.Text.Encoding.UTF8.GetBytes(privHtml);
                    var stream = new MemoryStream(bytes);
                    var response = core.Environment.CreateWebResourceResponse(
                        stream,
                        200,
                        "OK",
                        "Content-Type: text/html; charset=utf-8\r\nAccess-Control-Allow-Origin: *");
                    e.Response = response;
                    return;
                }
            }

            if (uri.StartsWith("https://nova.example", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("http://nova.example", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("https://nova.app", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("http://nova.app", StringComparison.OrdinalIgnoreCase))
            {
                if (sender is CoreWebView2 core)
                {
                    var accent = _settings?.AccentColor ?? _settings?.HtmlAccentColor ?? "#BEF47B";
                    var bg = _settings?.HtmlBackgroundColor ?? "#07090d";
                    var wp = GetWallpaperForHtml();
                    var dim = _settings?.HomeBgImageOpacity ?? 0.35;
                    var themed = NovaHomeHtml.GetThemedContent(accent, bg, wp, dim);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(themed);
                    var stream = new MemoryStream(bytes);
                    var response = core.Environment.CreateWebResourceResponse(
                        stream,
                        200,
                        "OK",
                        "Content-Type: text/html; charset=utf-8\r\nAccess-Control-Allow-Origin: *");
                    e.Response = response;
                }
            }
        }
        catch { }
    }

    protected override void OnInitialized(EventArgs e)
    {
        if (Application.Current.TryFindResource("HomeQuickLinkBtn") == null)
        {
            var s = new Style(typeof(Button));
            s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 10, 16, 10)));
            s.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(6)));
            s.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
            s.Setters.Add(new Setter(Control.FontSizeProperty, 13.0));
            Application.Current.Resources["HomeQuickLinkBtn"] = s;
        }
        if (_settings != null)
        {
            if (_settings.LastSession != null)
            {
                _settings.LastSession.RemoveAll(u => string.IsNullOrEmpty(u) || IsNovaHost(u));
            }
        }
        ExtractNovaHome(_settings?.AccentColor, _settings?.HtmlBackgroundColor);
        EnsurePrivateHtmlOnDisk();
        KillWpfHome();
        EnsureSettingsUiBuilt();

        AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler(OnAnyButtonClicked), true);

        if (HomePage != null)
        {
            HomePage.IsVisibleChanged += (_, _) =>
            {
                if (HomePage.Visibility == Visibility.Visible && _activeTab != null)
                {
                    KillWpfHome();
                    if (_activeTab.WebViewControl == null || !IsNovaHost(_activeTab.WebViewControl.Source?.ToString()))
                    {
                        _ = ShowNovaHomeAsync(_activeTab);
                    }
                }
            };
        }

        if (SettingsPage != null)
        {
            SettingsPage.IsVisibleChanged += (_, _) => SyncActiveTabVisuals();
        }

        if (TabsPanel != null)
        {
            TabsPanel.PreviewMouseDown += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(SyncActiveTabVisuals));
            TabsPanel.MouseLeftButtonUp += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(SyncActiveTabVisuals));
            TabsPanel.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var dep = e.OriginalSource as DependencyObject;
                while (dep != null && dep != TabsPanel)
                {
                    if (dep is Border b && b.Tag is BrowserTab t)
                    {
                        if (t.Url == "nova://settings")
                        {
                            if (t != _activeTab)
                            {
                                e.Handled = true;
                                OpenSettingsTabSafe();
                                return;
                            }
                        }
                        else if (_activeTab?.Url == "nova://settings" && _isSettingsDirty)
                        {
                            if (t != _activeTab)
                            {
                                e.Handled = true;
                                ShowUnsavedModal(() => SwitchToTab(t));
                                return;
                            }
                        }
                        break;
                    }
                    dep = GetParentSafe(dep);
                }
            };
        }



        Loaded += (_, _) =>
        {
            EnsureNavBarButtons();
            HookHomeNavButtons();
            EnsureSettingsUiBuilt();
            InjectSearchSettingsIntoWpf();
            KillWpfHome();

            if (_tabs.Count > 1)
            {
                var duplicateBlankTabs = _tabs.Where(t => t != _activeTab && t.Url != "nova://settings" && string.IsNullOrEmpty(t.Url) && (t.WebViewControl == null || string.IsNullOrEmpty(t.WebViewControl.Source?.ToString()))).ToList();
                foreach (var z in duplicateBlankTabs)
                {
                    CloseTab(z);
                }
            }

            if (_activeTab != null && _activeTab.Url == "nova://settings")
            {
                OpenSettingsTabSafe();
            }
            else if (_activeTab != null && (string.IsNullOrEmpty(_activeTab.Url) || IsNovaHost(_activeTab.Url) || _activeTab.Url == "about:blank"))
            {
                if (_activeTab.WebViewControl == null || !IsNovaHost(_activeTab.WebViewControl.Source?.ToString()))
                {
                    _ = ShowNovaHomeAsync(_activeTab);
                }
            }
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(EnsureNavBarButtons));
        };

        if (NavBarGrid != null)
        {
            NavBarGrid.Loaded += (_, _) => EnsureNavBarButtons();
            NavBarGrid.LayoutUpdated += (_, _) =>
            {
                if (MenuButton != null && (MenuButton.Content == null || MenuButton.Content.ToString() == ""))
                {
                    EnsureNavBarButtons();
                }
            };
        }

        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenSettingsFromUi()), Key.OemComma, ModifierKeys.Control));

        EnsureNavBarButtons();
        HookHomeNavButtons();
        HookAddressBoxSearch();
        base.OnInitialized(e);
    }

    void EnsureNavBarButtons()
    {
        try
        {
            if (MenuButton != null)
            {
                MenuButton.Visibility = Visibility.Visible;
                MenuButton.Width = 32;
                MenuButton.Height = 32;
                MenuButton.IsEnabled = true;
                MenuButton.Content = "\uE712";
                MenuButton.FontFamily = new FontFamily("Segoe MDL2 Assets");
                MenuButton.FontSize = 13;
            }

            if (SecurityButton != null)
            {
                SecurityButton.Visibility = Visibility.Visible;
                SecurityButton.FontFamily = new FontFamily("Segoe MDL2 Assets");
                SecurityButton.FontSize = 13;
                bool isSec = _activeTab?.IsSecure ?? true;
                SecurityButton.Content = isSec ? "\uE72E" : "\uE7BA";
                SecurityButton.Foreground = isSec
                    ? GetThemeBrush("AccentGreenBrush", Color.FromRgb(16, 185, 129))
                    : GetThemeBrush("AccentOrangeBrush", Color.FromRgb(245, 158, 11));
            }

            if (MuteButton != null)
            {
                MuteButton.FontFamily = new FontFamily("Segoe MDL2 Assets");
                MuteButton.FontSize = 13;
                if (_activeTab?.WebViewControl?.CoreWebView2 != null)
                {
                    bool isMuted = _activeTab.WebViewControl.CoreWebView2.IsMuted;
                    MuteButton.Content = isMuted ? "\uE767" : "\uE74F";
                }
            }

            if (NavBarGrid != null)
            {
                Button? settingsBtn = null;
                foreach (UIElement child in NavBarGrid.Children)
                {
                    if (child is Button b && ((b.Content is string s && s.Contains("\uE713")) || b.Name == "SettingsBtn"))
                    {
                        settingsBtn = b;
                        break;
                    }
                }

                if (settingsBtn == null)
                {
                    settingsBtn = new Button
                    {
                        Content = "\uE713",
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                        FontSize = 13,
                        Width = 32,
                        Height = 32,
                        Margin = new Thickness(0, 5, 2, 5),
                        Cursor = Cursors.Hand,
                        Visibility = Visibility.Visible
                    };
                    if (Application.Current.TryFindResource("NavButton") is Style navStyle)
                        settingsBtn.Style = navStyle;

                    while (NavBarGrid.ColumnDefinitions.Count <= 9)
                    {
                        NavBarGrid.ColumnDefinitions.Insert(Math.Max(0, NavBarGrid.ColumnDefinitions.Count - 1), new ColumnDefinition { Width = GridLength.Auto });
                    }
                    Grid.SetColumn(settingsBtn, 9);
                    NavBarGrid.Children.Add(settingsBtn);
                }
                else
                {
                    settingsBtn.Visibility = Visibility.Visible;
                    settingsBtn.Width = 32;
                    settingsBtn.Height = 32;
                    settingsBtn.IsEnabled = true;
                    settingsBtn.Content = "\uE713";
                    settingsBtn.FontFamily = new FontFamily("Segoe MDL2 Assets");
                    settingsBtn.FontSize = 13;
                }

                settingsBtn.Click -= SettingsBtn_Click;
                settingsBtn.Click += SettingsBtn_Click;
            }
        }
        catch { }
    }

    void SettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsFromUi();
    }


    private void SyncActiveTabVisuals()
    {
        try
        {
            if (_activeTab == null) return;

            if (_activeTab.Url == "nova://settings")
            {
                if (AddressBox != null && AddressBox.Text != string.Empty)
                {
                    AddressBox.Text = string.Empty;
                }
                if (WebViewContainer != null && WebViewContainer.Visibility != Visibility.Collapsed)
                {
                    WebViewContainer.Visibility = Visibility.Collapsed;
                }
                foreach (var tab in _tabs)
                {
                    if (tab.WebViewControl != null)
                    {
                        if (tab.WebViewControl.Visibility != Visibility.Collapsed)
                            tab.WebViewControl.Visibility = Visibility.Collapsed;
                        if (tab.WebViewControl.Width != 0)
                            tab.WebViewControl.Width = 0;
                        if (tab.WebViewControl.Height != 0)
                            tab.WebViewControl.Height = 0;
                    }
                }
                if (HomePage != null && HomePage.Visibility != Visibility.Collapsed)
                {
                    HomePage.Visibility = Visibility.Collapsed;
                }
                if (WallpaperLayer != null && WallpaperLayer.Visibility != Visibility.Collapsed)
                {
                    WallpaperLayer.Visibility = Visibility.Collapsed;
                }
                if (BlockedPage != null && BlockedPage.Visibility != Visibility.Collapsed)
                {
                    BlockedPage.Visibility = Visibility.Collapsed;
                }

                EnsureSettingsUiBuilt();

                if (SettingsPage != null)
                {
                    SettingsPage.BeginAnimation(UIElement.OpacityProperty, null);
                    SettingsPage.RenderTransform = null;
                    if (SettingsPage.Opacity < 1.0)
                        SettingsPage.Opacity = 1.0;
                    if (SettingsPage.Visibility != Visibility.Visible)
                        SettingsPage.Visibility = Visibility.Visible;
                    if (Panel.GetZIndex(SettingsPage) < 2000)
                        Panel.SetZIndex(SettingsPage, 2000);
                }
            }
            else
            {
                if (SettingsPage != null && SettingsPage.Visibility != Visibility.Collapsed)
                {
                    SettingsPage.Visibility = Visibility.Collapsed;
                }
                if (WebViewContainer != null && WebViewContainer.Visibility != Visibility.Visible)
                {
                    WebViewContainer.Visibility = Visibility.Visible;
                }
                if (_activeTab.WebViewControl != null)
                {
                    if (_activeTab.WebViewControl.Visibility != Visibility.Visible)
                    {
                        _activeTab.WebViewControl.Visibility = Visibility.Visible;
                    }
                    if (double.IsNaN(_activeTab.WebViewControl.Width) == false && _activeTab.WebViewControl.Width == 0)
                    {
                        _activeTab.WebViewControl.Width = double.NaN;
                        _activeTab.WebViewControl.Height = double.NaN;
                    }
                }
                else
                {
                    if (IsNovaHost(_activeTab.Url) || string.IsNullOrEmpty(_activeTab.Url))
                    {
                        _ = ShowNovaHomeAsync(_activeTab);
                    }
                }

                if (AddressBox != null && !AddressBox.IsFocused && !AddressBox.IsKeyboardFocused && !AddressBox.IsKeyboardFocusWithin)
                {
                    if (IsNovaHost(AddressBox.Text))
                    {
                        AddressBox.Text = string.Empty;
                    }
                }
            }

            if (TabsPanel != null)
            {
                foreach (UIElement child in TabsPanel.Children)
                {
                    if (child is Border tb && tb.Tag is BrowserTab tab)
                    {
                        bool isActive = (tab == _activeTab);
                        tb.CornerRadius = new CornerRadius(8, 8, 0, 0);
                        tb.Background = isActive 
                            ? GetThemeBrush("BgCardBrush", Color.FromRgb(20, 23, 36)) 
                            : Brushes.Transparent;
                        tb.BorderBrush = isActive 
                            ? GetThemeBrush("BorderBrush", Color.FromRgb(38, 42, 62)) 
                            : Brushes.Transparent;
                        tb.BorderThickness = isActive ? new Thickness(1, 1, 1, 0) : new Thickness(0);

                        foreach (var textBlock in FindVisualChildren<TextBlock>(tb))
                        {
                            textBlock.Foreground = isActive 
                                ? Brushes.White 
                                : GetThemeBrush("TextSecondaryBrush", Color.FromRgb(142, 147, 166));
                            textBlock.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
                        }
                    }
                }
            }

            if (SecurityButton != null)
            {
                bool isSec = _activeTab?.IsSecure ?? true;
                SecurityButton.Content = isSec ? "" : "";
                SecurityButton.Foreground = isSec
                    ? GetThemeBrush("AccentGreenBrush", Color.FromRgb(16, 185, 129))
                    : GetThemeBrush("AccentOrangeBrush", Color.FromRgb(245, 158, 11));
            }

            if (MuteButton != null && _activeTab?.WebViewControl?.CoreWebView2 != null)
            {
                bool isMuted = _activeTab.WebViewControl.CoreWebView2.IsMuted;
                MuteButton.Content = isMuted ? "" : "";
            }
        }
        catch { }
    }

    void OpenSettingsFromUi()
    {
        OpenSettingsTabSafe();
    }

    void OnAnyButtonClicked(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep)
        {
            var btn = dep as Button ?? FindVisualParent<Button>(dep);
            if (btn != null)
            {
                if (btn.Tag as string == "SettingsInternal") return;
                if (SettingsPage != null && btn.IsDescendantOf(SettingsPage)) return;

                if (btn.Content is string s && s.Contains(""))
                {
                    e.Handled = true;
                    if (_activeTab != null)
                    {
                        _isOnHomePage = true;
                        _ = ShowNovaHomeAsync(_activeTab);
                    }
                }
                else if (btn.Content is string sc && (sc.Contains("") || sc.Contains("⚙") || Grid.GetColumn(btn) == 9))
                {
                    e.Handled = true;
                    if (MenuPopup != null) MenuPopup.IsOpen = false;
                    OpenSettingsTabSafe();
                }

                else if (btn.Content is Panel p)
                {
                    foreach (var tb in FindVisualChildren<TextBlock>(p))
                    {
                        if (tb.Text != null && (tb.Text.Contains("") || tb.Text.Contains("На главную")))
                        {
                            e.Handled = true;
                            if (MenuPopup != null) MenuPopup.IsOpen = false;
                            if (_activeTab != null)
                            {
                                _isOnHomePage = true;
                                _ = ShowNovaHomeAsync(_activeTab);
                            }
                            break;
                        }
                        else if (tb.Text != null && (tb.Text.Contains("") || tb.Text.Contains("Настройки")))
                        {
                            e.Handled = true;
                            if (MenuPopup != null) MenuPopup.IsOpen = false;
                            OpenSettingsTabSafe();
                            break;
                        }
                    }
                }
            }
        }
    }

    void HookHomeNavButtons()
    {
        try
        {
            if (AddressBox?.Parent is FrameworkElement addressContainer && addressContainer.Parent is Panel navPanel)
            {
                foreach (UIElement child in navPanel.Children)
                {
                    if (child is Button btn)
                    {
                        if (Grid.GetColumn(btn) == 3 || (btn.Content is string sh && sh.Contains("")))
                        {
                            btn.Click -= Home_Click;
                            btn.Click += (_, _) =>
                            {
                                if (_activeTab != null)
                                {
                                    _isOnHomePage = true;
                                    _ = ShowNovaHomeAsync(_activeTab);
                                }
                            };
                        }
                        else if (Grid.GetColumn(btn) == 9 || (btn.Content is string ss && (ss.Contains("") || ss.Contains("⚙"))))
                        {
                            btn.Click -= Settings_Click;
                            btn.Click += (_, _) => OpenSettingsTabSafe();
                        }

                    }
                }
            }
        }
        catch { }
    }

    void InjectSearchSettingsIntoWpf()
    {
        if (_searchSettingsInjected) return;
        try
        {
            if (TabGeneral != null)
            {
                var card = CreateSearchSourcesSettingsCard();
                if (TabGeneral.Children.Count > 2)
                    TabGeneral.Children.Insert(2, card);
                else
                    TabGeneral.Children.Add(card);
                _searchSettingsInjected = true;
            }
        }
        catch { }
    }

    Border CreateSearchSourcesSettingsCard()
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20, 16, 20, 16),
            Margin = new Thickness(0, 0, 0, 12)
        };
        border.SetResourceReference(Border.BackgroundProperty, "BgSurfaceBrush");

        var stack = new StackPanel();

        var title = new TextBlock
        {
            Text = "Источники поиска Nova",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        };
        title.SetResourceReference(TextBlock.FontSizeProperty, "UiFontSize");
        title.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        stack.Children.Add(title);

        var desc = new TextBlock
        {
            Text = "Отметьте сайты, результаты с которых будут выдаваться при поиске в Nova (ссылки ведут на реальные сайты, без перенаправлений):",
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 14),
            TextWrapping = TextWrapping.Wrap
        };
        desc.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        stack.Children.Add(desc);

        var grid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var sources = new (int col, int row, string label, Func<bool> get, Action<bool> set)[]
        {
            (0, 0, "Яндекс (yandex.ru)", () => _settings.SearchSourceYandex, v => _settings.SearchSourceYandex = v),
            (1, 0, "Google (google.com)", () => _settings.SearchSourceGoogle, v => _settings.SearchSourceGoogle = v),
            (0, 1, "Википедия (wikipedia.org)", () => _settings.SearchSourceWiki, v => _settings.SearchSourceWiki = v),
            (1, 1, "Хабр (habr.com)", () => _settings.SearchSourceHabr, v => _settings.SearchSourceHabr = v),
            (0, 2, "GitHub (github.com)", () => _settings.SearchSourceGitHub, v => _settings.SearchSourceGitHub = v),
            (1, 2, "Кинопоиск (kinopoisk.ru)", () => _settings.SearchSourceKinopoisk, v => _settings.SearchSourceKinopoisk = v),
            (0, 3, "YouTube (youtube.com)", () => _settings.SearchSourceYouTube, v => _settings.SearchSourceYouTube = v),
            (1, 3, "ВКонтакте (vk.com)", () => _settings.SearchSourceVK, v => _settings.SearchSourceVK = v)
        };

        foreach (var (col, row, label, getter, setter) in sources)
        {
            var chk = new CheckBox
            {
                Content = label,
                IsChecked = getter(),
                Margin = new Thickness(0, 0, 12, 10),
                Cursor = Cursors.Hand
            };
            chk.SetResourceReference(CheckBox.ForegroundProperty, "TextPrimaryBrush");
            chk.SetResourceReference(CheckBox.FontSizeProperty, "UiFontSizeSm");
            chk.Checked += (_, _) => { setter(true); SaveSettings(); PushSearchSettingsToAllTabs(); };
            chk.Unchecked += (_, _) => { setter(false); SaveSettings(); PushSearchSettingsToAllTabs(); };
            Grid.SetColumn(chk, col);
            Grid.SetRow(chk, row);
            grid.Children.Add(chk);
        }
        stack.Children.Add(grid);

        var siteHeader = new TextBlock
        {
            Text = "Ограничить поиск конкретным сайтом (Site search)",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 0, 4)
        };
        siteHeader.SetResourceReference(TextBlock.FontSizeProperty, "UiFontSizeSm");
        siteHeader.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        stack.Children.Add(siteHeader);

        var siteDesc = new TextBlock
        {
            Text = "Если поле заполнено, поиск будет ограничен указанным доменом (например: habr.com или github.com):",
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap
        };
        siteDesc.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        stack.Children.Add(siteDesc);

        var siteBox = new TextBox
        {
            Text = _settings.SearchSiteFilter ?? "",
            Margin = new Thickness(0, 0, 0, 14),
            Height = 36,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        if (TryFindResource("AddressBar") is Style st) siteBox.Style = st;
        siteBox.TextChanged += (_, _) =>
        {
            _settings.SearchSiteFilter = siteBox.Text.Trim();
            SaveSettings();
            PushSearchSettingsToAllTabs();
        };
        stack.Children.Add(siteBox);



        border.Child = stack;
        return border;
    }

    void PushSearchSettingsToAllTabs()
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                type = "searchSettings",
                google = _settings.SearchSourceGoogle,
                yandex = _settings.SearchSourceYandex,
                wiki = _settings.SearchSourceWiki,
                habr = _settings.SearchSourceHabr,
                github = _settings.SearchSourceGitHub,
                kino = _settings.SearchSourceKinopoisk,
                youtube = _settings.SearchSourceYouTube,
                vk = _settings.SearchSourceVK,
                siteFilter = _settings.SearchSiteFilter ?? ""
            });
            foreach (var tab in _tabs)
            {
                if (tab.WebViewControl?.CoreWebView2 != null && IsNovaHost(tab.WebViewControl.Source?.ToString()))
                {
                    tab.WebViewControl.CoreWebView2.PostWebMessageAsJson(payload);
                }
            }
        }
        catch { }
    }

    internal void PushThemeSettingsToAllTabs(string? accentHex = null, string? bgHex = null)
    {
        try
        {
            var accent = !string.IsNullOrEmpty(accentHex) ? accentHex : (_settings?.AccentColor ?? _settings?.HtmlAccentColor ?? "#BEF47B");
            var bg = !string.IsNullOrEmpty(bgHex) ? bgHex : (_settings?.HtmlBackgroundColor ?? "#07090d");
            var wp = GetWallpaperForHtml();
            var dim = _settings?.HomeBgImageOpacity ?? 0.35;

            ExtractNovaHome(accent, bg, wp, dim);

            var payload = JsonSerializer.Serialize(new
            {
                type = "themeSettings",
                accent = accent,
                bg = bg,
                wallpaper = wp,
                dim = dim
            });

            foreach (var tab in _tabs)
            {
                if (tab.WebViewControl?.CoreWebView2 != null && IsNovaHost(tab.WebViewControl.Source?.ToString()))
                {
                    try { tab.WebViewControl.CoreWebView2.PostWebMessageAsJson(payload); } catch { }
                    try
                    {
                        var wpSafe = wp.Replace("'", "\\'").Replace("\r", "").Replace("\n", "");
                        var js = $"if(window.applyAccentTheme) window.applyAccentTheme('{accent}'); if(window.applyBgTheme) window.applyBgTheme('{bg}'); if(window.applyWallpaperSettings) window.applyWallpaperSettings('{wpSafe}', {dim.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)});";
                        _ = tab.WebViewControl.CoreWebView2.ExecuteScriptAsync(js);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    void SendSearchSettingsToTab(BrowserTab? tab)
    {
        if (tab?.WebViewControl?.CoreWebView2 == null) return;
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                type = "searchSettings",
                google = _settings.SearchSourceGoogle,
                yandex = _settings.SearchSourceYandex,
                wiki = _settings.SearchSourceWiki,
                habr = _settings.SearchSourceHabr,
                github = _settings.SearchSourceGitHub,
                kino = _settings.SearchSourceKinopoisk,
                youtube = _settings.SearchSourceYouTube,
                vk = _settings.SearchSourceVK,
                siteFilter = _settings.SearchSiteFilter ?? ""
            });
            tab.WebViewControl.CoreWebView2.PostWebMessageAsJson(payload);
        }
        catch { }
    }

    public void RevealWindow()
    {
        this.Opacity = _settings?.WindowOpacity ?? 1.0;
        this.Activate();
        this.Focus();
    }

    void HookAddressBoxSearch()
    {
        AddressBox.PreviewMouseLeftButtonDown += (sender, e) =>
        {
            AddressBox.Focus();
            Keyboard.Focus(AddressBox);
        };
        AddressBox.GotFocus += (sender, e) =>
        {
            AddressBox.SelectAll();
        };

        AddressBox.PreviewKeyDown += (sender, e) =>
        {
            if (e.Key == Key.Enter)
            {
                var text = AddressBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(text)) return;
                if (text.Equals("nova://private", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("about:private", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("chrome://private", StringComparison.OrdinalIgnoreCase))
                {
                    CreatePrivateTab();
                    return;
                }
                if (text.Equals("nova://settings", StringComparison.OrdinalIgnoreCase))
                {
                    e.Handled = true;
                    OpenSettingsFromUi();
                    return;
                }
                bool isUrl = text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                          || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                          || text.StartsWith("nova://", StringComparison.OrdinalIgnoreCase)
                          || text.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
                          || (!text.Contains(' ') && text.Contains('.') && text.Length > 3);
                if (isUrl)
                {
                    e.Handled = true;
                    SuggestPopup.IsOpen = false;
                    Keyboard.ClearFocus();
                    var targetUrl = text;
                    if (!targetUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        && !targetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        && !targetUrl.StartsWith("nova://", StringComparison.OrdinalIgnoreCase)
                        && !targetUrl.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                    {
                        targetUrl = "https://" + targetUrl;
                    }
                    _isOnHomePage = false;
                    HideAllPages();
                    NavigateTo(targetUrl);
                    return;
                }

                e.Handled = true;
                SuggestPopup.IsOpen = false;
                Keyboard.ClearFocus();
                var searchUrl = SecurityHelper.NormalizeInput(text, _settings.SearchEngine);
                NavigateTo(searchUrl);
            }
        };
    }

    void KillWpfHome()
    {
        if (HomePage != null)
        {
            HomePage.Visibility = Visibility.Collapsed;
            HomePage.Width = 0;
            HomePage.Height = 0;
            HomePage.IsEnabled = false;
        }
        if (WallpaperLayer != null)
        {
            WallpaperLayer.Visibility = Visibility.Collapsed;
        }
    }

    void ClearHomeAddress(string? currentUrl = null)
    {
        if (AddressBox.IsFocused || AddressBox.IsKeyboardFocused || AddressBox.IsKeyboardFocusWithin) return;
        if (_activeTab?.Url == "nova://settings")
        {
            if (AddressBox.Text != string.Empty) AddressBox.Text = string.Empty;
            return;
        }
        var src = currentUrl ?? _activeTab?.WebViewControl?.Source?.ToString();
        if (_isOnHomePage || IsNovaHost(src) || IsNovaHost(_activeTab?.Url) || IsNovaUrl(src) || IsNovaUrl(_activeTab?.Url))
        {
            AddressBox.Text = string.Empty;
        }
    }

    internal async Task ShowNovaHomeAsync(BrowserTab? tab, string? targetUrl = null)
    {
        if (tab == null || _htmlHomeBusy || tab.Url == "nova://settings" || tab.IsPrivate) return;
        _htmlHomeBusy = true;
        try
        {
            _isOnHomePage = true;
            var destUrl = targetUrl ?? NovaHomeUrl;
            tab.Url = destUrl;
            if (destUrl.Contains("#/search?q="))
            {
                var qMatch = System.Text.RegularExpressions.Regex.Match(destUrl, @"[?&]q=([^&]*)");
                if (qMatch.Success)
                {
                    var q = Uri.UnescapeDataString(qMatch.Groups[1].Value);
                    tab.Title = $"{q} — Nova Search";
                }
                else
                {
                    tab.Title = "Nova Search";
                }
            }
            else
            {
                tab.Title = "Новая вкладка";
            }
            UpdateTabsUI();
            KillWpfHome();

            var webView = tab.WebViewControl;
            if (webView == null)
            {
                webView = await CreateWebViewForTab(tab);
            }
            if (webView == null) return;

            await EnsureNovaHomeRegisteredAsync(webView);

            tab.WebViewControl = webView;
            webView.Width = double.NaN;
            webView.Height = double.NaN;
            webView.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView.VerticalAlignment = VerticalAlignment.Stretch;
            webView.Visibility = Visibility.Visible;
            if (WebViewContainer != null)
            {
                WebViewContainer.Visibility = Visibility.Visible;
                if (!WebViewContainer.Children.Contains(webView))
                    WebViewContainer.Children.Add(webView);
            }

            var currentSrc = webView.Source?.ToString() ?? string.Empty;
            if (!currentSrc.Equals(destUrl, StringComparison.OrdinalIgnoreCase))
            {
                webView.CoreWebView2.Navigate(destUrl);
            }
            else
            {
                webView.CoreWebView2.Reload();
            }
            ClearHomeAddress(destUrl);
            SendSearchSettingsToTab(tab);
        }
        finally
        {
            _htmlHomeBusy = false;
        }
    }

    async Task EnsureNovaHomeRegisteredAsync(WebView2 webView)
    {
        if (webView.CoreWebView2 == null)
        {
            if (_webViewEnv == null && App.EnvTask != null)
                _webViewEnv = await App.EnvTask;
            if (_webViewEnv != null)
                await webView.EnsureCoreWebView2Async(_webViewEnv);
            else
                await webView.EnsureCoreWebView2Async();
        }

        var core = webView.CoreWebView2;
        if (core == null) return;

        try
        {
            core.Settings.IsWebMessageEnabled = true;
            core.Settings.IsScriptEnabled = true;
        }
        catch { }
        MapNovaHost(core);

        core.NavigationStarting -= OnNovaNavStart;
        core.NavigationStarting += OnNovaNavStart;
        core.NavigationCompleted -= OnNovaNavCompleted;
        core.NavigationCompleted += OnNovaNavCompleted;
        core.WebMessageReceived -= OnNovaWebMessage;
        core.WebMessageReceived += OnNovaWebMessage;
        core.DOMContentLoaded -= OnNovaDomReady;
        core.DOMContentLoaded += OnNovaDomReady;
    }

    void OnNovaNavStart(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (e.Uri.Equals("nova://settings", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            Dispatcher.Invoke(() => OpenSettingsFromUi());
            return;
        }

        if (e.Uri.Equals("nova://downloads", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            Dispatcher.Invoke(() => ShowDownloadsTab());
            return;
        }
        if (e.Uri.Equals("nova://history", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            Dispatcher.Invoke(() => ShowHistoryTab());
            return;
        }
        if (e.Uri.Equals("nova://bookmarks", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            Dispatcher.Invoke(() => ShowBookmarksTab());
            return;
        }

        if (e.Uri.StartsWith("https://nova.app", StringComparison.OrdinalIgnoreCase) ||
            e.Uri.StartsWith("http://nova.app", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            Dispatcher.Invoke(() =>
            {
                if (_activeTab != null) _ = ShowNovaHomeAsync(_activeTab);
            });
            return;
        }

        if (IsNovaHost(e.Uri))
        {
            _isOnHomePage = true;
            Dispatcher.Invoke(() => ClearHomeAddress(e.Uri));
            return;
        }

        _isOnHomePage = false;
        Dispatcher.Invoke(() =>
        {
            HideAllPages();
            if (_activeTab?.WebViewControl != null)
            {
                _activeTab.WebViewControl.Visibility = Visibility.Visible;
                _activeTab.WebViewControl.Width = double.NaN;
                _activeTab.WebViewControl.Height = double.NaN;
            }
            if (_activeTab?.Url == "nova://settings" || IsNovaHost(e.Uri) || IsNovaUrl(e.Uri))
            {
                AddressBox.Text = string.Empty;
                if (_activeTab != null)
                {
                    _activeTab.Url = e.Uri;
                    UpdateTabsUI();
                }
                return;
            }
            AddressBox.Text = e.Uri;
            if (_activeTab != null)
            {
                _activeTab.Url = e.Uri;
                UpdateTabsUI();
            }
        });
    }

    void OnNovaNavCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_isOnHomePage || IsNovaHost(AddressBox.Text) || IsNovaHost(_activeTab?.Url) || (_activeTab?.Url != null && IsNovaUrl(_activeTab.Url)))
        {
            Dispatcher.Invoke(() => ClearHomeAddress());
        }
        else
        {
            Dispatcher.Invoke(() =>
            {
                if (_activeTab != null && !IsNovaHost(_activeTab.Url) && !IsNovaUrl(_activeTab.Url))
                {
                    AddressBox.Text = _activeTab.Url;
                }
                else
                {
                    AddressBox.Text = string.Empty;
                }
                UpdateTabsUI();
            });
        }
    }

    void OnNovaDomReady(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            ClearHomeAddress();
            SendSearchSettingsToTab(_activeTab);
            PushThemeSettingsToAllTabs();
        });
    }

    void OnNovaWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            string? raw = null;
            try { raw = e.TryGetWebMessageAsString(); } catch { }
            if (string.IsNullOrWhiteSpace(raw))
            {
                try { raw = e.WebMessageAsJson; } catch { }
            }
            if (string.IsNullOrWhiteSpace(raw)) return;

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var te) ? te.GetString() : null;

            switch (type)
            {
                case "ready":
                {
                    Dispatcher.Invoke(() =>
                    {
                        ClearHomeAddress();
                        SendSearchSettingsToTab(_activeTab);
                        PushThemeSettingsToAllTabs();
                        HtmlHomeReadyOnce?.Invoke();
                        HtmlHomeReadyOnce = null;
                    });
                    break;
                }
                case "getSearchSettings":
                {
                    Dispatcher.Invoke(() => SendSearchSettingsToTab(_activeTab));
                    break;
                }
                case "getThemeSettings":
                {
                    Dispatcher.Invoke(() => PushThemeSettingsToAllTabs());
                    break;
                }
                case "openSettings":
                {
                    Dispatcher.Invoke(() => OpenSettingsFromUi());
                    break;
                }
                case "openDownloads":
                {
                    Dispatcher.Invoke(() => ShowDownloadsTab());
                    break;
                }
                case "openHistory":
                {
                    Dispatcher.Invoke(() => ShowHistoryTab());
                    break;
                }
                case "openBookmarks":
                {
                    Dispatcher.Invoke(() => ShowBookmarksTab());
                    break;
                }
                case "nova_element_inspected":
                {
                    Dispatcher.Invoke(() =>
                    {
                        var sel = root.TryGetProperty("selector", out var se) ? se.GetString() : "";
                        var html = root.TryGetProperty("html", out var he) ? he.GetString() : "";
                        if (DtElementSelectorInput != null) DtElementSelectorInput.Text = sel ?? "";
                        if (DtElementsTree != null) DtElementsTree.Text = html ?? "";
                        if (DtElementsStatus != null) DtElementsStatus.Text = $"Выбран элемент: {sel}";
                        SetDevToolsView(DtElementsView);
                    });
                    break;
                }
                case "fetchLiveSearchResults":
                {
                    var q = root.TryGetProperty("query", out var qe) ? qe.GetString() : null;
                    var tab = root.TryGetProperty("tab", out var te2) ? te2.GetString() : "all";
                    var core = (sender as CoreWebView2) ?? _activeTab?.WebViewControl?.CoreWebView2;
                    if (!string.IsNullOrWhiteSpace(q) && core != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            var json = await FetchLiveSearchAggregatedJsonAsync(q, tab ?? "all");
                            Dispatcher.Invoke(() =>
                            {
                                try { core.PostWebMessageAsJson(json); } catch { }
                                try
                                {
                                    _ = core.ExecuteScriptAsync($"if(typeof renderLiveSearchResults==='function'){{ renderLiveSearchResults({json}); }}");
                                }
                                catch { }
                            });
                        });
                    }
                    break;
                }
                case "fetchMoreImages":
                {
                    var q = root.TryGetProperty("query", out var qe) ? qe.GetString() : null;
                    var offset = root.TryGetProperty("offset", out var oe) ? oe.GetInt32() : 151;
                    var core = (sender as CoreWebView2) ?? _activeTab?.WebViewControl?.CoreWebView2;
                    if (!string.IsNullOrWhiteSpace(q) && core != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            var more = await FetchMoreBingImagesAsync(q, offset);
                            var payload = new Dictionary<string, object?>
                            {
                                ["type"] = "moreImagesResult",
                                ["query"] = q,
                                ["offset"] = offset,
                                ["images"] = more
                            };
                            var json = JsonSerializer.Serialize(payload);
                            Dispatcher.Invoke(() =>
                            {
                                try { core.PostWebMessageAsJson(json); } catch { }
                            });
                        });
                    }
                    break;
                }
                case "fetchMoreVideos":
                {
                    var q = root.TryGetProperty("query", out var qe) ? qe.GetString() : null;
                    var page = root.TryGetProperty("page", out var pe) ? pe.GetInt32() : 2;
                    var core = (sender as CoreWebView2) ?? _activeTab?.WebViewControl?.CoreWebView2;
                    if (!string.IsNullOrWhiteSpace(q) && core != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            var more = await FetchMoreVideosAsync(q, page);
                            var payload = new Dictionary<string, object?>
                            {
                                ["type"] = "moreVideosResult",
                                ["query"] = q,
                                ["page"] = page,
                                ["videos"] = more
                            };
                            var json = JsonSerializer.Serialize(payload);
                            Dispatcher.Invoke(() =>
                            {
                                try { core.PostWebMessageAsJson(json); } catch { }
                            });
                        });
                    }
                    break;
                }
                case "search":
                {
                    var q = root.TryGetProperty("query", out var qe) ? qe.GetString() : null;
                    if (string.IsNullOrWhiteSpace(q)) return;
                    Dispatcher.Invoke(() =>
                    {
                        ExecuteNovaHtmlSearch(q!);
                    });
                    break;
                }
                case "searchStateChanged":
                {
                    var inSearch = root.TryGetProperty("inSearch", out var se) && se.GetBoolean();
                    var query = root.TryGetProperty("query", out var qe) ? qe.GetString() : null;
                    Dispatcher.Invoke(() =>
                    {
                        if (inSearch && !string.IsNullOrWhiteSpace(query))
                        {
                            if (_activeTab != null)
                            {
                                _activeTab.Title = $"{query} — Nova Search";
                                _activeTab.Url = $"{NovaHomeUrl}#/search?q={Uri.EscapeDataString(query)}";
                                UpdateTabsUI();
                                SaveSession();
                            }
                        }
                        else
                        {
                            if (_activeTab != null)
                            {
                                _activeTab.Title = "Новая вкладка";
                                _activeTab.Url = "";
                                UpdateTabsUI();
                                SaveSession();
                            }
                        }
                        ClearHomeAddress();
                    });
                    break;
                }
                case "navigate":
                {
                    var url = root.TryGetProperty("url", out var ue) ? ue.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        if (url.Equals("nova://private", StringComparison.OrdinalIgnoreCase) ||
                            url.Equals("about:private", StringComparison.OrdinalIgnoreCase) ||
                            url.Equals("chrome://private", StringComparison.OrdinalIgnoreCase))
                        {
                            Dispatcher.Invoke(() => CreatePrivateTab());
                            return;
                        }
                        if (url.Equals("nova://settings", StringComparison.OrdinalIgnoreCase))
                        {
                            Dispatcher.Invoke(() => OpenSettingsFromUi());
                            return;
                        }
                        if (url.Equals("nova://downloads", StringComparison.OrdinalIgnoreCase))
                        {
                            Dispatcher.Invoke(() => ShowDownloadsTab());
                            return;
                        }
                        if (url.Equals("nova://history", StringComparison.OrdinalIgnoreCase))
                        {
                            Dispatcher.Invoke(() => ShowHistoryTab());
                            return;
                        }
                        if (url.Equals("nova://bookmarks", StringComparison.OrdinalIgnoreCase))
                        {
                            Dispatcher.Invoke(() => ShowBookmarksTab());
                            return;
                        }
                        if (url.StartsWith("https://nova.app", StringComparison.OrdinalIgnoreCase) ||
                            url.StartsWith("http://nova.app", StringComparison.OrdinalIgnoreCase))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (_activeTab != null) _ = ShowNovaHomeAsync(_activeTab);
                            });
                            return;
                        }
                        _isOnHomePage = false;
                        Dispatcher.Invoke(() =>
                        {
                            HideAllPages();
                            NavigateTo(url!);
                        });
                    }
                    break;
                }
                case "split":
                {
                    var g = root.TryGetProperty("google", out var ge) ? ge.GetString() : null;
                    var y = root.TryGetProperty("yandex", out var ye) ? ye.GetString() : null;
                    Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(g)) CreateNewTab(g);
                        if (!string.IsNullOrWhiteSpace(y)) CreateNewTab(y);
                    });
                    break;
                }
                case "open_tab":
                {
                    var u = root.TryGetProperty("url", out var ue) ? ue.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(u)) Dispatcher.Invoke(() => CreateNewTab(u));
                    break;
                }

                case "open_settings":
                {
                    Dispatcher.Invoke(OpenSettingsFromUi);
                    break;
                }
            }
        }
        catch { }
    }

    static string BuildSearchUrl(string query, string engine)
    {
        var enc = Uri.EscapeDataString(query);
        return engine.ToLowerInvariant() switch
        {
            "nova" or "nova mix" or "nova search" => $"{NovaHomeUrl}#/search?q={enc}",
            "yandex" or "яндекс" => $"https://yandex.ru/search/?text={enc}",
            "bing" => $"https://www.bing.com/search?q={enc}",
            "duckduckgo" => $"https://duckduckgo.com/?q={enc}",
            "brave search" => $"https://search.brave.com/search?q={enc}",
            "yahoo" => $"https://search.yahoo.com/search?p={enc}",
            "baidu" => $"https://www.baidu.com/s?wd={enc}",
            "mail.ru" => $"https://go.mail.ru/search?q={enc}",
            "ecosia" => $"https://www.ecosia.org/search?q={enc}",
            "startpage" => $"https://www.startpage.com/sp/search?query={enc}",
            _ => $"{NovaHomeUrl}#/search?q={enc}"
        };
    }

    internal void ExecuteNovaHtmlSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        query = query.Trim();
        var enc = Uri.EscapeDataString(query);
        var targetUrl = $"{NovaHomeUrl}#/search?q={enc}";

        if (_activeTab?.WebViewControl?.CoreWebView2 != null && IsNovaHost(_activeTab.WebViewControl.Source?.ToString()))
        {
            _isOnHomePage = true;
            _activeTab.Url = targetUrl;
            _activeTab.Title = $"{query} — Nova Search";
            UpdateTabsUI();
            ClearHomeAddress();
            SaveSession();

            var escapedQuery = query.Replace("\\", "\\\\").Replace("'", "\\'");
            _activeTab.WebViewControl.CoreWebView2.ExecuteScriptAsync(
                $"if(typeof doSearch==='function'){{ doSearch('{escapedQuery}','all'); }}else{{ window.location.hash='#/search?q={enc}'; }}");
        }
        else if (_activeTab != null)
        {
            _isOnHomePage = true;
            _activeTab.Url = targetUrl;
            _activeTab.Title = $"{query} — Nova Search";
            UpdateTabsUI();
            ClearHomeAddress();
            SaveSession();
            _ = ShowNovaHomeAsync(_activeTab, targetUrl);
        }
    }

    internal static bool IsNovaSearchEngine(string? engine)
    {
        if (string.IsNullOrWhiteSpace(engine)) return true;
        var lower = engine.Trim().ToLowerInvariant();
        return lower is "nova" or "nova mix" or "nova search";
    }

    internal static DependencyObject? GetParentSafe(DependencyObject? element)
    {
        if (element == null) return null;
        if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
        {
            try { return VisualTreeHelper.GetParent(element); }
            catch { }
        }
        if (element is FrameworkContentElement fce)
        {
            return fce.Parent;
        }
        if (element is ContentElement ce)
        {
            return ContentOperations.GetParent(ce);
        }
        try { return LogicalTreeHelper.GetParent(element); }
        catch { return null; }
    }

    internal static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        var current = child;
        int steps = 0;
        while (current != null && steps++ < 100)
        {
            if (current is T match) return match;
            current = GetParentSafe(current);
        }
        return null;
    }

    internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj) where T : DependencyObject
    {
        if (depObj == null) yield break;
        if (depObj is not Visual && depObj is not System.Windows.Media.Media3D.Visual3D) yield break;
        int count = 0;
        try { count = VisualTreeHelper.GetChildrenCount(depObj); }
        catch { yield break; }
        for (int i = 0; i < count; i++)
        {
            DependencyObject? child = null;
            try { child = VisualTreeHelper.GetChild(depObj, i); }
            catch { }
            if (child is T t) yield return t;
            if (child != null)
            {
                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }

    private static readonly HttpClient _liveSearchClient = new HttpClient(new SocketsHttpHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    })
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    private static string CleanSnippet(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var s = raw;
        s = WebUtility.HtmlDecode(s);
        s = WebUtility.HtmlDecode(s);
        s = Regex.Replace(s, @"<script[^>]*>.*?</script>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<style[^>]*>.*?</style>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<[^>]+>", " ");
        s = Regex.Replace(s, @"&nbsp;?", " ");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    private static async Task<string> FetchLiveSearchAggregatedJsonAsync(string query, string tab = "all")
    {
        var cleanQ = query.Trim();
        var enc = Uri.EscapeDataString(cleanQ);

        var results = new List<Dictionary<string, string>>();
        var videos = new List<Dictionary<string, string>>();
        var images = new List<Dictionary<string, string>>();
        var related = new List<string>();
        Dictionary<string, string>? quickAnswer = null;

        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var domainCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        void AddResult(string title, string url, string snip, string source, string badge)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
            var cleanUrl = url.Split('?')[0].TrimEnd('/');
            var domain = "";
            try { domain = new Uri(url).Host.ToLowerInvariant(); } catch { }
            if (domain.StartsWith("www.")) domain = domain[4..];

            title = CleanSnippet(title);
            snip = CleanSnippet(snip);

            lock (results)
            {
                if (seenUrls.Contains(cleanUrl)) return;
                if (!string.IsNullOrEmpty(domain))
                {
                    if (domainCounts.TryGetValue(domain, out var count) && count >= 5) return;
                    domainCounts[domain] = count + 1;
                }
                seenUrls.Add(cleanUrl);
                results.Add(new Dictionary<string, string>
                {
                    ["title"] = title,
                    ["url"] = url,
                    ["domain"] = domain,
                    ["snippet"] = snip,
                    ["source"] = source,
                    ["badge"] = badge
                });
            }
        }

        async Task ScrapeGoogleNewsAsync(string qStr)
        {
            try
            {
                var encS = Uri.EscapeDataString(qStr);
                using var req = new HttpRequestMessage(HttpMethod.Get, $"https://news.google.com/rss/search?q={encS}&hl=ru&gl=RU&ceid=RU:ru");
                req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                var res = await _liveSearchClient.SendAsync(req);
                if (res.IsSuccessStatusCode)
                {
                    var xml = await res.Content.ReadAsStringAsync();
                    var items = Regex.Matches(xml, @"<item>(.*?)</item>", RegexOptions.Singleline);
                    foreach (Match m in items)
                    {
                        var tm = Regex.Match(m.Groups[1].Value, @"<title>(.*?)</title>");
                        var lm = Regex.Match(m.Groups[1].Value, @"<link>(.*?)</link>");
                        var dm = Regex.Match(m.Groups[1].Value, @"<description>(.*?)</description>", RegexOptions.Singleline);
                        var sm = Regex.Match(m.Groups[1].Value, @"<source[^>]*url=[\x27""](https?://[^\x27""]+)[\x27""][^>]*>(.*?)</source>");
                        if (tm.Success)
                        {
                            var title = CleanSnippet(tm.Groups[1].Value);
                            var u = lm.Success ? lm.Groups[1].Value.Trim() : "";
                            var sourceName = sm.Success ? CleanSnippet(sm.Groups[2].Value) : "Google";
                            var snip = dm.Success ? CleanSnippet(dm.Groups[1].Value) : "";
                            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(u))
                            {
                                AddResult(title, u, snip, sourceName, "Google");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        async Task ScrapeBingNewsAsync(string qStr)
        {
            try
            {
                var encS = Uri.EscapeDataString(qStr);
                using var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.bing.com/news/search?q={encS}&format=rss&setmkt=ru-RU&setlang=ru");
                req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                var res = await _liveSearchClient.SendAsync(req);
                if (res.IsSuccessStatusCode)
                {
                    var xml = await res.Content.ReadAsStringAsync();
                    var items = Regex.Matches(xml, @"<item>(.*?)</item>", RegexOptions.Singleline);
                    foreach (Match m in items)
                    {
                        var tm = Regex.Match(m.Groups[1].Value, @"<title>(.*?)</title>");
                        var lm = Regex.Match(m.Groups[1].Value, @"<link>(.*?)</link>");
                        var dm = Regex.Match(m.Groups[1].Value, @"<description>(.*?)</description>", RegexOptions.Singleline);
                        if (tm.Success)
                        {
                            var title = CleanSnippet(tm.Groups[1].Value);
                            var rawU = lm.Success ? lm.Groups[1].Value.Trim() : "";
                            var u = rawU;
                            var mUrl = Regex.Match(rawU, @"url=([^&]+)");
                            if (mUrl.Success) u = Uri.UnescapeDataString(mUrl.Groups[1].Value);
                            var snip = dm.Success ? CleanSnippet(dm.Groups[1].Value) : "";
                            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(u))
                            {
                                AddResult(title, u, snip, "Яндекс", "Яндекс");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        async Task ScrapeBingAsync(string qStr, int offset, string source, string badge)
        {
            try
            {
                var encS = Uri.EscapeDataString(qStr);
                using var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.bing.com/search?q={encS}&first={offset}&setmkt=ru-RU&setlang=ru");
                req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                req.Headers.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8");
                req.Headers.Add("Cookie", "SRCHHPGUSR=ADLT=OFF&NRSLT=50&SRCHLANG=ru;");
                var res = await _liveSearchClient.SendAsync(req);
                if (res.IsSuccessStatusCode)
                {
                    var html = await res.Content.ReadAsStringAsync();
                    var items = Regex.Matches(html, @"<li[^>]*class=[\x27""][^\x27""]*\bb_algo\b[^\x27""]*[\x27""][^>]*>(.*?)</li>", RegexOptions.Singleline);
                    foreach (Match item in items)
                    {
                        var tm = Regex.Match(item.Groups[1].Value, @"<h2><a[^>]+href=[\x27""](https?://[^\x27""]+)[\x27""][^>]*>(.*?)</a></h2>", RegexOptions.Singleline);
                        var sm = Regex.Match(item.Groups[1].Value, @"<p[^>]*>(.*?)</p>", RegexOptions.Singleline);
                        if (tm.Success)
                        {
                            var u = DecodeBingUrl(tm.Groups[1].Value);
                            var rawT = tm.Groups[2].Value;
                            var rawS = sm.Success ? sm.Groups[1].Value : "";
                            var title = CleanSnippet(rawT);
                            var snip = CleanSnippet(rawS);
                            AddResult(title, u, snip, source, badge);
                        }
                    }
                }
            }
            catch { }
        }

        var taskSuggest = Task.Run(async () =>
        {
            try
            {
                using var req1 = new HttpRequestMessage(HttpMethod.Get, $"https://suggestqueries.google.com/complete/search?client=firefox&q={enc}");
                req1.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                var res1 = await _liveSearchClient.SendAsync(req1);
                if (res1.IsSuccessStatusCode)
                {
                    var json1 = await res1.Content.ReadAsStringAsync();
                    using var doc1 = JsonDocument.Parse(json1);
                    if (doc1.RootElement.GetArrayLength() > 1 && doc1.RootElement[1].ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in doc1.RootElement[1].EnumerateArray())
                        {
                            var s = el.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                lock (related) { if (!related.Contains(s)) related.Add(s); }
                            }
                        }
                    }
                }
            }
            catch { }
            try
            {
                using var req2 = new HttpRequestMessage(HttpMethod.Get, $"https://suggest.yandex.ru/suggest-ya.cgi?v=4&part={enc}");
                req2.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                var res2 = await _liveSearchClient.SendAsync(req2);
                if (res2.IsSuccessStatusCode)
                {
                    var json2 = await res2.Content.ReadAsStringAsync();
                    using var doc2 = JsonDocument.Parse(json2);
                    if (doc2.RootElement.GetArrayLength() > 1 && doc2.RootElement[1].ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in doc2.RootElement[1].EnumerateArray())
                        {
                            var s = el.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                lock (related) { if (!related.Contains(s)) related.Add(s); }
                            }
                        }
                    }
                }
            }
            catch { }
        });

        var taskBing1 = Task.Run(() => ScrapeBingAsync(cleanQ, 1, "Google", "Google"));
        var taskBing2 = Task.Run(() => ScrapeBingAsync(cleanQ, 11, "Яндекс", "Яндекс"));
        var taskBing3 = Task.Run(() => ScrapeBingAsync(cleanQ, 21, "Google", "Google"));
        var taskBing4 = Task.Run(() => ScrapeBingAsync(cleanQ, 31, "Яндекс", "Яндекс"));
        var taskBing5 = Task.Run(() => ScrapeBingAsync(cleanQ, 41, "Google", "Google"));
        var taskBing6 = Task.Run(() => ScrapeBingAsync(cleanQ, 51, "Яндекс", "Яндекс"));
        var taskBing7 = Task.Run(() => ScrapeBingAsync(cleanQ + " официальный сайт", 1, "Google", "Google"));
        var taskBing8 = Task.Run(() => ScrapeBingAsync(cleanQ + " статьи обзор отзывы", 1, "Яндекс", "Яндекс"));
        var taskBing9 = Task.Run(() => ScrapeBingAsync(cleanQ + " каталог портал", 1, "Яндекс", "Яндекс"));
        var taskNews1 = Task.Run(() => ScrapeGoogleNewsAsync(cleanQ));
        var taskNews2 = Task.Run(() => ScrapeBingNewsAsync(cleanQ));

        var taskWiki = Task.Run(async () =>
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"https://ru.wikipedia.org/w/api.php?action=query&list=search&srsearch={enc}&srlimit=10&format=json&utf8=1");
                req.Headers.UserAgent.ParseAdd("NovaBrowser/1.0");
                var res = await _liveSearchClient.SendAsync(req);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("query", out var qObj) && qObj.TryGetProperty("search", out var sArr))
                    {
                        string? firstTitle = null;
                        int wikiAdded = 0;
                        foreach (var item in sArr.EnumerateArray())
                        {
                            var title = item.TryGetProperty("title", out var te) ? te.GetString() : null;
                            var rawSnippet = item.TryGetProperty("snippet", out var se) ? se.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(title))
                            {
                                firstTitle ??= title;
                                if (wikiAdded < 5)
                                {
                                    wikiAdded++;
                                    var cleanSnippet = CleanSnippet(rawSnippet);
                                    var safeSlug = Uri.EscapeDataString(title.Replace(' ', '_'));
                                    var itemUrl = $"https://ru.wikipedia.org/wiki/{safeSlug}";
                                    AddResult(title + " — Википедия", itemUrl, cleanSnippet, "Википедия", "Вики");
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(firstTitle))
                        {
                            try
                            {
                                var extEnc = Uri.EscapeDataString(firstTitle);
                                using var extReq = new HttpRequestMessage(HttpMethod.Get, $"https://ru.wikipedia.org/w/api.php?action=query&prop=extracts&exintro=1&explaintext=1&titles={extEnc}&format=json");
                                extReq.Headers.UserAgent.ParseAdd("NovaBrowser/1.0");
                                var extRes = await _liveSearchClient.SendAsync(extReq);
                                if (extRes.IsSuccessStatusCode)
                                {
                                    var extJson = await extRes.Content.ReadAsStringAsync();
                                    using var extDoc = JsonDocument.Parse(extJson);
                                    if (extDoc.RootElement.TryGetProperty("query", out var eqObj) && eqObj.TryGetProperty("pages", out var pObj))
                                    {
                                        foreach (var page in pObj.EnumerateObject())
                                        {
                                            if (page.Value.TryGetProperty("extract", out var ee))
                                            {
                                                var rawExt = ee.GetString();
                                                if (!string.IsNullOrWhiteSpace(rawExt))
                                                {
                                                    var lines = rawExt.Split('\n');
                                                    var firstP = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
                                                    if (!string.IsNullOrWhiteSpace(firstP))
                                                    {
                                                        quickAnswer = new Dictionary<string, string>
                                                        {
                                                            ["title"] = firstTitle,
                                                            ["text"] = firstP,
                                                            ["url"] = $"https://ru.wikipedia.org/wiki/{Uri.EscapeDataString(firstTitle.Replace(' ', '_'))}",
                                                            ["source"] = "Википедия"
                                                        };
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        });

        var taskGitHub = Task.Run(async () =>
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/search/repositories?q={enc}&sort=stars&order=desc");
                req.Headers.UserAgent.ParseAdd("NovaBrowser/1.0");
                var res = await _liveSearchClient.SendAsync(req);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var it in items.EnumerateArray().Take(2))
                        {
                            var name = it.TryGetProperty("full_name", out var np) ? np.GetString() : null;
                            var u = it.TryGetProperty("html_url", out var up) ? up.GetString() : null;
                            var desc = it.TryGetProperty("description", out var dp) ? dp.GetString() : "Репозиторий исходного кода на GitHub";
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(u))
                            {
                                AddResult(name + " — GitHub", u, desc ?? "Репозиторий исходного кода на GitHub", "GitHub", "GitHub");
                            }
                        }
                    }
                }
            }
            catch { }
        });

        var taskImages = Task.Run(async () =>
        {
            var offsets = new[] { 1, 51, 101, 151 };
            var subTasks = offsets.Select(async offset =>
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.bing.com/images/search?q={enc}&adlt=off&safesearch=off&form=HDRSC2&first={offset}");
                    req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                    req.Headers.Add("Cookie", "SRCHHPGUSR=ADLT=OFF&NRSLT=50&SRCHLANG=ru;");
                    var res = await _liveSearchClient.SendAsync(req);
                    if (res.IsSuccessStatusCode)
                    {
                        var html = await res.Content.ReadAsStringAsync();
                        var matches = Regex.Matches(html, @"m=[\x27""]({[^\x27""]+murl[^\x27""]+})[\x27""]");
                        foreach (Match m in matches)
                        {
                            try
                            {
                                var cleanJson = WebUtility.HtmlDecode(m.Groups[1].Value);
                                using var doc = JsonDocument.Parse(cleanJson);
                                var root = doc.RootElement;
                                var murl = root.TryGetProperty("murl", out var mp) ? mp.GetString() : null;
                                var turl = root.TryGetProperty("turl", out var tp) ? tp.GetString() : null;
                                var title = root.TryGetProperty("t", out var titp) ? titp.GetString() : cleanQ;
                                if (!string.IsNullOrEmpty(murl) && !string.IsNullOrEmpty(turl))
                                {
                                    lock (images)
                                    {
                                        if (images.Count < 120 && !images.Any(x => x["url"] == murl))
                                        {
                                            images.Add(new Dictionary<string, string>
                                            {
                                                ["thumb"] = turl,
                                                ["url"] = murl,
                                                ["title"] = WebUtility.HtmlDecode(title ?? cleanQ).Trim()
                                            });
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            });
            await Task.WhenAll(subTasks);
        });

        var taskYouTube = Task.Run(async () =>
        {
            var queries = new[] { enc, Uri.EscapeDataString(cleanQ + " видео"), Uri.EscapeDataString(cleanQ + " обзор") };
            var seen = new HashSet<string>();
            var ytTasks = queries.Select(async qEnc =>
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.youtube.com/results?search_query={qEnc}");
                    req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                    var res = await _liveSearchClient.SendAsync(req);
                    if (res.IsSuccessStatusCode)
                    {
                        var html = await res.Content.ReadAsStringAsync();
                        var match = Regex.Match(html, @"var ytInitialData = ({.*?});</script>");
                        if (match.Success)
                        {
                            using var doc = JsonDocument.Parse(match.Groups[1].Value);
                            void Walk(JsonElement el)
                            {
                                lock (videos)
                                {
                                    if (videos.Count >= 45) return;
                                }
                                if (el.ValueKind == JsonValueKind.Object)
                                {
                                    if (el.TryGetProperty("videoRenderer", out var vr))
                                    {
                                        var vid = vr.TryGetProperty("videoId", out var vidProp) ? vidProp.GetString() : null;
                                        if (!string.IsNullOrEmpty(vid))
                                        {
                                            bool isNew = false;
                                            lock (seen)
                                            {
                                                if (!seen.Contains(vid))
                                                {
                                                    seen.Add(vid);
                                                    isNew = true;
                                                }
                                            }
                                            if (isNew)
                                            {
                                                var vTitle = "";
                                                if (vr.TryGetProperty("title", out var tp) && tp.TryGetProperty("runs", out var tr) && tr.GetArrayLength() > 0)
                                                    vTitle = tr[0].TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : "";
                                                var channel = "YouTube";
                                                if (vr.TryGetProperty("ownerText", out var op) && op.TryGetProperty("runs", out var or) && or.GetArrayLength() > 0)
                                                    channel = or[0].TryGetProperty("text", out var ctxt) ? ctxt.GetString() ?? "YouTube" : "YouTube";
                                                var duration = "";
                                                if (vr.TryGetProperty("lengthText", out var lp) && lp.TryGetProperty("simpleText", out var st))
                                                    duration = st.GetString() ?? "";
                                                var thumb = $"https://i.ytimg.com/vi/{vid}/hqdefault.jpg";
                                                if (vr.TryGetProperty("thumbnail", out var thp) && thp.TryGetProperty("thumbnails", out var thArr) && thArr.GetArrayLength() > 0)
                                                {
                                                    var last = thArr[thArr.GetArrayLength() - 1];
                                                    if (last.TryGetProperty("url", out var up))
                                                        thumb = up.GetString() ?? thumb;
                                                }
                                                if (!string.IsNullOrWhiteSpace(vTitle))
                                                {
                                                    lock (videos)
                                                    {
                                                        if (videos.Count < 45)
                                                        {
                                                            videos.Add(new Dictionary<string, string>
                                                            {
                                                                ["id"] = vid,
                                                                ["title"] = WebUtility.HtmlDecode(vTitle).Trim(),
                                                                ["channel"] = WebUtility.HtmlDecode(channel).Trim(),
                                                                ["duration"] = duration,
                                                                ["thumb"] = thumb,
                                                                ["url"] = $"https://www.youtube.com/watch?v={vid}"
                                                            });
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    foreach (var prop in el.EnumerateObject())
                                        Walk(prop.Value);
                                }
                                else if (el.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in el.EnumerateArray())
                                        Walk(item);
                                }
                            }
                            Walk(doc.RootElement);
                        }
                    }
                }
                catch { }
            });
            await Task.WhenAll(ytTasks);
        });

        await Task.WhenAny(
            Task.WhenAll(taskBing1, taskBing2, taskBing3, taskBing4, taskBing5, taskBing6, taskBing7, taskBing8, taskBing9, taskNews1, taskNews2, taskWiki, taskGitHub, taskImages, taskYouTube, taskSuggest),
            Task.Delay(4500));

        if (results.Count < 15)
        {
            var fallbackServices = new (string name, string url, string desc, string domain, string badge)[]
            {
                ("Google Поиск: " + cleanQ, $"https://www.google.com/search?q={enc}", $"Открыть полную выдачу веб-сайтов и ответов по запросу «{cleanQ}» в Google.", "google.com", "Google"),
                ("Яндекс Поиск: " + cleanQ, $"https://yandex.ru/search/?text={enc}", $"Найдётся всё: смотреть актуальные русскоязычные ответы по «{cleanQ}» в Яндексе.", "yandex.ru", "Яндекс"),
                ("Википедия: " + cleanQ, $"https://ru.wikipedia.org/w/index.php?search={enc}", $"Энциклопедическая справка, происхождение термина, устройство и характеристики «{cleanQ}».", "ru.wikipedia.org", "Вики"),
                ("YouTube Видео: " + cleanQ, $"https://www.youtube.com/results?search_query={enc}", $"Обучающие видеоролики, подробные инструкции, распаковки и обзоры «{cleanQ}».", "youtube.com", "Google"),
                ("VK Видео: " + cleanQ, $"https://vk.com/video?q={enc}", $"Смотреть видео, клипы и трансляции по запросу «{cleanQ}» ВКонтакте.", "vk.com", "Яндекс"),
                ("Habr Статьи: " + cleanQ, $"https://habr.com/ru/search/?q={enc}", $"Инженерные разборы, статьи специалистов, руководства и опыт сообщества по «{cleanQ}».", "habr.com", "Яндекс"),
                ("4PDA Форум: " + cleanQ, $"https://4pda.to/forum/index.php?act=search&query={enc}", $"Прошивки, модификации, обсуждения и полезный софт для «{cleanQ}».", "4pda.to", "Яндекс"),
                ("RuTracker: " + cleanQ, $"https://rutracker.org/forum/tracker.php?nm={enc}", $"Каталог дистрибутивов, документации и материалов по теме «{cleanQ}».", "rutracker.org", "Яндекс"),
                ("Rutube: " + cleanQ, $"https://rutube.ru/search/?query={enc}", $"Смотреть ролики, передачи и обучающие программы по запросу «{cleanQ}».", "rutube.ru", "Яндекс"),
                ("GitHub Репозитории: " + cleanQ, $"https://github.com/search?q={enc}", $"Исходный код, библиотеки, утилиты и открытые проекты по теме «{cleanQ}».", "github.com", "Google"),
                ("Mail.ru Поиск: " + cleanQ, $"https://go.mail.ru/search?q={enc}", $"Русскоязычные сайты, каталоги и справочники по запросу «{cleanQ}».", "mail.ru", "Яндекс"),
                ("DuckDuckGo: " + cleanQ, $"https://duckduckgo.com/?q={enc}", $"Приватный веб-поиск без отслеживания по теме «{cleanQ}».", "duckduckgo.com", "Google")
            };
            foreach (var (name, url, desc, domain, badge) in fallbackServices)
            {
                AddResult(name, url, desc, badge, badge);
            }
        }

        var payload = new Dictionary<string, object?>
        {
            ["type"] = "liveSearchResults",
            ["query"] = cleanQ,
            ["tab"] = tab,
            ["totalCount"] = results.Count,
            ["quickAnswer"] = quickAnswer,
            ["results"] = results,
            ["videos"] = videos,
            ["images"] = images,
            ["related"] = related.Take(12).ToList()
        };

        return JsonSerializer.Serialize(payload);
    }

    private static async Task<List<Dictionary<string, string>>> FetchMoreBingImagesAsync(string query, int offset)
    {
        var cleanQ = query.Trim();
        var enc = Uri.EscapeDataString(cleanQ);
        var list = new List<Dictionary<string, string>>();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.bing.com/images/search?q={enc}&adlt=off&safesearch=off&form=HDRSC2&first={offset}");
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            req.Headers.Add("Cookie", "SRCHHPGUSR=ADLT=OFF&NRSLT=50&SRCHLANG=ru;");
            var res = await _liveSearchClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var html = await res.Content.ReadAsStringAsync();
                var matches = Regex.Matches(html, @"m=[\x27""]({[^\x27""]+murl[^\x27""]+})[\x27""]");
                foreach (Match m in matches)
                {
                    if (list.Count >= 40) break;
                    try
                    {
                        var cleanJson = WebUtility.HtmlDecode(m.Groups[1].Value);
                        using var doc = JsonDocument.Parse(cleanJson);
                        var root = doc.RootElement;
                        var murl = root.TryGetProperty("murl", out var mp) ? mp.GetString() : null;
                        var turl = root.TryGetProperty("turl", out var tp) ? tp.GetString() : null;
                        var title = root.TryGetProperty("t", out var titp) ? titp.GetString() : cleanQ;
                        if (!string.IsNullOrEmpty(murl) && !string.IsNullOrEmpty(turl))
                        {
                            list.Add(new Dictionary<string, string>
                            {
                                ["thumb"] = turl,
                                ["url"] = murl,
                                ["title"] = WebUtility.HtmlDecode(title ?? cleanQ).Trim()
                            });
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
        if (list.Count == 0)
        {
            var engines = new[] { ("Яндекс Картинки", $"https://yandex.ru/images/search?text={Uri.EscapeDataString(cleanQ)}"),
                                  ("Google Картинки", $"https://www.google.com/search?tbm=isch&q={Uri.EscapeDataString(cleanQ)}") };
            foreach (var (eng, url) in engines)
            {
                list.Add(new Dictionary<string, string>
                {
                    ["thumb"] = "https://yastatic.net/icon-pack/0.0.1/default/favicon.ico",
                    ["url"] = url,
                    ["title"] = $"{cleanQ} — {eng}"
                });
            }
        }
        return list;
    }

    private static async Task<List<Dictionary<string, string>>> FetchMoreVideosAsync(string query, int page)
    {
        var cleanQ = query.Trim();
        var suffixes = new[] { "клип", "интересное", "2024", "обзор", "лучшее", "hd" };
        var suffix = suffixes[(Math.Max(1, page) - 1) % suffixes.Length];
        var enc = Uri.EscapeDataString($"{cleanQ} {suffix}");
        var list = new List<Dictionary<string, string>>();
        var seen = new HashSet<string>();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.youtube.com/results?search_query={enc}");
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            var res = await _liveSearchClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var html = await res.Content.ReadAsStringAsync();
                var match = Regex.Match(html, @"var ytInitialData = ({.*?});</script>");
                if (match.Success)
                {
                    using var doc = JsonDocument.Parse(match.Groups[1].Value);
                    void Walk(JsonElement el)
                    {
                        if (list.Count >= 20) return;
                        if (el.ValueKind == JsonValueKind.Object)
                        {
                            if (el.TryGetProperty("videoRenderer", out var vr))
                            {
                                var vid = vr.TryGetProperty("videoId", out var vidProp) ? vidProp.GetString() : null;
                                if (!string.IsNullOrEmpty(vid))
                                {
                                    bool isNew = false;
                                    lock (seen)
                                    {
                                        if (!seen.Contains(vid))
                                        {
                                            seen.Add(vid);
                                            isNew = true;
                                        }
                                    }
                                    if (isNew)
                                    {
                                        var vTitle = "";
                                        if (vr.TryGetProperty("title", out var tp) && tp.TryGetProperty("runs", out var tr) && tr.GetArrayLength() > 0)
                                            vTitle = tr[0].TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : "";
                                        var channel = "YouTube";
                                        if (vr.TryGetProperty("ownerText", out var op) && op.TryGetProperty("runs", out var or) && or.GetArrayLength() > 0)
                                            channel = or[0].TryGetProperty("text", out var ctxt) ? ctxt.GetString() ?? "YouTube" : "YouTube";
                                        var duration = "";
                                        if (vr.TryGetProperty("lengthText", out var lp) && lp.TryGetProperty("simpleText", out var st))
                                            duration = st.GetString() ?? "";
                                        var thumb = $"https://i.ytimg.com/vi/{vid}/hqdefault.jpg";
                                        if (vr.TryGetProperty("thumbnail", out var thp) && thp.TryGetProperty("thumbnails", out var thArr) && thArr.GetArrayLength() > 0)
                                        {
                                            var last = thArr[thArr.GetArrayLength() - 1];
                                            if (last.TryGetProperty("url", out var up))
                                                thumb = up.GetString() ?? thumb;
                                        }
                                        if (!string.IsNullOrWhiteSpace(vTitle))
                                        {
                                            list.Add(new Dictionary<string, string>
                                            {
                                                ["id"] = vid,
                                                ["title"] = WebUtility.HtmlDecode(vTitle).Trim(),
                                                ["channel"] = WebUtility.HtmlDecode(channel).Trim(),
                                                ["duration"] = duration,
                                                ["thumb"] = thumb,
                                                ["url"] = $"https://www.youtube.com/watch?v={vid}"
                                            });
                                        }
                                    }
                                }
                            }
                            foreach (var prop in el.EnumerateObject())
                                Walk(prop.Value);
                        }
                        else if (el.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in el.EnumerateArray())
                                Walk(item);
                        }
                    }
                    Walk(doc.RootElement);
                }
            }
        }
        catch { }
        if (list.Count == 0)
        {
            var platforms = new[] { ("YouTube", $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(cleanQ)}", "https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg"),
                                    ("Rutube", $"https://rutube.ru/search/?query={Uri.EscapeDataString(cleanQ)}", "https://pic.rutube.ru/static/favicon/favicon-192x192.png"),
                                    ("VK Видео", $"https://vk.com/video?q={Uri.EscapeDataString(cleanQ)}", "https://vk.com/images/svg_icons/ic_video.svg") };
            foreach (var (plt, url, th) in platforms)
            {
                list.Add(new Dictionary<string, string>
                {
                    ["id"] = Guid.NewGuid().ToString("N"),
                    ["title"] = $"{cleanQ} — смотреть на {plt}",
                    ["channel"] = plt,
                    ["duration"] = "HD",
                    ["thumb"] = th,
                    ["url"] = url
                });
            }
        }
        return list;
    }

    private static string DecodeBingUrl(string rawUrl)
    {
        try
        {
            var clean = rawUrl.Replace("&amp;", "&");
            var match = Regex.Match(clean, @"[?&]u=a1([a-zA-Z0-9_\-]+)");
            if (match.Success)
            {
                var b64 = match.Groups[1].Value.Replace('-', '+').Replace('_', '/');
                int mod = b64.Length % 4;
                if (mod > 0) b64 += new string('=', 4 - mod);
                var bytes = Convert.FromBase64String(b64);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
        }
        catch { }
        return rawUrl.Replace("&amp;", "&");
    }

}
