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
        JellyfinSdkSettings sdkClientSettings = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinSdkSettings>();

        ViewModel = new VideoViewModel(jellyfinApiClient, sdkClientSettings, PlayerElement);
    }

    internal VideoViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e) => ViewModel.HandleParameters(e.Parameter as Parameters);

    public record Parameters(Guid VideoId, int? VideoStreamIndex, int? AudioStreamIndex, int? SubtitleStreamIndex);
}
