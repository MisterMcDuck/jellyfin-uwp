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
        string imageTag;

        // For some reason BackdropImageTags is a separate field
        if (imageType == ImageType.Backdrop)
        {
            if (item.BackdropImageTags.Count == 0)
            {
                return null;
            }

            imageTag = item.BackdropImageTags[0];
        }
        else
        {
            if (!item.ImageTags.AdditionalData.TryGetValue(imageTypeStr, out object imageTagObj))
            {
                return null;
            }

            imageTag = imageTagObj.ToString();
        }

        RequestInformation imageRequest = jellyfinApiClient.Items[item.Id.Value].Images[imageTypeStr].ToGetRequestInformation(
            request =>
            {
                request.QueryParameters.FillWidth = width;
                request.QueryParameters.FillHeight = height;
                request.QueryParameters.Quality = 96;
                request.QueryParameters.Tag = imageTag;
            });
        return jellyfinApiClient.BuildUri(imageRequest);
    }
}