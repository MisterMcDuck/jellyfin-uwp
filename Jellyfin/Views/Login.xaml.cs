using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed partial class Login : Page
{
    public Login()
    {
        InitializeComponent();

        ViewModel = AppServices.Instance.ServiceProvider.GetRequiredService<LoginViewModel>();
    }

    public LoginViewModel ViewModel { get; }
}