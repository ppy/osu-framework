// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    public unsafe partial class VideoDecoder
    {
        private readonly struct FFmpegCodec
        {
            public readonly AVCodec* Pointer;
            public AVCodecID Id => Pointer->id;
            public readonly bool IsDecoder;
            public readonly Lazy<IReadOnlyList<AVHWDeviceType>> SupportedHwDeviceTypes;

            public FFmpegCodec(AVCodec* codec, FFmpegFuncs ffmpeg)
            {
                Pointer = codec;
                IsDecoder = ffmpeg.av_codec_is_decoder(codec) != 0;

                SupportedHwDeviceTypes = new Lazy<IReadOnlyList<AVHWDeviceType>>(() =>
                {
                    var list = new List<AVHWDeviceType>();

                    int i = 0;

                    while (true)
                    {
                        var hwCfg = ffmpeg.avcodec_get_hw_config(codec, i);
                        if (hwCfg == null) break;

                        list.Add(hwCfg->device_type);

                        i++;
                    }

                    return list;
                });
            }
        }
    }
}
