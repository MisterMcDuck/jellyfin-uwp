using System;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
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
        DeviceProfileManager deviceProfileManager = AppServices.Instance.ServiceProvider.GetRequiredService<DeviceProfileManager>();

        ViewModel = new VideoViewModel(jellyfinApiClient, sdkClientSettings, deviceProfileManager, PlayerElement);
    }

    internal VideoViewModel ViewModel { get; }

    protected override async void OnNavigatedTo(NavigationEventArgs e) => await ViewModel.PlayVideoAsync(e.Parameter as Parameters);

    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e) => await ViewModel.StopVideoAsync();

    public record Parameters(Guid VideoId, MediaStream VideoStream, MediaStream AudioStream, MediaStream SubtitleStream);
}
