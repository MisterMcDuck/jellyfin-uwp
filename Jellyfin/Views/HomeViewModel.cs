using System;
using System.Collections.ObjectModel;
using Jellyfin.Core;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Kiota.Abstractions;

namespace Jellyfin.Views;

public sealed record UserView(Guid Id, string Name, Uri ImageUri);

public sealed class HomeViewModel : BindableBase
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;

    public HomeViewModel(AppSettings appSettings, JellyfinApiClient jellyfinApiClient)
    {
        _appSettings = appSettings;
        _jellyfinApiClient = jellyfinApiClient;

        InitializeUserViews();
    }

    public ObservableCollection<UserView> UserViews { get; } = new();

    public void LogOut()
    {
        _appSettings.AccessToken = null;
        App.AppFrame.Navigate(typeof(Login));
    }

    private async void InitializeUserViews()
    {
        BaseItemDtoQueryResult result = await _jellyfinApiClient.UserViews.GetAsync();
        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }
            Guid itemId = item.Id.Value;

            RequestInformation imageRequest = _jellyfinApiClient.Items[itemId].Images[ImageType.Primary.ToString()].ToGetRequestInformation();
            Uri imageUri = _jellyfinApiClient.BuildUri(imageRequest);

            UserView view = new(itemId, item.Name, imageUri);

            UserViews.Add(view);
        }
    }
}