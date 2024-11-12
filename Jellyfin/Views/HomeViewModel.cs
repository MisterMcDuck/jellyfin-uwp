using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Jellyfin.Commands;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Kiota.Abstractions;

namespace Jellyfin.Views;

public sealed record UserView(
    Guid Id,
    BaseItemDto_CollectionType? CollectionType,
    string Name,
    Uri ImageUri)
{
    // TODO: Create a better abstraction for this!
    public void Navigate(NavigationManager navigationManager)
    {
        if (CollectionType.HasValue && CollectionType.Value == BaseItemDto_CollectionType.Movies)
        {
            navigationManager.NavigateToMovies(Id);
        }
    }
}

public sealed class HomeViewModel : BindableBase
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    public HomeViewModel(
        AppSettings appSettings,
        JellyfinApiClient jellyfinApiClient,
        NavigationManager navigationManager)
    {
        _appSettings = appSettings;
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;

        InitializeUserViews();
    }

    public ObservableCollection<UserView> UserViews { get; } = new();

    public void SelectServer()
    {
        _navigationManager.NavigateToServerSelection();
    }

    public void SignOut()
    {
        _appSettings.AccessToken = null;
        _navigationManager.NavigateToLogin();
    }

    // TODO: Singleton on NavigationManager?
    public ICommand NavigateToViewCommand { get; } = new NavigateToViewCommand();

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

            UserView view = new(itemId, item.CollectionType, item.Name, imageUri);

            UserViews.Add(view);
        }
    }
}