using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Kiota.Abstractions;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Jellyfin.Views;

public sealed record MediaInfoItem(string Text);

public sealed partial class ItemDetailsViewModel : ObservableObject
{
    private static readonly SolidColorBrush OnBrush = new SolidColorBrush(Colors.Red);
    private static readonly SolidColorBrush OffBrush = new SolidColorBrush(Colors.White);

    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    private BaseItemDto _item;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private Uri _imageUri;

    [ObservableProperty]
    private ObservableCollection<MediaInfoItem> _mediaInfo;

    [ObservableProperty]
    private string _overview;

    [ObservableProperty]
    private string _tags;

    [ObservableProperty]
    private bool _isPlayed;

    [ObservableProperty]
    private Brush _playStateBrush;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private Brush _favoriteBrush;

    public ItemDetailsViewModel(JellyfinApiClient jellyfinApiClient, NavigationManager navigationManager)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;
    }

    public async void HandleParameters(ItemDetails.Parameters parameters)
    {
        _item = await _jellyfinApiClient.Items[parameters.ItemId].GetAsync();

        Name = _item.Name;
        ImageUri = _jellyfinApiClient.GetImageUri(_item, ImageType.Primary, 150, 225);

        List<MediaInfoItem> mediaInfo = new();
        if (_item.ProductionYear.HasValue)
        {
            mediaInfo.Add(new MediaInfoItem(_item.ProductionYear.ToString()));
        }

        if (_item.RunTimeTicks.HasValue)
        {
            mediaInfo.Add(new MediaInfoItem(GetDisplayDuration(_item.RunTimeTicks.Value)));
        }

        if (!string.IsNullOrEmpty(_item.OfficialRating))
        {
            // TODO: Style correctly
            mediaInfo.Add(new MediaInfoItem(_item.OfficialRating));
        }

        if (_item.CommunityRating.HasValue)
        {
            // TODO: Style correctly
            mediaInfo.Add(new MediaInfoItem(_item.CommunityRating.Value.ToString("F1")));
        }

        if (_item.CriticRating.HasValue)
        {
            // TODO: Style correctly
            mediaInfo.Add(new MediaInfoItem(_item.CriticRating.Value.ToString()));
        }

        if (_item.RunTimeTicks.HasValue)
        {
            mediaInfo.Add(new MediaInfoItem(GetEndsAt(_item.RunTimeTicks.Value)));
        }

        MediaInfo = new ObservableCollection<MediaInfoItem>(mediaInfo);
        Overview = _item.Overview;
        Tags = $"Tags: {string.Join(", ", _item.Tags)}";

        UpdateUserData();
    }

    public void Play()
    {
        // TODO: Move to user-selectable drop-downs
        MediaStream videoStream = null;
        MediaStream audioStream = null;
        MediaStream subtitleStream = null;
        foreach (MediaStream mediaStream in _item.MediaStreams)
        {
            switch (mediaStream.Type)
            {
                case MediaStream_Type.Video:
                {
                    if (videoStream is null || mediaStream.IsDefault.GetValueOrDefault())
                    {
                        videoStream = mediaStream;
                    }

                    break;
                }
                case MediaStream_Type.Audio:
                {
                    if (audioStream is null || mediaStream.IsDefault.GetValueOrDefault())
                    {
                        audioStream = mediaStream;
                    }

                    break;
                }
                case MediaStream_Type.Subtitle:
                {
                    if (subtitleStream is null || mediaStream.IsDefault.GetValueOrDefault())
                    {
                        subtitleStream = mediaStream;
                    }

                    break;
                }
            }
        }

        _navigationManager.NavigateToVideo(
            _item.Id.Value,
            videoStream,
            audioStream,
            subtitleStream);
    }

    public async void PlayTrailer()
    {
        if (_item.LocalTrailerCount > 0)
        {
            List<BaseItemDto> localTrailers = await _jellyfinApiClient.Items[_item.Id.Value].LocalTrailers.GetAsync();
            if (localTrailers.Count > 0)
            {
                // TODO play all the trailers instead of just the first?
                _navigationManager.NavigateToVideo(
                    localTrailers[0].Id.Value,
                    videoStream: null,
                    audioStream: null,
                    subtitleStream: null);
                return;
            }
        }

        if (_item.RemoteTrailers.Count > 0)
        {
            // TODO play all the trailers instead of just the first?
            Uri videoUri = GetWebVideoUri(_item.RemoteTrailers[0].Url);

            _navigationManager.NavigateToWebVideo(videoUri);
            return;
        }
    }

    public async void TogglePlayed()
    {
        _item.UserData = _item.UserData.Played.GetValueOrDefault()
            ? await _jellyfinApiClient.UserPlayedItems[_item.Id.Value].DeleteAsync()
            : await _jellyfinApiClient.UserPlayedItems[_item.Id.Value].PostAsync();
        UpdateUserData();
    }

    public async void ToggleFavorite()
    {
        _item.UserData = _item.UserData.IsFavorite.GetValueOrDefault()
            ? await _jellyfinApiClient.UserFavoriteItems[_item.Id.Value].DeleteAsync()
            : await _jellyfinApiClient.UserFavoriteItems[_item.Id.Value].PostAsync();
        UpdateUserData();
    }

    // Return a string in '{}h {}m' format for duration.
    private string GetDisplayDuration(long ticks)
    {
        int totalMinutes = (int)Math.Round(ticks / 600000000d);
        if (totalMinutes == 0)
        {
            totalMinutes = 1;
        }

        double totalHours = totalMinutes / 60;
        double remainderMinutes = totalMinutes % 60;

        StringBuilder sb = new();
        if (totalHours > 0)
        {
            sb.Append(totalHours);
            sb.Append("h ");
        }

        sb.Append(remainderMinutes);
        sb.Append('m');

        return sb.ToString();
    }

    private string GetEndsAt(long ticks)
    {
        DateTime endDate = DateTime.Now + TimeSpan.FromTicks(ticks);
        return $"Ends at {endDate:t}";
    }

    private void UpdateUserData()
    {
        IsPlayed = _item.UserData.Played.GetValueOrDefault();
        PlayStateBrush = IsPlayed ? OnBrush : OffBrush;

        IsFavorite = _item.UserData.IsFavorite.GetValueOrDefault();
        FavoriteBrush = IsFavorite ? OnBrush : OffBrush;
    }

    private Uri GetWebVideoUri(string url)
    {
        Match match = YouTubeRegex.Match(url);
        if (match.Success)
        {
            string youtubeBaseUrl = match.Groups["urlBase"].Value;
            string youtubeVideoId = match.Groups["id"].Value;

            // Use the embed url with autoplay enabled.
            return new($"{youtubeBaseUrl}/embed/{youtubeVideoId}?rel=0&autoplay=1");
        }

        // Fallback to the full url.
        return new Uri(url);
    }

    private static readonly Regex YouTubeRegex = new(
        @"(?<urlBase>https://www.youtube.com)/watch\?v=(?<id>[^&]+)",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);
}