// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AVStream
    {
        internal int index;

        internal int id;

        internal IntPtr codec;

        internal IntPtr priv_data;

        internal AVRational time_base;

        internal long start_time;

        internal long duration;

        internal long nb_frames;

        internal int disposition;

        internal int discard;

        internal AVRational sample_aspect_ratio;

        internal IntPtr metadata;

        internal AVRational avg_frame_rate;

        internal AVPacket attached_pic;

        internal IntPtr side_data;

        internal int nb_side_data;

        internal int event_flags;

        internal AVRational r_frame_rate;

        internal IntPtr recommended_encoder_configuration;

        internal AVCodecParameters* codecpar;

        internal IntPtr info;

        internal int pts_wrap_bits;

        internal long first_dts;

        internal long cur_dts;

        internal long last_IP_pts;

        internal int probe_packets;

        internal int codec_info_nb_frames;

        internal int need_parsing;

        internal IntPtr parser;

        internal IntPtr last_in_packet_buffer;

        internal AVProbeData probe_data;

        internal fixed long pts_buffer[17];

        internal IntPtr index_entries;

        internal int nb_index_entries;

        internal uint index_entries_allocated_size;

        internal int stream_identifier;

        internal long interleaver_chunk_size;

        internal long interleaver_chunk_duration;

        internal int request_probe;

        internal int skip_to_keyframe;

        internal int skip_samples;

        internal long start_skip_samples;

        internal long first_discard_sample;

        internal long last_discard_sample;

        internal int nb_decoded_frames;

        internal long mux_ts_offset;

        internal long pts_wrap_reference;

        internal int pts_wrap_behaviour;

        internal int update_initial_durations_done;

        internal fixed long pts_reorder_error[17];

        internal fixed byte pts_reorder_error_count[17];

        internal long last_dts_for_order_check;

        internal byte dts_ordered;

        internal byte dts_misordered;

        internal int inject_global_side_data;

        internal AVRational display_aspect_ratio;

        internal IntPtr @internal;
    }
}
