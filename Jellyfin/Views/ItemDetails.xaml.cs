using System;
using Jellyfin.Sdk;
using Jellyfin.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.Views;

public sealed partial class ItemDetails : Page
{
    public ItemDetails()
    {
        InitializeComponent();

        // TODO: Is there a better way to do DI in UWP?
        JellyfinApiClient jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();
        NavigationManager navigationManager = AppServices.Instance.ServiceProvider.GetRequiredService<NavigationManager>();

        ViewModel = new ItemDetailsViewModel(jellyfinApiClient, navigationManager);
    }

    internal ItemDetailsViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e) => ViewModel.HandleParameters(e.Parameter as Parameters);

    public record Parameters(Guid ItemId);
}
