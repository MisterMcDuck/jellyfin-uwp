using System;
using System.Linq;
using System.Security.Cryptography;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Kiota.Abstractions;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed class VideoViewModel : BindableBase
{
    private readonly record struct Codecs(string Video, string Audio);

    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly MediaPlayerElement _playerElement;
    private readonly DeviceProfileManager _deviceProfileManager;

    public VideoViewModel(
        JellyfinApiClient jellyfinApiClient,
        JellyfinSdkSettings sdkClientSettings,
        DeviceProfileManager deviceProfileManager,
        MediaPlayerElement playerElement)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _sdkClientSettings = sdkClientSettings;
        _deviceProfileManager = deviceProfileManager;
        _playerElement = playerElement;
    }

    public async void PlayVideo(Video.Parameters parameters)
    {
        Guid videoId = parameters.VideoId;

        // TODO: Caller should provide this? Or cache the item information app-wide?
        BaseItemDto item = await _jellyfinApiClient.Items[videoId].GetAsync();

        PlaybackInfoDto playbackInfo = new()
        {
            DeviceProfile = _deviceProfileManager.Profile,
        };

        // TODO: Video stream index? Or is that just the media source id?

        if (parameters.AudioStream is not null)
        {
            playbackInfo.AudioStreamIndex = parameters.AudioStream.Index;
        }

        if (parameters.SubtitleStream is not null)
        {
            playbackInfo.SubtitleStreamIndex = parameters.SubtitleStream.Index;
        }

        // TODO: Does this create a play session? If so, update progress properly.
        PlaybackInfoResponse playbackInfoResponse = await _jellyfinApiClient.Items[videoId].PlaybackInfo.PostAsync(playbackInfo);

        // TODO: Always the first? What if 0 or > 1?
        MediaSourceInfo mediaSourceInfo = playbackInfoResponse.MediaSources[0];

        Uri mediaUri;

        if (mediaSourceInfo.SupportsDirectPlay.GetValueOrDefault() || mediaSourceInfo.SupportsDirectStream.GetValueOrDefault())
        {
            RequestInformation request = _jellyfinApiClient.Videos[videoId].StreamWithContainer(mediaSourceInfo.Container).ToGetRequestInformation();
            mediaUri = _jellyfinApiClient.BuildUri(request);
        }
        else if (mediaSourceInfo.SupportsTranscoding.GetValueOrDefault())
        {
            if (!Uri.TryCreate(_sdkClientSettings.ServerUrl + mediaSourceInfo.TranscodingUrl, UriKind.Absolute, out mediaUri))
            {
                // TODO: Error handling
                return;
            }
        }
        else
        {
            // TODO: Default handling
            return;
        }

        AdaptiveMediaSourceCreationResult result = await AdaptiveMediaSource.CreateFromUriAsync(mediaUri);
        MediaSource mediaSource;
        if (result.Status == AdaptiveMediaSourceCreationStatus.Success)
        {
            AdaptiveMediaSource ams = result.MediaSource;
            ams.InitialBitrate = ams.AvailableBitrates.Max();

            mediaSource = MediaSource.CreateFromAdaptiveMediaSource(ams);
        }
        else
        {
            // Fall back to creating from the Uri directly
            // TODO: This doesn't seem to allow seeking.
            mediaSource = MediaSource.CreateFromUri(mediaUri);
        }

        _playerElement.SetMediaPlayer(new MediaPlayer());
        _playerElement.MediaPlayer.Source = mediaSource;
        _playerElement.MediaPlayer.Play();
    }

    public void StopVideo()
    {
        MediaPlayer player = _playerElement.MediaPlayer;
        if (player is not null)
        {
            player.Pause();

            IDisposable disposableSource = player.Source as IDisposable;
            player.Source = null;

            disposableSource?.Dispose();

            // This seems to throw. Can this be properly disposed?
            //player.Dispose();
        }
    }
}