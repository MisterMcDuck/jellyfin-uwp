using Jellyfin.Sdk;
using Jellyfin.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed partial class Login : Page
{
    public Login()
    {
        InitializeComponent();

        // TODO: Is there a better way to do DI in UWP?
        AppSettings appSettings = AppServices.Instance.ServiceProvider.GetRequiredService<AppSettings>();
        JellyfinSdkSettings sdkClientSettings = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinSdkSettings>();
        JellyfinApiClient jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();
        NavigationManager navigationManager = AppServices.Instance.ServiceProvider.GetRequiredService<NavigationManager>();

        ViewModel = new LoginViewModel(appSettings, sdkClientSettings, jellyfinApiClient, navigationManager);
    }

    public LoginViewModel ViewModel { get; }
}