using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Kiota.Abstractions;

namespace Jellyfin.Views;

public sealed record UserView(
    Guid Id,
    BaseItemDto_CollectionType? CollectionType,
    string Name,
    Uri ImageUri);

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    [ObservableProperty]
    private ObservableCollection<UserView> _userViews;

    public HomeViewModel(JellyfinApiClient jellyfinApiClient, NavigationManager navigationManager)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;

        InitializeUserViews();
    }

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

    [RelayCommand]
    private void NavigateToUserView(UserView userView)
    {
        if (userView.CollectionType.HasValue && userView.CollectionType.Value == BaseItemDto_CollectionType.Movies)
        {
            _navigationManager.NavigateToMovies(userView.Id);
        }
    }
}