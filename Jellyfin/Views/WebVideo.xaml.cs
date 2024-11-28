﻿using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.Views;

public sealed partial class WebVideo : Page
{
    public WebVideo()
    {
        InitializeComponent();

        ViewModel = new WebVideoViewModel();
    }

    internal WebVideoViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e) => ViewModel.HandleParameters(e.Parameter as Parameters);

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        // Dispose of the webview to stop the video
        WebView2.Close();
    }

    public record Parameters(Uri VideoUri);
}
