using Jellyfin.Core;
using Jellyfin.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed partial class ServerSelection : Page
{
    public ServerSelection()
    {
        InitializeComponent();

        // TODO: Is there a better way to do DI in UWP?
        AppSettings appSettings = AppServices.Instance.ServiceProvider.GetRequiredService<AppSettings>();
        JellyfinSdkSettings sdkClientSettings = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinSdkSettings>();
        JellyfinApiClient jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();

        ViewModel = new ServerSelectionViewModel(appSettings, sdkClientSettings, jellyfinApiClient);
    }

    public ServerSelectionViewModel ViewModel { get; }
}