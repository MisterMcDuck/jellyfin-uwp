using System;
using System.Collections.ObjectModel;
using System.Text;
using Jellyfin.Common;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Kiota.Abstractions;

namespace Jellyfin.Views;

public sealed record MediaInfoItem(string Text);

public sealed class ItemDetailsViewModel : BindableBase
{
    private readonly JellyfinApiClient _jellyfinApiClient;

    private Guid _itemId;

    public ItemDetailsViewModel(JellyfinApiClient jellyfinApiClient)
    {
        _jellyfinApiClient = jellyfinApiClient;
    }

    public string Name { get; set => SetProperty(ref field, value); }

    public Uri ImageUri { get; set => SetProperty(ref field, value); }

    public string Overview { get; set => SetProperty(ref field, value); }

    public string Tags { get; set => SetProperty(ref field, value); }

    public ObservableCollection<MediaInfoItem> MediaInfo { get; } = new();

    public async void LoadItem(Guid itemId)
    {
        _itemId = itemId;

        BaseItemDto item = await _jellyfinApiClient.Items[itemId].GetAsync();

        Name = item.Name;

        RequestInformation imageRequest = _jellyfinApiClient.Items[itemId].Images[ImageType.Primary.ToString()].ToGetRequestInformation();
        ImageUri = _jellyfinApiClient.BuildUri(imageRequest);

        if (item.ProductionYear.HasValue)
        {
            MediaInfo.Add(new MediaInfoItem(item.ProductionYear.ToString()));
        }

        if (item.RunTimeTicks.HasValue)
        {
            MediaInfo.Add(new MediaInfoItem(GetDisplayDuration(item.RunTimeTicks.Value)));
        }

        if (!string.IsNullOrEmpty(item.OfficialRating))
        {
            // TODO: Style correctly
            MediaInfo.Add(new MediaInfoItem(item.OfficialRating));
        }

        if (item.CommunityRating.HasValue)
        {
            // TODO: Style correctly
            MediaInfo.Add(new MediaInfoItem(item.CommunityRating.Value.ToString("F1")));
        }

        if (item.CriticRating.HasValue)
        {
            // TODO: Style correctly
            MediaInfo.Add(new MediaInfoItem(item.CriticRating.Value.ToString()));
        }

        if (item.RunTimeTicks.HasValue)
        {
            MediaInfo.Add(new MediaInfoItem(GetEndsAt(item.RunTimeTicks.Value)));
        }

        Overview = item.Overview;
        Tags = $"Tags: {string.Join(", ", item.Tags)}";
    }

    public void Play()
    {
        App.AppFrame.Navigate(typeof(Video), _itemId);
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