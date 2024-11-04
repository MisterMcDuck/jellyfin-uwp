using System;
using Jellyfin.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.Views;

public sealed partial class Video : Page
{
    public Video()
    {
        InitializeComponent();

        // TODO: Is there a better way to do DI in UWP?
        JellyfinApiClient jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();

        ViewModel = new VideoViewModel(jellyfinApiClient);
    }

    internal VideoViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // TODO: Use something like MoviesParameters?
        Guid parameters = (Guid)e.Parameter;

        ViewModel.SetVideoId(parameters);
    }
}
