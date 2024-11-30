using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;

namespace Jellyfin.Views;

public sealed record UserView(
    Guid Id,
    BaseItemDto_CollectionType? CollectionType,
    string Name,
    Uri ImageUri,
    BaseItemDto_Type? ItemType = null,
    String AdditionalInfo = "",
    int Progress = 0
    );
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    [ObservableProperty]
    private ObservableCollection<UserView> _userViews;

    [ObservableProperty]
    private ObservableCollection<UserView> _resume;

    [ObservableProperty]
    private ObservableCollection<UserView> _nextUp;

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
            Uri imageUri = _jellyfinApiClient.GetImageUri(item, ImageType.Primary, Constants.CardImageWidth, Constants.WideCardImageHeight);
            UserView view = new(itemId, item.CollectionType, item.Name, imageUri);

            userViews.Add(view);
        }

        UserViews = new ObservableCollection<UserView>(userViews);

        var resume = await _jellyfinApiClient.UserItems.Resume.GetAsync();
        if (resume.Items.Any())
        {
            Resume = new ObservableCollection<UserView>();
        }

        foreach (var item in resume.Items)
        {
            Guid itemId = item.Id.Value;

            Uri imageUri = _jellyfinApiClient.GetImageUri(item, ImageType.Primary, Constants.CardImageWidth, Constants.WideCardImageHeight);
            String additional, name;
            if (item.Type == BaseItemDto_Type.Movie)
            {
                name = item.Name;
                additional = item.ProductionYear.ToString();
            }
            else
            {
                name = item.SeriesName;
                additional = $"S{item.ParentIndexNumber}:E{item.IndexNumber} - {item.Name}";
            }
            int progress = (int)Math.Round(item.UserData.PlayedPercentage.Value, 0);
            UserView view = new(itemId, item.CollectionType, name, imageUri, item.Type.Value, additional, progress);
            Resume.Add(view);
        }

        var nextUpResult = await _jellyfinApiClient.Shows.NextUp.GetAsync();
        if (nextUpResult.Items.Any())
        {
            NextUp = new ObservableCollection<UserView>();
        }
        foreach (var item in nextUpResult.Items)
        {
            Guid itemId = item.Id.Value;

            Uri imageUri = _jellyfinApiClient.GetImageUri(item, ImageType.Primary, Constants.CardImageWidth, Constants.WideCardImageHeight);
            String subName = item.CollectionType == BaseItemDto_CollectionType.Movies ? item.ProductionYear.ToString() : $"S{item.ParentIndexNumber}:E{item.IndexNumber} - {item.Name}";

            UserView view = new(itemId, item.CollectionType, item.SeriesName, imageUri, item.Type.Value, subName);
            NextUp.Add(view);
        }
        //TODO: Do we want this? 
        /*
        foreach (var userView in UserViews)
        {
            var recentlyAdded = await _jellyfinApiClient.Items.Latest.GetAsync(request =>
            {
                request.QueryParameters.Limit = 16;
                request.QueryParameters.ParentId = userView.Id;
            });
        }
        */
    }

    [RelayCommand]
    private void NavigateToUserView(UserView userView)
    {
        if (userView.CollectionType.HasValue && userView.CollectionType.Value == BaseItemDto_CollectionType.Movies)
        {
            _navigationManager.NavigateToMovies(userView.Id);
        }
        else if (userView.ItemType == BaseItemDto_Type.Movie)
        {
            _navigationManager.NavigateToItemDetails(userView.Id);
        }

    }
}