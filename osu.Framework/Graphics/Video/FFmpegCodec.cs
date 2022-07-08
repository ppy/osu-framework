// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    public sealed unsafe class FFmpegCodec
    {
        public readonly AVCodec* Pointer;
        public readonly Lazy<IReadOnlyList<AVHWDeviceType>> SupportedHwDeviceTypes;

        public AVCodecID Id => Pointer->id;

        public string Name => Marshal.PtrToStringAnsi((IntPtr)Pointer->name);

        public bool IsDecoder => ffmpeg.av_codec_is_decoder(Pointer) != 0;

        private readonly FFmpegFuncs ffmpeg;

        public FFmpegCodec(FFmpegFuncs ffmpeg, AVCodec* codec)
        {
            this.ffmpeg = ffmpeg;

            Pointer = codec;
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
