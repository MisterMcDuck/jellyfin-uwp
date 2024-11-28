using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Jellyfin.Views;

public sealed partial class WebVideoViewModel : ObservableObject
{
    [ObservableProperty]
    private Uri _videoUri;

    public void HandleParameters(WebVideo.Parameters parameters)
    {
        VideoUri = parameters.VideoUri;
    }
}