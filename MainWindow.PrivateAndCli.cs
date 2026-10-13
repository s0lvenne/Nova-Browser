#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NovaBrowser.Helpers;
using NovaBrowser.Models;

namespace NovaBrowser;

public partial class MainWindow
{
    DispatcherTimer? _openUrlTimer;

    public void StartOpenUrlWatcher()
    {
        _openUrlTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        _openUrlTimer.Tick += (_, _) =>
        {
            try
            {
                var pipe = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NovaBrowser", "open.url");
                if (!File.Exists(pipe)) return;
                var url = File.ReadAllText(pipe).Trim();
                File.Delete(pipe);
                if (!string.IsNullOrWhiteSpace(url))
                {
                    Activate();
                    WindowState = WindowState.Normal;
                    CreateNewTab(url);
                }
            }
            catch { }
        };
        _openUrlTimer.Start();
    }

    public async Task CreatePrivateTabFixed()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var privatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovaBrowser", "Private", id);
        Directory.CreateDirectory(privatePath);

        var tab = new BrowserTab
        {
            Title = "Приватный режим",
            Url = NovaPrivateUrl,
            IsPrivate = true,
            PrivateProfilePath = privatePath
        };
        _tabs.Add(tab);
        _activeTab = tab;
        HideAllWebViews();

        try
        {
            var privArgs = App.BuildWebView2Arguments(_settings) +
                " --disable-background-timer-throttling " +
                "--disable-backgrounding-occluded-windows " +
                "--disable-renderer-backgrounding " +
                "--disk-cache-size=67108864 --media-cache-size=33554432 ";
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: privatePath,
                options: new CoreWebView2EnvironmentOptions(privArgs));
            var webView = new WebView2 { Visibility = Visibility.Visible };
            WebViewContainer.Children.Add(webView);
            await webView.EnsureCoreWebView2Async(env);

            if (webView.CoreWebView2 != null)
            {
                var s = webView.CoreWebView2.Settings;
                s.IsPasswordAutosaveEnabled = false;
                s.IsGeneralAutofillEnabled = false;
                s.IsReputationCheckingRequired = _settings.SafeBrowsingMode;
                MapNovaHost(webView.CoreWebView2);
                webView.CoreWebView2.WebMessageReceived -= OnNovaWebMessage;
                webView.CoreWebView2.WebMessageReceived += OnNovaWebMessage;
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    GetCachedInjectionScript());
                WireAdBlocker(webView.CoreWebView2, tab);

                webView.CoreWebView2.DocumentTitleChanged += (_, _) =>
                {
                    tab.Title = "• " + webView.CoreWebView2.DocumentTitle;
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
                        AddressBox.Text = (IsNovaUrl(tab.Url) || IsNovaHost(tab.Url) || tab.Url.Contains("private.html", StringComparison.OrdinalIgnoreCase)) ? "" : tab.Url;
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
            if (PrivateIndicator != null) PrivateIndicator.Visibility = Visibility.Visible;
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

    public void WipePrivateProfile(BrowserTab tab)
    {
        if (!tab.IsPrivate || string.IsNullOrEmpty(tab.PrivateProfilePath)) return;
        try
        {
            tab.WebViewControl?.Dispose();
            tab.WebViewControl = null;
            var path = tab.PrivateProfilePath;
            Task.Run(() =>
            {
                try
                {
                    Task.Delay(400).Wait();
                    if (Directory.Exists(path))
                        Directory.Delete(path, recursive: true);
                }
                catch { }
            });
        }
        catch { }
    }
}
