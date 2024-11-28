using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed partial class Home : Page
{
    public Home()
    {
        InitializeComponent();

        ViewModel = AppServices.Instance.ServiceProvider.GetRequiredService<HomeViewModel>();
    }

    internal HomeViewModel ViewModel { get; }
}
