using System;
using System.Collections.ObjectModel;
using Jellyfin.Commands;
using System.Windows.Input;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Kiota.Abstractions;
using Jellyfin.Services;

namespace Jellyfin.Views;

public sealed record Movie(Guid Id, string Name, Uri ImageUri)
{
    // TODO: Create a better abstraction for this!
    public void Navigate(NavigationManager navigationManager)
    {
        navigationManager.NavigateToItemDetails(Id);
    }
}

public sealed class MoviesViewModel : BindableBase
{
    private readonly JellyfinApiClient _jellyfinApiClient;

    private Guid? _collectionItemId;

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

    public ObservableCollection<Movie> Movies { get; } = new();

    // TODO: Singleton on NavigationManager?
    public ICommand NavigateToViewCommand { get; } = new NavigateToViewCommand();

    private async void InitializeMovies()
    {
        Movies.Clear();

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

            Movies.Add(movie);
        }
    }
}