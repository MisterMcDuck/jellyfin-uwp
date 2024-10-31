using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Jellyfin.Core;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.Views;

public sealed partial class Home : Page
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;

    public Home()
    {
        // TODO: Is there a better way to do DI in UWP?
        _appSettings = AppServices.Instance.ServiceProvider.GetRequiredService<AppSettings>();
        _jellyfinApiClient = AppServices.Instance.ServiceProvider.GetRequiredService<JellyfinApiClient>();

        InitializeComponent();
    }

    private ObservableCollection<UserView> UserViews { get; } = new();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // TODO: How to properly load async data?
        LoadUserViews().Wait();

        base.OnNavigatedTo(e);
    }

    private async Task LoadUserViews()
    {
        BaseItemDtoQueryResult result = await _jellyfinApiClient.UserViews.GetAsync()
            .ConfigureAwait(false);
        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }

            Guid itemId = item.Id.Value;

            RequestInformation imageRequest = _jellyfinApiClient.Items[itemId].Images[ImageType.Primary.ToString()].ToGetRequestInformation();
            Uri imageUri = _jellyfinApiClient.BuildUri(imageRequest);

            UserView view = new()
            {
                Id = itemId,
                Name = item.Name,
                ImageUri = imageUri,
            };

            UserViews.Add(view);
        }
    }

    private void LogOutButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        _appSettings.AccessToken = null;
        Frame.Navigate(typeof(Login));
    }
}

public sealed class UserView()
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public Uri ImageUri { get; set; }
}
