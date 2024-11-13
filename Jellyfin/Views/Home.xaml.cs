using Jellyfin.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed partial class Home : Page
{
    public Home()
    {
        InitializeComponent();

        // TODO: Is there a better way to do DI in UWP?
        JellyfinApiClient jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();

        ViewModel = new HomeViewModel(jellyfinApiClient);
    }

    internal HomeViewModel ViewModel { get; }
}
