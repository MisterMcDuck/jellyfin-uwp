using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
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
    private readonly record struct Codecs(string Video, string Audio);

    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly MediaPlayerElement _playerElement;
    private readonly AppSettings _appSettings;
    private Guid _playingVideoId;
    private string _playingSessionId;
    private Timer _progressTimer;
    private readonly DeviceProfileManager _deviceProfileManager;

    public VideoViewModel(
        JellyfinApiClient jellyfinApiClient,
        JellyfinSdkSettings sdkClientSettings,
        DeviceProfileManager deviceProfileManager,
        MediaPlayerElement playerElement,
        AppSettings appSettings)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _sdkClientSettings = sdkClientSettings;
        _deviceProfileManager = deviceProfileManager;
        _playerElement = playerElement;
        _appSettings = appSettings;
    }

    public async void PlayVideo(Video.Parameters parameters)
    {
        Guid videoId = _playingVideoId = parameters.VideoId;

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
        _playingSessionId = playbackInfoResponse.PlaySessionId;

        // TODO: Always the first? What if 0 or > 1?
        MediaSourceInfo mediaSourceInfo = playbackInfoResponse.MediaSources[0];

        bool isAdaptive;
        Uri mediaUri;

        if (mediaSourceInfo.SupportsDirectPlay.GetValueOrDefault() || mediaSourceInfo.SupportsDirectStream.GetValueOrDefault())
        {
            RequestInformation request = _jellyfinApiClient.Videos[videoId].StreamWithContainer(mediaSourceInfo.Container).ToGetRequestInformation(
                parameters =>
                {
                    parameters.QueryParameters.Static = true;
                    parameters.QueryParameters.MediaSourceId = mediaSourceInfo.Id;

                    // TODO Copied from AppServices. Get this in a better way, shared by the Jellyfin SDK settings initialization.
                    parameters.QueryParameters.DeviceId = new EasClientDeviceInformation().Id.ToString();

                    if (mediaSourceInfo.ETag is not null)
                    {
                        parameters.QueryParameters.Tag = mediaSourceInfo.ETag;
                    }

                    if (mediaSourceInfo.LiveStreamId is not null)
                    {
                        parameters.QueryParameters.LiveStreamId = mediaSourceInfo.LiveStreamId;
                    }
                });
            mediaUri = _jellyfinApiClient.BuildUri(request);

            // TODO: The Jellyfin SDK doesn't appear to provide a way to add this query param.
            mediaUri = new Uri($"{mediaUri.AbsoluteUri}&api_key={_sdkClientSettings.AccessToken}");
            isAdaptive = false;
        }
        else if (mediaSourceInfo.SupportsTranscoding.GetValueOrDefault())
        {
            if (!Uri.TryCreate(_sdkClientSettings.ServerUrl + mediaSourceInfo.TranscodingUrl, UriKind.Absolute, out mediaUri))
            {
                // TODO: Error handling
                return;
            }

            isAdaptive = mediaSourceInfo.TranscodingSubProtocol == MediaSourceInfo_TranscodingSubProtocol.Hls;
        }
        else
        {
            // TODO: Default handling
            return;
        }

        MediaSource mediaSource;
        if (isAdaptive)
        {
            AdaptiveMediaSourceCreationResult result = await AdaptiveMediaSource.CreateFromUriAsync(mediaUri);
            if (result.Status == AdaptiveMediaSourceCreationStatus.Success)
            {
                AdaptiveMediaSource ams = result.MediaSource;
                ams.InitialBitrate = ams.AvailableBitrates.Max();

                mediaSource = MediaSource.CreateFromAdaptiveMediaSource(ams);
            }
            else
            {
                // Fall back to creating from the Uri directly
                mediaSource = MediaSource.CreateFromUri(mediaUri);
            }
        }
        else
        {
            mediaSource = MediaSource.CreateFromUri(mediaUri);
        }

        _playerElement.SetMediaPlayer(new MediaPlayer());
        _playerElement.MediaPlayer.Source = mediaSource;
        _playerElement.MediaPlayer.Play();
        _playerElement.MediaPlayer.MediaEnded += (mp, o) =>
        {
            _ = _jellyfinApiClient.UserPlayedItems[videoId].PostAsync();
        };
        _playerElement.MediaPlayer.PlaybackSession.PlaybackStateChanged += async (session, obj) =>
        {
            //are we playing media?
            if (_progressTimer == null)
            {
                return;
            }

            switch (session.PlaybackState)
            {
                case MediaPlaybackState.Paused:
                    _ = _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(new PlaybackProgressInfo()
                    {
                        ItemId = videoId,
                        MediaSourceId = videoId.ToString("N"),
                        AudioStreamIndex = playbackInfo.AudioStreamIndex,
                        SubtitleStreamIndex = playbackInfo.SubtitleStreamIndex,
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
                        AudioStreamIndex = playbackInfo.AudioStreamIndex,
                        SubtitleStreamIndex = playbackInfo.SubtitleStreamIndex,
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
            request.QueryParameters.AudioStreamIndex = playbackInfo.AudioStreamIndex;
            request.QueryParameters.SubtitleStreamIndex = playbackInfo.SubtitleStreamIndex;
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
                    request.QueryParameters.AudioStreamIndex = playbackInfo.AudioStreamIndex;
                    request.QueryParameters.SubtitleStreamIndex = playbackInfo.SubtitleStreamIndex;
                    request.QueryParameters.PlaySessionId = _playingSessionId;
                    request.QueryParameters.PositionTicks = currentTicks;

                });
                _ = _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(new PlaybackProgressInfo()
                {
                    CanSeek = true,
                    ItemId = videoId,
                    MediaSourceId = videoId.ToString("N"),
                    AudioStreamIndex = playbackInfo.AudioStreamIndex,
                    SubtitleStreamIndex = playbackInfo.SubtitleStreamIndex,
                    PlaySessionId = _playingSessionId,
                    PositionTicks = currentTicks,
                    SessionId = _appSettings.SessionId
                });
            }
        }, videoId, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public void StopVideo()
    {
        _progressTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _progressTimer = null;

        //StopVideo called from UI thread, so this is okay
        Int64 currentTicks = (int)_playerElement.MediaPlayer.PlaybackSession.Position.TotalMilliseconds * 10000;
        _ = ReportStoppedPlayer(currentTicks);

        MediaPlayer player = _playerElement.MediaPlayer;
        if (player is not null)
        {
            player.Pause();

            MediaSource mediaSource = (MediaSource)player.Source;

            // Detach components from each other
            _playerElement.SetMediaPlayer(null);
            player.Source = null;

            // Dispose components
            mediaSource.Dispose();
            player.Dispose();
        }
    }
    private async Task ReportStoppedPlayer(Int64 currentTicks)
    {
        await _jellyfinApiClient.Sessions.Playing.Stopped.PostAsync(new PlaybackStopInfo()
        {
            ItemId = _playingVideoId,
            PlaySessionId = _playingSessionId,
            PositionTicks = currentTicks,
            SessionId = _appSettings.SessionId
        });
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
}