using System;
using System.Linq;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Microsoft.Kiota.Abstractions;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed class VideoViewModel : BindableBase
{
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly JellyfinSdkSettings _sdkClientSettings;

    public VideoViewModel(JellyfinApiClient jellyfinApiClient, JellyfinSdkSettings sdkClientSettings)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _sdkClientSettings = sdkClientSettings;
    }

    public async void PlayVideo(MediaPlayerElement playerElement, Guid videoId)
    {
        // TODO: Create play session and set PlaySessionId
        RequestInformation videoStreamRequest = _jellyfinApiClient.Videos[videoId].MainM3u8.ToGetRequestInformation(request =>
        {
            request.QueryParameters.MediaSourceId = videoId.ToString("N");

            // TODO Copied from AppServices. Get this in a better way, shared by the Jellyfin SDK settings initialization.
            request.QueryParameters.DeviceId = new EasClientDeviceInformation().Id.ToString();

            // TODO: These settings are just copied from what was observed from the web client. How to properly set these?
            request.QueryParameters.VideoCodec = "av1,hevc,h264";
            request.QueryParameters.AudioCodec = "aac,opus,flac";
            request.QueryParameters.AudioStreamIndex = 1;
            request.QueryParameters.VideoBitRate = 139616000;
            request.QueryParameters.AudioBitRate = 384000;
            request.QueryParameters.MaxFramerate = 23.976025f;
            request.QueryParameters.TranscodingMaxAudioChannels = 2;
            request.QueryParameters.RequireAvc = false;
            request.QueryParameters.SegmentContainer = "mp4";
            request.QueryParameters.MinSegments = 1;
            request.QueryParameters.BreakOnNonKeyFrames = true;
            //request.QueryParameters.Level = "3";
            //request.QueryParameters.VideoBitRate = 8;
            //request.QueryParameters.Profile = "advanced";
            //request.QueryParameters.TranscodeReasons = "ContainerNotSupported, VideoCodecNotSupported, AudioCodecNotSupported";
        });

        Uri videoUri = _jellyfinApiClient.BuildUri(videoStreamRequest);

        // TODO: The Jellyfin SDK doesn't appear to provide a way to add this required query param.
        videoUri = new Uri($"{videoUri.AbsoluteUri}&api_key={_sdkClientSettings.AccessToken}");

        AdaptiveMediaSourceCreationResult result = await AdaptiveMediaSource.CreateFromUriAsync(videoUri);

        if (result.Status == AdaptiveMediaSourceCreationStatus.Success)
        {
            AdaptiveMediaSource ams = result.MediaSource;

            playerElement.SetMediaPlayer(new MediaPlayer());
            playerElement.MediaPlayer.Source = MediaSource.CreateFromAdaptiveMediaSource(ams);
            playerElement.MediaPlayer.Play();

            ams.InitialBitrate = ams.AvailableBitrates.Max();
        }
        else
        {
            // Handle failure to create the adaptive media source
        }
    }
}