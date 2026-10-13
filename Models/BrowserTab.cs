#nullable enable
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;

namespace NovaBrowser.Models;

public class BrowserTab
{
    public string  Title      { get; set; } = "Новая вкладка";
    public string  Url        { get; set; } = "";
    public bool    IsLoading  { get; set; }
    public bool    IsSecure   { get; set; }
    public bool    IsPinned   { get; set; }
    public bool    IsMuted    { get; set; }
    public bool    IsPlayingAudio { get; set; }
    public bool    IsPrivate  { get; set; }
    public string? PrivateProfilePath { get; set; }
    public string? FaviconUrl { get; set; }
    public ImageSource? FaviconImage { get; set; }
    public double  ZoomLevel  { get; set; } = 0;
    public WebView2? WebViewControl { get; set; }
}
