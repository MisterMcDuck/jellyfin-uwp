using System;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.Views;

public sealed partial class ItemDetails : Page
{
    public ItemDetails()
    {
        InitializeComponent();

        ViewModel = AppServices.Instance.ServiceProvider.GetRequiredService<ItemDetailsViewModel>();
    }

    internal ItemDetailsViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e) => ViewModel.HandleParameters(e.Parameter as Parameters);

    public record Parameters(Guid ItemId);
}
