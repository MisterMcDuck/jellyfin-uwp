using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Kiota.Abstractions;

namespace Jellyfin.Views;

public sealed record Movie(Guid Id, string Name, Uri ImageUri)
{
    public void Select()
    {
        // TODO
    }
}

public sealed class MoviesViewModel : BindableBase
{
    private readonly JellyfinApiClient _jellyfinApiClient;

    private Guid? _parentId;

    public MoviesViewModel(JellyfinApiClient jellyfinApiClient)
    {
        _jellyfinApiClient = jellyfinApiClient;

        InitializeMovies();
    }

    public void SetParentId(Guid parentId)
    {
        _parentId = parentId;
        InitializeMovies();
    }

    public ObservableCollection<Movie> Movies { get; } = new();

    private async void InitializeMovies()
    {
        Movies.Clear();

        // Uninitialized
        if (_parentId is null)
        {
            return;
        }

        // TODO: Paginate
        BaseItemDtoQueryResult result = await _jellyfinApiClient.Items.GetAsync(parameters =>
        {
            parameters.QueryParameters.ParentId = _parentId;
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