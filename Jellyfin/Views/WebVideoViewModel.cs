using System;
using Jellyfin.Common;

namespace Jellyfin.Views;

public sealed class WebVideoViewModel : BindableBase
{
    public Uri VideoUri { get; set => SetProperty(ref field, value); }

    public void HandleParameters(WebVideo.Parameters parameters)
    {
        VideoUri = parameters.VideoUri;
    }
}