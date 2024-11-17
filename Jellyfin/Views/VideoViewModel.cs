using System;
using System.Linq;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Sdk.Generated.Videos.Item.MainM3u8;
using Jellyfin.Services;
using Microsoft.Kiota.Abstractions;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed class VideoViewModel : BindableBase
{
    private readonly record struct Codecs(string Video, string Audio);

    // TODO: Verify these values.
    // TODO: Detect codecs the device supports.
    /*
    private static readonly Dictionary<string, Codecs> CodecsForContainer = new(StringComparer.OrdinalIgnoreCase)
    {
        { "mkv", new Codecs("av1,hevc,h264", "aac,opus,flac") },
    };
    */

    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly MediaPlayerElement _playerElement;

    public VideoViewModel(JellyfinApiClient jellyfinApiClient, JellyfinSdkSettings sdkClientSettings, MediaPlayerElement playerElement)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _sdkClientSettings = sdkClientSettings;
        _playerElement = playerElement;
    }

    public async void HandleParameters(Video.Parameters parameters)
    {
        Guid videoId = parameters.VideoId;

        // TODO: Caller should provide this? Or cache the item information app-wide?
        BaseItemDto item = await _jellyfinApiClient.Items[videoId].GetAsync();

        // TODO: Create play session and set PlaySessionId
        RequestInformation videoStreamRequest = _jellyfinApiClient.Videos[videoId].MainM3u8.ToGetRequestInformation(request =>
        {
            request.QueryParameters.MediaSourceId = videoId.ToString("N");

            // TODO Copied from AppServices. Get this in a better way, shared by the Jellyfin SDK settings initialization.
            request.QueryParameters.DeviceId = new EasClientDeviceInformation().Id.ToString();

            // TODO: These settings are just copied from what was observed from the web client. How to properly set these?
            request.QueryParameters.VideoCodec = "av1,hevc,h264";
            request.QueryParameters.AudioCodec = "aac,opus,flac";

            if (parameters.VideoStream is not null)
            {
                request.QueryParameters.VideoStreamIndex = parameters.VideoStream.Index;

                ////request.QueryParameters.Level = parameters.VideoStream.Level.ToString();
                ////request.QueryParameters.MaxVideoBitDepth = parameters.VideoStream.BitDepth;
                ////request.QueryParameters.Profile = parameters.VideoStream.Profile;
            }

            if (parameters.AudioStream is not null)
            {
                request.QueryParameters.AudioStreamIndex = parameters.AudioStream.Index;
            }

            if (parameters.SubtitleStream is not null)
            {
                request.QueryParameters.SubtitleStreamIndex = parameters.SubtitleStream.Index;
                request.QueryParameters.SubtitleMethod = SubtitleDeliveryMethod.Encode;
            }

            request.QueryParameters.VideoBitRate = 139616000;
            request.QueryParameters.AudioBitRate = 384000;

            request.QueryParameters.TranscodingMaxAudioChannels = 2;
            request.QueryParameters.RequireAvc = false;
            request.QueryParameters.SegmentContainer = "mp4";
            request.QueryParameters.MinSegments = 1;
            request.QueryParameters.BreakOnNonKeyFrames = true;
            //request.QueryParameters.TranscodeReasons = "ContainerNotSupported, VideoCodecNotSupported, AudioCodecNotSupported";
        });

        Uri videoUri = _jellyfinApiClient.BuildUri(videoStreamRequest);

        // TODO: The Jellyfin SDK doesn't appear to provide a way to add this required query param.
        videoUri = new Uri($"{videoUri.AbsoluteUri}&api_key={_sdkClientSettings.AccessToken}");

        AdaptiveMediaSourceCreationResult result = await AdaptiveMediaSource.CreateFromUriAsync(videoUri);

        if (result.Status == AdaptiveMediaSourceCreationStatus.Success)
        {
            AdaptiveMediaSource ams = result.MediaSource;

            _playerElement.SetMediaPlayer(new MediaPlayer());
            _playerElement.MediaPlayer.Source = MediaSource.CreateFromAdaptiveMediaSource(ams);
            _playerElement.MediaPlayer.Play();

            ams.InitialBitrate = ams.AvailableBitrates.Max();
        }
        else
        {
            // Handle failure to create the adaptive media source
        }
    }
}