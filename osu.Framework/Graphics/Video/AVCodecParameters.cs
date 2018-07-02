// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AVCodecParameters
    {
        internal AVMediaType codec_type;

        internal AVCodecID codec_id;

        internal uint codec_tag;

        internal IntPtr extradata;

        internal int extradata_size;

        internal int format;

        internal long bit_rate;

        internal int bits_per_coded_sample;

        internal int bits_per_raw_sample;

        internal int profile;

        internal int level;

        internal int width;

        internal int height;

        internal AVRational sample_aspect_ratio;

        internal int field_order;

        internal int color_range;

        internal int color_primaries;

        internal int color_trc;

        internal int color_space;

        internal int chroma_location;

        internal int video_delay;

        internal ulong channel_layout;

        internal int channels;

        internal int sample_rate;

        internal int block_align;

        internal int frame_size;

        internal int initial_padding;

        internal int trailing_padding;

        internal int seek_preroll;
    }
}
