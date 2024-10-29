using Jellyfin.Core;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed partial class Home : Page
{
    private readonly AppSettings _appSettings;

    public Home()
    {
        // TODO: Is there a better way to do DI in UWP?
        _appSettings = AppServices.Instance.ServiceProvider.GetRequiredService<AppSettings>();

        InitializeComponent();
    }

    private void LogOut_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        _appSettings.AccessToken = null;
        Frame.Navigate(typeof(Login));
    }
}