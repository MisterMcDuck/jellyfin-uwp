using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Kiota.Abstractions;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

public sealed class VideoViewModel : BindableBase
{
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly MediaPlayerElement _playerElement;
    private readonly AppSettings _appSettings;
    private Guid _playingVideoId;
    private string _playingSessionId;
    private Timer _progressTimer;

    public VideoViewModel(JellyfinApiClient jellyfinApiClient, JellyfinSdkSettings sdkClientSettings, MediaPlayerElement playerElement, AppSettings appSettings)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _sdkClientSettings = sdkClientSettings;
        _playerElement = playerElement;
        _appSettings = appSettings;
    }

    public async void HandleParameters(Video.Parameters parameters)
    {
        Guid videoId = _playingVideoId = parameters.VideoId;

        PlaybackInfoResponse playbackInfo = await _jellyfinApiClient.Items[videoId].PlaybackInfo.GetAsync();
        _playingSessionId = playbackInfo.PlaySessionId;

        RequestInformation videoStreamRequest = _jellyfinApiClient.Videos[videoId].MainM3u8.ToGetRequestInformation(request =>
        {
            request.QueryParameters.MediaSourceId = videoId.ToString("N");

            // TODO Copied from AppServices. Get this in a better way, shared by the Jellyfin SDK settings initialization.
            request.QueryParameters.DeviceId = new EasClientDeviceInformation().Id.ToString();

            // TODO: These settings are just copied from what was observed from the web client. How to properly set these?
            request.QueryParameters.VideoCodec = "av1,hevc,h264";
            request.QueryParameters.AudioCodec = "aac,opus,flac";
            request.QueryParameters.VideoStreamIndex = parameters.VideoStreamIndex;
            request.QueryParameters.AudioStreamIndex = parameters.AudioStreamIndex;
            request.QueryParameters.SubtitleStreamIndex = parameters.SubtitleStreamIndex;
            request.QueryParameters.VideoBitRate = 139616000;
            request.QueryParameters.AudioBitRate = 384000;
            request.QueryParameters.MaxFramerate = 23.976025f;
            request.QueryParameters.TranscodingMaxAudioChannels = 2;
            request.QueryParameters.RequireAvc = false;
            request.QueryParameters.SegmentContainer = "mp4";
            request.QueryParameters.MinSegments = 1;
            request.QueryParameters.BreakOnNonKeyFrames = true;
            request.QueryParameters.PlaySessionId = playbackInfo.PlaySessionId;
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

            _playerElement.SetMediaPlayer(new MediaPlayer());
            _playerElement.MediaPlayer.Source = MediaSource.CreateFromAdaptiveMediaSource(ams);
            _playerElement.MediaPlayer.Play();
            

            ams.InitialBitrate = ams.AvailableBitrates.Max();

            _playerElement.MediaPlayer.MediaEnded += (mp, o) =>
            {
                _ = _jellyfinApiClient.UserPlayedItems[videoId].PostAsync();
            };
            _playerElement.MediaPlayer.PlaybackSession.PlaybackStateChanged += async (session, obj) =>
            {
                switch (session.PlaybackState)
                {
                    case MediaPlaybackState.Paused:
                        _ = _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(new PlaybackProgressInfo()
                        {
                            ItemId = videoId,
                            MediaSourceId = videoId.ToString("N"),
                            AudioStreamIndex = parameters.AudioStreamIndex,
                            SubtitleStreamIndex = parameters.SubtitleStreamIndex,
                            PlaySessionId = _playingSessionId,
                            PositionTicks = await GetCurrentTicks(),
                            SessionId = _appSettings.SessionId,
                            IsPaused = true
                        });
                        break;
                    case MediaPlaybackState.Playing:
                        _ = _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(new PlaybackProgressInfo()
                        {
                            CanSeek = true,
                            ItemId = videoId,
                            MediaSourceId = videoId.ToString("N"),
                            AudioStreamIndex = parameters.AudioStreamIndex,
                            SubtitleStreamIndex = parameters.SubtitleStreamIndex,
                            PlaySessionId = _playingSessionId,
                            PositionTicks = await GetCurrentTicks(),
                            SessionId = _appSettings.SessionId,
                            IsPaused = false
                        });
                        break;
                }
            };

            await _jellyfinApiClient.PlayingItems[videoId].PostAsync(request =>
            {
                request.QueryParameters.MediaSourceId = videoId.ToString("N");
                request.QueryParameters.AudioStreamIndex = parameters.AudioStreamIndex;
                request.QueryParameters.SubtitleStreamIndex = parameters.SubtitleStreamIndex;
                request.QueryParameters.PlaySessionId = _playingSessionId;
                // TODO: do we need to support sessions/sessionId?
                request.QueryParameters.CanSeek = _playerElement.MediaPlayer.PlaybackSession.CanSeek;

            });

            _progressTimer = new Timer(async (o) =>
            {
                if (await IsPlaying())
                {
                    Int64 currentTicks = await GetCurrentTicks();
                    _ = _jellyfinApiClient.PlayingItems[videoId].Progress.PostAsync(request =>
                    {
                        request.QueryParameters.MediaSourceId = videoId.ToString("N");
                        request.QueryParameters.AudioStreamIndex = parameters.AudioStreamIndex;
                        request.QueryParameters.SubtitleStreamIndex = parameters.SubtitleStreamIndex;
                        request.QueryParameters.PlaySessionId = _playingSessionId;
                        request.QueryParameters.PositionTicks = currentTicks;

                    });
                    _ = _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(new PlaybackProgressInfo()
                    {
                        CanSeek = true,
                        ItemId = videoId,
                        MediaSourceId = videoId.ToString("N"),
                        AudioStreamIndex = parameters.AudioStreamIndex,
                        SubtitleStreamIndex = parameters.SubtitleStreamIndex,
                        PlaySessionId = _playingSessionId,
                        PositionTicks = currentTicks,
                        SessionId = _appSettings.SessionId
                    });
                }
            }, videoId, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
        else
        {
            // Handle failure to create the adaptive media source
        }
    }

    private async Task<Int64> GetCurrentTicks()
    {
        Int64 currentTicks = 0;
        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => currentTicks = (int)_playerElement.MediaPlayer.PlaybackSession.Position.TotalMilliseconds * 10000);
        return currentTicks > 0 ? currentTicks : 0;
    }
    private async Task<bool> IsPlaying()
    {
        bool playing = false;
        await RunOnUIThread(() =>
        {
            playing = _playerElement.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
        });
        return playing;
    }
    private async Task RunOnUIThread(DispatchedHandler action)
    {
        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, action);
    }

    public async void LeavingPlayer()
    {
        _progressTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _progressTimer = null;
        Int64 currentTicks = await GetCurrentTicks();
        await _jellyfinApiClient.Sessions.Playing.Stopped.PostAsync(new PlaybackStopInfo()
        {
            ItemId = _playingVideoId,
            PlaySessionId = _playingSessionId,
            PositionTicks = currentTicks,
            SessionId = _appSettings.SessionId
        });
        /*
        await _jellyfinApiClient.PlayingItems[_playingVideoId].DeleteAsync(request =>
        {
            request.QueryParameters.MediaSourceId = _playingVideoId.ToString("N");
            request.QueryParameters.PlaySessionId = _playingSessionId;
            request.QueryParameters.PositionTicks = currentTicks;

        });
        */
    }
}