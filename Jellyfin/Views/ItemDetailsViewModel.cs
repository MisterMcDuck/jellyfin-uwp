using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Microsoft.Kiota.Abstractions;

namespace Jellyfin.Views;

public sealed record MediaInfoItem(string Text);

public sealed class ItemDetailsViewModel : BindableBase
{
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    private BaseItemDto _item;

    public ItemDetailsViewModel(JellyfinApiClient jellyfinApiClient, NavigationManager navigationManager)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;
    }

    public string Name { get; set => SetProperty(ref field, value); }

    public Uri ImageUri { get; set => SetProperty(ref field, value); }

    public string Overview { get; set => SetProperty(ref field, value); }

    public string Tags { get; set => SetProperty(ref field, value); }

    public ObservableCollection<MediaInfoItem> MediaInfo { get; } = new();

    public async void HandleParameters(ItemDetails.Parameters parameters)
    {
        _item = await _jellyfinApiClient.Items[parameters.ItemId].GetAsync();

        Name = _item.Name;

        RequestInformation imageRequest = _jellyfinApiClient.Items[_item.Id.Value].Images[ImageType.Primary.ToString()].ToGetRequestInformation();
        ImageUri = _jellyfinApiClient.BuildUri(imageRequest);

        if (_item.ProductionYear.HasValue)
        {
            MediaInfo.Add(new MediaInfoItem(_item.ProductionYear.ToString()));
        }

        if (_item.RunTimeTicks.HasValue)
        {
            MediaInfo.Add(new MediaInfoItem(GetDisplayDuration(_item.RunTimeTicks.Value)));
        }

        if (!string.IsNullOrEmpty(_item.OfficialRating))
        {
            // TODO: Style correctly
            MediaInfo.Add(new MediaInfoItem(_item.OfficialRating));
        }

        if (_item.CommunityRating.HasValue)
        {
            // TODO: Style correctly
            MediaInfo.Add(new MediaInfoItem(_item.CommunityRating.Value.ToString("F1")));
        }

        if (_item.CriticRating.HasValue)
        {
            // TODO: Style correctly
            MediaInfo.Add(new MediaInfoItem(_item.CriticRating.Value.ToString()));
        }

        if (_item.RunTimeTicks.HasValue)
        {
            MediaInfo.Add(new MediaInfoItem(GetEndsAt(_item.RunTimeTicks.Value)));
        }

        Overview = _item.Overview;
        Tags = $"Tags: {string.Join(", ", _item.Tags)}";
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
            videoStream?.Index,
            audioStream?.Index,
            subtitleStream?.Index);
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
}