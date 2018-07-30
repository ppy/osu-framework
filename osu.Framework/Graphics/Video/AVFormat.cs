// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    internal static class AVFormat
    {
        private const string dll_name = "avformat-58";

        internal const int AVSEEK_FLAG_BACKWARD = 1;

        internal const int AVSEEK_SIZE = 0x10000;

        internal unsafe delegate int ReadPacketCallback(void* opaque, byte* buf, int buf_size);

        internal unsafe delegate long SeekCallback(void* opaque, long offset, int whence);

        [DllImport(dll_name)]
        internal static extern unsafe AVFormatContext* avformat_alloc_context();

        [DllImport(dll_name)]
        internal static extern unsafe AVIOContext* avio_alloc_context(byte* buffer, int buffer_size, int write_flag, void* opaque, [MarshalAs(UnmanagedType.FunctionPtr)]ReadPacketCallback read_packet, IntPtr write_packet, [MarshalAs(UnmanagedType.FunctionPtr)]SeekCallback seek);

        [DllImport(dll_name)]
        internal static extern unsafe int avformat_open_input(AVFormatContext** ps, string url, IntPtr format, IntPtr options);

        [DllImport(dll_name)]
        internal static extern unsafe void avformat_close_input(AVFormatContext** s);

        [DllImport(dll_name)]
        internal static extern int avformat_find_stream_info(IntPtr ic, IntPtr options);

        [DllImport(dll_name)]
        internal static extern unsafe int av_read_frame(AVFormatContext* s, AVPacket* pkt);

        [DllImport(dll_name)]
        internal static extern unsafe int av_seek_frame(AVFormatContext* s, int stream_index, long timestamp, int flags);
    }
}
