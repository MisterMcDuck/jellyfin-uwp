using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Commands;
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

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly JellyfinApiClient _jellyfinApiClient;

    [ObservableProperty]
    private ObservableCollection<UserView> _userViews;

    public HomeViewModel(JellyfinApiClient jellyfinApiClient)
    {
        _jellyfinApiClient = jellyfinApiClient;

        InitializeUserViews();
    }

    // TODO: Singleton on NavigationManager?
    public ICommand NavigateToViewCommand { get; } = new NavigateToViewCommand();

    private async void InitializeUserViews()
    {
        List<UserView> userViews = new();

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

            userViews.Add(view);
        }

        UserViews = new ObservableCollection<UserView>(userViews);
    }
}