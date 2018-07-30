// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AVFormatContext
    {
        internal IntPtr av_class;

        internal IntPtr iformat;

        internal IntPtr oformat;

        internal IntPtr priv_data;

        internal AVIOContext* pb;

        internal int ctx_flags;

        internal uint nb_streams;

        internal AVStream** streams;

        private fixed byte filename[1024];

        internal char* url;

        internal long start_time;

        internal long duration;

        internal long bit_rate;

        internal uint packet_size;

        internal int max_delay;

        internal int flags;

        internal long probesize;

        internal long max_analyze_duration;

        internal byte* key;

        internal int keylen;

        internal uint nb_programs;

        internal IntPtr programs;

        internal AVCodecID video_codec_id;

        internal AVCodecID audio_codec_id;

        internal AVCodecID subtitle_codec_id;

        internal uint max_index_size;

        internal uint max_picture_buffer;

        internal uint nb_chapters;

        internal IntPtr chapters;

        internal IntPtr metadata;

        internal long start_time_realtime;

        internal int fps_probe_size;

        internal int error_recognition;

        internal IntPtr interrupt_callback;

        internal int debug;

        internal long max_interleave_delta;

        internal int strict_std_compliance;

        internal int event_flags;

        internal int max_ts_probe;

        internal int avoid_negative_ts;

        internal int ts_id;

        internal int audio_preload;

        internal int max_chunk_duration;

        internal int max_chunk_size;

        internal int use_wallclock_as_timestamps;

        internal int avio_flags;

        internal int duration_estimation_method;

        internal long skip_initial_bytes;

        internal uint correct_ts_overflow;

        internal int seek2any;

        internal int flush_packets;

        internal int probe_score;

        internal int format_probesize;

        internal char* codec_whitelist;

        internal char* format_whitelist;

        internal IntPtr @internal;

        internal int io_repositioned;

        internal IntPtr video_codec;

        internal IntPtr audio_codec;

        internal IntPtr subtitle_codec;

        internal IntPtr data_codec;

        internal int metadata_header_padding;

        internal IntPtr opaque;

        internal IntPtr control_message_cb;

        internal long output_ts_offset;

        internal byte* dump_separator;

        internal AVCodecID data_codec_id;

        internal char* protocol_whitelist;

        internal IntPtr io_open;

        internal IntPtr io_close;

        internal char* protocol_blacklist;

        internal int max_streams;
    }
}
