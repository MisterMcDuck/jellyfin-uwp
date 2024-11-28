using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed partial class ServerSelection : Page
{
    public ServerSelection()
    {
        InitializeComponent();

        ViewModel = AppServices.Instance.ServiceProvider.GetRequiredService<ServerSelectionViewModel>();
    }

    public ServerSelectionViewModel ViewModel { get; }
}