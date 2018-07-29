// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AVIOContext
    {
        internal IntPtr av_class;

        internal byte* buffer;

        internal int buffer_size;

        internal byte* buf_ptr;

        internal byte* buf_end;

        internal void* opaque;

        internal IntPtr read_packet;

        internal IntPtr write_packet;

        internal IntPtr seek;

        internal long pos;

        internal int eof_reached;

        internal int write_flag;

        internal int max_packet_size;

        internal long checksum;

        internal byte* checksum_ptr;

        internal IntPtr update_checksum;

        internal int error;

        internal IntPtr read_pause;

        internal IntPtr read_seek;

        internal int seekable;

        internal long maxsize;

        internal int direct;

        internal long bytes_read;

        internal int seek_count;

        internal int writeout_count;

        internal int orig_buffer_size;

        internal int short_seek_threshold;

        internal char* protocol_whitelist;

        internal char* protocol_blacklist;

        internal IntPtr write_data_type;

        internal int ignore_boundary_point;

        internal int current_type;

        internal long last_time;

        internal IntPtr short_seek_get;

        internal long written;

        internal byte* buf_ptr_max;

        internal int min_packet_size;
    }
}
