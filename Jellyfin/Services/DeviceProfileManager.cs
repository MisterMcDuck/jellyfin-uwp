using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.Render;

namespace Jellyfin.Services;

public sealed class DeviceProfileManager
{
    // This logic is adapted from the web client's browserDeviceProfile.js
    public async Task InitializeAsync()
    {
        CodecQuery codecQuery = new();

        HashSet<string> videoCodecGuids = new(StringComparer.OrdinalIgnoreCase);
        foreach (CodecInfo codecInfo in await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Decoder, string.Empty))
        {
            foreach (string subType in codecInfo.Subtypes)
            {
                videoCodecGuids.Add(subType);
            }
        }

        HashSet<string> audioCodecGuids = new(StringComparer.OrdinalIgnoreCase);
        foreach (CodecInfo codecInfo in await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Decoder, string.Empty))
        {
            foreach (string subType in codecInfo.Subtypes)
            {
                audioCodecGuids.Add(subType);
            }
        }

        const int maxBitrate = 120_000_000;
        uint audioChannelCount = await GetAudioChannelCountAsync();

        bool canPlayVp8 = videoCodecGuids.Contains(CodecSubtypes.VideoFormatVP80);
        bool canPlayVp9 = videoCodecGuids.Contains(CodecSubtypes.VideoFormatVP90);
        List<string> webmAudioCodecs = ["vorbis"];

        bool canPlayMkv = true; // Can we just assume true? UWP supports it generally.

        DeviceProfile profile = new()
        {
            MaxStreamingBitrate = maxBitrate,
            MaxStaticBitrate = 100_000_000,
            MusicStreamingTranscodingBitrate = Math.Min(maxBitrate, 384000),
            DirectPlayProfiles = [],
        };

        List<string> videoAudioCodecs = [];
        List<string> hlsInTsVideoAudioCodecs = [];
        List<string> hlsInFmp4VideoAudioCodecs = [];

        // TODO: Check codecs (any):
        // video/mp4; codecs="avc1.640029, mp4a.69
        // video/mp4; codecs="avc1.640029, mp4a.6B"
        // video/mp4; codecs="avc1.640029, mp3"
        bool supportsMp3VideoAudio = true;

        // TODO: Check codecs
        bool supportsMp2VideoAudio = true;

        // Xbox always renders at 1920 x 1080
        // TODO: For genericism should this be detected anyway?
        int maxVideoWidth = 1920;

        // TODO: Check codecs
        bool canPlayAacVideoAudio = true; // 'video/mp4; codecs="avc1.640029, mp4a.40.2"
        bool canPlayMp3VideoAudioInHls = true; // 'application/x-mpegurl; codecs="avc1.64001E, mp4a.40.34"' or 'application/vnd.apple.mpegURL; codecs="avc1.64001E, mp4a.40.34"'
        bool canPlayAc3VideoAudio = true; // 'audio/mp4; codecs="ac-3"'
        bool canPlayEac3VideoAudio = true; // 'audio/mp4; codecs="ec-3"'
        bool canPlayAc3VideoAudioInHls = true; // application/x-mpegurl; codecs="avc1.42E01E, ac-3 or application/vnd.apple.mpegURL; codecs="avc1.42E01E, ac-3"

        // Transcoding codec is the first in hlsVideoAudioCodecs.
        // Prefer AAC, MP3 to other codecs when audio transcoding.
        if (canPlayAacVideoAudio)
        {
            videoAudioCodecs.Add("aac");
            hlsInTsVideoAudioCodecs.Add("aac");
            hlsInFmp4VideoAudioCodecs.Add("aac");
        }

        if (supportsMp3VideoAudio)
        {
            videoAudioCodecs.Add("mp3");
            hlsInTsVideoAudioCodecs.Add("mp3");
        }

        if (canPlayMp3VideoAudioInHls)
        {
            hlsInFmp4VideoAudioCodecs.Add("mp3");
        }

        // For AC3/EAC3 remuxing.
        // Do not use AC3 for audio transcoding unless AAC and MP3 are not supported.
        if (canPlayAc3VideoAudio)
        {
            videoAudioCodecs.Add("ac3");

            if (canPlayEac3VideoAudio)
            {
                videoAudioCodecs.Add("eac3");
            }

            if (canPlayAc3VideoAudioInHls)
            {
                hlsInTsVideoAudioCodecs.Add("ac3");
                hlsInFmp4VideoAudioCodecs.Add("ac3");

                if (canPlayEac3VideoAudio)
                {
                    hlsInTsVideoAudioCodecs.Add("eac3");
                    hlsInFmp4VideoAudioCodecs.Add("eac3");
                }
            }
        }

        if (supportsMp2VideoAudio)
        {
            videoAudioCodecs.Add("mp2");
            hlsInTsVideoAudioCodecs.Add("mp2");
            hlsInFmp4VideoAudioCodecs.Add("mp2");
        }

        // TODO: Check codecs: 'video/mp4; codecs="dts-"' or 'video/mp4; codecs="dts+"'
        // Also: appSettings.enableDts() || options.supportsDts;
        bool supportsDts = true;
        if (supportsDts)
        {
            videoAudioCodecs.Add("dca");
            videoAudioCodecs.Add("dts");
        }

        // TODO: appSettings.enableTrueHd() || options.supportsTrueHd
        videoAudioCodecs.Add("truehd");

        // TODO: Use these values
        _ = audioChannelCount;
        _ = canPlayVp8;
        _ = canPlayVp9;
        _ = webmAudioCodecs;
        _ = canPlayMkv;
        _ = profile;
        _ = supportsMp2VideoAudio;
        _ = maxVideoWidth;


        // TODO: Jellyfin docs call out (via https://jellyfin.org/docs/general/clients/codec-support/):
        //       - MPEG-4 Part 2/SP (Simple Profile) (DivX?)
        //       - MPEG-4 Part 2/ASP (Advanced Simple Profile)
        //       - H.264 8Bit
        //       - H.264 10Bit
        //       - H.265 8Bit
        //       - H.265 10Bit
        //       - VP9
        //       - AV1
    }

    private async Task<uint> GetAudioChannelCountAsync()
    {
        string defaultAudioRenderDeviceId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
        DeviceInformation defaultAudioDevice = await DeviceInformation.CreateFromIdAsync(defaultAudioRenderDeviceId);

        AudioGraphSettings settings = new(AudioRenderCategory.Media)
        {
            PrimaryRenderDevice = defaultAudioDevice
        };
        CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

        if (result.Status != AudioGraphCreationStatus.Success)
        {
            // Audio graph creation failed. Just default to 2.
            return 2;
        }

        return result.Graph.EncodingProperties.ChannelCount;
    }

    // TODO: Better define
    private class DeviceProfile
    {
        public int MaxStreamingBitrate { get; set; }
        public int MaxStaticBitrate { get; set; }
        public int MusicStreamingTranscodingBitrate { get; set; }
        public List<string> DirectPlayProfiles { get; set; }
    }
}
