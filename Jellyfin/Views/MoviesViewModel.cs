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

public sealed record Movie(Guid Id, string Name, Uri ImageUri)
{
    // TODO: Create a better abstraction for this!
    public void Navigate(NavigationManager navigationManager)
    {
        navigationManager.NavigateToItemDetails(Id);
    }
}

public sealed partial class MoviesViewModel : ObservableObject
{
    private readonly JellyfinApiClient _jellyfinApiClient;

    private Guid? _collectionItemId;

    [ObservableProperty]
    private ObservableCollection<Movie> _movies;

    public MoviesViewModel(JellyfinApiClient jellyfinApiClient)
    {
        _jellyfinApiClient = jellyfinApiClient;

        InitializeMovies();
    }

    public void HandleParameters(Movies.Parameters parameters)
    {
        _collectionItemId = parameters.CollectionItemId;
        InitializeMovies();
    }

    // TODO: Singleton on NavigationManager?
    public ICommand NavigateToViewCommand { get; } = new NavigateToViewCommand();

    private async void InitializeMovies()
    {
        // Uninitialized
        if (_collectionItemId is null)
        {
            return;
        }

        // TODO: Paginate
        BaseItemDtoQueryResult result = await _jellyfinApiClient.Items.GetAsync(parameters =>
        {
            parameters.QueryParameters.ParentId = _collectionItemId;
            parameters.QueryParameters.SortBy = [ItemSortBy.SortName];
        });

        List<Movie> movies = new();
        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }
            Guid itemId = item.Id.Value;

            RequestInformation imageRequest = _jellyfinApiClient.Items[itemId].Images[ImageType.Primary.ToString()].ToGetRequestInformation();
            Uri imageUri = _jellyfinApiClient.BuildUri(imageRequest);

            Movie movie = new(itemId, item.Name, imageUri);

            movies.Add(movie);
        }

        Movies = new ObservableCollection<Movie>(movies);
    }
}