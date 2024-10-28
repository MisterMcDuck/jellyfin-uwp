using System;
using Jellyfin.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Controls;

public sealed partial class JellyfinWebView : UserControl
{
    public JellyfinWebView()
    {
        InitializeComponent();

        WView.ContainsFullScreenElementChanged += JellyfinWebView_ContainsFullScreenElementChanged;
        WView.NavigationCompleted += JellyfinWebView_NavigationCompleted;
        WView.NavigationFailed += WView_NavigationFailed;

        SystemNavigationManager.GetForCurrentView().BackRequested += Back_BackRequested;
        Loaded += JellyfinWebView_Loaded;
    }

    private async void WView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
    {
        MessageDialog md = new MessageDialog("Navigation failed");
        await md.ShowAsync();
    }

    private void JellyfinWebView_Loaded(object sender, RoutedEventArgs e)
    {
        WView.Navigate(new Uri(Central.Settings.JellyfinServer));
    }

    private void Back_BackRequested(object sender, BackRequestedEventArgs e)
    {
        if (WView.CanGoBack)
        {
            WView.GoBack();
        }
        e.Handled = true;
    }

    private async void JellyfinWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
    {
        await WView.InvokeScriptAsync("eval", new string[] { "navigator.gamepadInputEmulation = 'mouse';" });
    }

    private void JellyfinWebView_ContainsFullScreenElementChanged(WebView sender, object args)
    {
        ApplicationView appView = ApplicationView.GetForCurrentView();

        if (sender.ContainsFullScreenElement)
        {
            appView.TryEnterFullScreenMode();
            return;
        }

        if (!appView.IsFullScreenMode)
        {
            return;
        }

        appView.ExitFullScreenMode();
    }
}
