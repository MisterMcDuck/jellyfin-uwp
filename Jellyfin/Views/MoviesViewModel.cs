using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;

namespace Jellyfin.Views;

public sealed record Movie(Guid Id, string Name, Uri ImageUri);

public sealed partial class MoviesViewModel : ObservableObject
{
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    private Guid? _collectionItemId;

    [ObservableProperty]
    private ObservableCollection<Movie> _movies;

    public MoviesViewModel(JellyfinApiClient jellyfinApiClient, NavigationManager navigationManager)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;

        InitializeMovies();
    }

    public void HandleParameters(Movies.Parameters parameters)
    {
        _collectionItemId = parameters.CollectionItemId;
        InitializeMovies();
    }

    private async void InitializeMovies()
    {
        // Uninitialized
        if (_collectionItemId is null)
        {
            return;
        }

        // TODO: Paginate?
        BaseItemDtoQueryResult result = await _jellyfinApiClient.Items.GetAsync(parameters =>
        {
            parameters.QueryParameters.ParentId = _collectionItemId;
            parameters.QueryParameters.SortBy = [ItemSortBy.SortName, ItemSortBy.ProductionYear];
            parameters.QueryParameters.SortOrder = [SortOrder.Ascending];
            parameters.QueryParameters.IncludeItemTypes = [BaseItemKind.Movie];
            parameters.QueryParameters.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.MediaSourceCount];
            parameters.QueryParameters.ImageTypeLimit = 1;
            parameters.QueryParameters.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Banner, ImageType.Thumb];
        });

        List<Movie> movies = new();
        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }

            Guid itemId = item.Id.Value;
            Uri imageUri = _jellyfinApiClient.GetImageUri(item, ImageType.Primary, Constants.CardImageWidth, Constants.TallCardImageHeight);
            Movie movie = new(itemId, item.Name, imageUri);

            movies.Add(movie);
        }

        Movies = new ObservableCollection<Movie>(movies);
    }

    [RelayCommand]
    private void NavigateToMovie(Movie movie)
    {
        _navigationManager.NavigateToItemDetails(movie.Id);
    }

}