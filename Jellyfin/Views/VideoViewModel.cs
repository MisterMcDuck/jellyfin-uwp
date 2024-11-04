using System;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Microsoft.Kiota.Abstractions;
using Windows.Media.Core;

namespace Jellyfin.Views;

public sealed class VideoViewModel : BindableBase
{
    private readonly JellyfinApiClient _jellyfinApiClient;

    private Guid? _videoId;

    public VideoViewModel(JellyfinApiClient jellyfinApiClient)
    {
        _jellyfinApiClient = jellyfinApiClient;
    }

    public MediaSource Source { get; set => SetProperty(ref field, value); }

    public void SetVideoId(Guid videoId)
    {
        _videoId = videoId;
        InitializeVideo();
    }

    private void InitializeVideo()
    {
        // Uninitialized
        if (_videoId is null)
        {
            return;
        }

        // TODO: Create play session?
        RequestInformation videoStreamRequest = _jellyfinApiClient.Videos[_videoId.Value].Stream.ToGetRequestInformation();
        Uri videoUri = _jellyfinApiClient.BuildUri(videoStreamRequest);

        Source = MediaSource.CreateFromUri(videoUri);
    }
}