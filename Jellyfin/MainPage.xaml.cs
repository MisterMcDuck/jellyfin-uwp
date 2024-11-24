using System;
using Jellyfin.Sdk;
using Jellyfin.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin;

public sealed partial class MainPage : Page
{
    private readonly NavigationManager _navigationManager;

    public MainPage()
    {
        InitializeComponent();

        // TODO: Is there a better way to do DI in UWP?
        AppSettings appSettings = AppServices.Instance.ServiceProvider.GetRequiredService<AppSettings>();
        JellyfinApiClient jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();
        _navigationManager = AppServices.Instance.ServiceProvider.GetRequiredService<NavigationManager>();

        ViewModel = new MainPageViewModel(appSettings, jellyfinApiClient, _navigationManager, ContentFrame);

        // Cache the page state so the ContentFrame's BackStack can be preserved
        NavigationCacheMode = NavigationCacheMode.Required;
    }

    internal MainPageViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _navigationManager.RegisterContentFrame(ContentFrame);

        ViewModel.HandleParameters(e.Parameter as Parameters);

        base.OnNavigatedTo(e);
    }

    public record Parameters(Action DeferredNavigationAction);
}
