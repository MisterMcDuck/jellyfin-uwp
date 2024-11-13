using System;
using Jellyfin.Sdk;
using Jellyfin.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();

        // TODO: Is there a better way to do DI in UWP?
        AppSettings appSettings = AppServices.Instance.ServiceProvider.GetRequiredService<AppSettings>();
        JellyfinApiClient jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();
        NavigationManager navigationManager = AppServices.Instance.ServiceProvider.GetRequiredService<NavigationManager>();

        ViewModel = new MainPageViewModel(appSettings, jellyfinApiClient, navigationManager, ContentFrame);
    }

    internal MainPageViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e) => ViewModel.HandleParameters(e.Parameter as Parameters);

    public record Parameters(Action DeferredNavigationAction);
}
