#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;

namespace NovaBrowser;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void SetStatus(string text)
    {
        Dispatcher.Invoke(() =>
        {
            if (StatusText != null) StatusText.Text = text;
        });
    }
}
