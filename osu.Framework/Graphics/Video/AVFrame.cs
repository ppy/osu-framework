// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AVFrame
    {
        // cannot used fixed because fixed-size buffers must not be pointer-types (compiler error)
        internal byte* data0, data1, data2, data3, data4, data5, data6, data7;

        internal fixed int linesize[8];

        internal byte** extended_data;

        internal int width;

        internal int height;

        internal int nb_samples;

        internal int format;

        internal int key_frame;

        internal int pict_type;

        internal AVRational sample_aspect_ratio;

        internal long pts;

        internal long pkt_pts;

        internal long pkt_dts;

        internal int coded_picture_number;

        internal int display_picture_number;

        internal int quality;

        internal IntPtr opaque;

        internal fixed ulong error[8];

        internal int repeat_pict;

        internal int interlaced_frame;

        internal int top_field_first;

        internal int palette_has_changed;

        internal long reordered_opaque;

        internal int sample_rate;

        internal ulong channel_layout;

        // cannot used fixed because fixed-size buffers must not be pointer-types (compiler error)
        internal IntPtr buf0, buf1, buf2, buf3, buf4, buf5, buf6, buf7;

        internal IntPtr extended_buf;

        internal int nb_extended_buf;

        internal IntPtr side_data;

        internal int nb_side_data;

        internal int flags;

        internal int color_range;

        internal int color_primaries;

        internal int color_trc;

        internal int colorspace;

        internal int chroma_location;

        internal long best_effort_timestamp;

        internal long pkt_pos;

        internal long pkt_duration;

        internal IntPtr metadata;

        internal int decode_error_flags;

        internal int channels;

        internal int pkt_size;

        internal sbyte* qscale_table;

        internal int qstride;

        internal int qscale_type;

        internal IntPtr qp_table_buf;

        internal IntPtr hw_frames_ctx;

        internal IntPtr opaque_ref;

        internal ulong crop_top;

        internal ulong crop_bottom;

        internal ulong crop_left;

        internal ulong crop_right;

        internal IntPtr private_ref;
    }
}
