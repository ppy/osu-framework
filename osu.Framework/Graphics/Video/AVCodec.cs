// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// See documentation for libavcodec, here: https://www.ffmpeg.org/doxygen/trunk/group__libavc.html
    /// </summary>
    internal static class AVCodec
    {
        private const string dllName = "x64/avcodec-58.dll";

        internal const int AV_INPUT_BUFFER_PADDING_SIZE = 64;

        internal const long AV_NOPTS_VALUE = unchecked((long)0x8000000000000000);

        [DllImport(dllName)]
        internal static extern IntPtr avcodec_find_decoder(AVCodecID id);

        [DllImport(dllName)]
        internal static extern int avcodec_open2(IntPtr context, IntPtr codec, IntPtr options);

        [DllImport(dllName)]
        internal static extern unsafe AVPacket* av_packet_alloc();

        [DllImport(dllName)]
        internal static extern unsafe void av_packet_free(AVPacket** packet);

        [DllImport(dllName)]
        internal static extern unsafe void av_packet_unref(AVPacket* pkt);

        [DllImport(dllName)]
        internal static extern unsafe int avcodec_send_packet(IntPtr avctx, AVPacket* avpkt);

        [DllImport(dllName)]
        internal static extern unsafe int avcodec_receive_frame(IntPtr avctx, AVFrame* frame);
    }
}
