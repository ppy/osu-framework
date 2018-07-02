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

        // deprecated, but it still exists. might need to remove this if AVFormat gets udpated.
        // also, this is dumb and retarded and bad, but we can't have this struct be a pointer and use marshalling
        // at the same time. opting for pointers here, since that makes use of AVFormat easier generally
        private long filename0, filename1, filename2, filename3, filename4, filename5, filename6, filename7, filename8, filename9, filename10, filename11, filename12, filename13, filename14, filename15, filename16, filename17, filename18, filename19, filename20, filename21, filename22, filename23, filename24, filename25, filename26, filename27, filename28, filename29, filename30, filename31, filename32, filename33, filename34, filename35, filename36, filename37, filename38, filename39, filename40, filename41, filename42, filename43, filename44, filename45, filename46, filename47, filename48, filename49, filename50, filename51, filename52, filename53, filename54, filename55, filename56, filename57, filename58, filename59, filename60, filename61, filename62, filename63, filename64, filename65, filename66, filename67, filename68, filename69, filename70, filename71, filename72, filename73, filename74, filename75, filename76, filename77, filename78, filename79, filename80, filename81, filename82, filename83, filename84, filename85, filename86, filename87, filename88, filename89, filename90, filename91, filename92, filename93, filename94, filename95, filename96, filename97, filename98, filename99, filename100, filename101, filename102, filename103, filename104, filename105, filename106, filename107, filename108, filename109, filename110, filename111, filename112, filename113, filename114, filename115, filename116, filename117, filename118, filename119, filename120, filename121, filename122, filename123, filename124, filename125, filename126, filename127;

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
