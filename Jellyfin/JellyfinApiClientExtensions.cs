using System;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Kiota.Abstractions;

namespace Jellyfin;

public static class JellyfinApiClientExtensions
{
    public static Uri GetImageUri(
        this JellyfinApiClient jellyfinApiClient,
        BaseItemDto item,
        ImageType imageType,
        int width,
        int height)
    {
        string imageTypeStr = imageType.ToString();
        if (!item.ImageTags.AdditionalData.TryGetValue(imageTypeStr, out object imageTagObj))
        {
            return null;
        }

        RequestInformation imageRequest = jellyfinApiClient.Items[item.Id.Value].Images[imageTypeStr].ToGetRequestInformation(
            request =>
            {
                request.QueryParameters.FillWidth = width;
                request.QueryParameters.FillHeight = height;
                request.QueryParameters.Quality = 96;
                request.QueryParameters.Tag = imageTagObj.ToString();
            });
        return jellyfinApiClient.BuildUri(imageRequest);
    }
}