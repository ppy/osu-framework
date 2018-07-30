// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    internal static class AVUtil
    {
        private const string dll_name = "avutil-56";

        internal const int AV_TIME_BASE = 1000000;

        [DllImport(dll_name)]
        internal static extern unsafe AVFrame* av_frame_alloc();

        [DllImport(dll_name)]
        internal static extern unsafe void av_frame_free(AVFrame** frame);

        // siehe https://www.ffmpeg.org/doxygen/4.0/rational_8h_source.html#l00104
        // the method does not get exported, so we have to reimplement it here
        internal static double av_q2d(AVRational a) => a.num / (double)a.den;

        [DllImport(dll_name)]
        internal static extern int av_image_get_buffer_size(AVPixelFormat pix_fmt, int width, int height, int align = 1);

        [DllImport(dll_name)]
        internal static extern unsafe int av_image_fill_arrays(byte** dst_data, int* dst_linesize, IntPtr src, AVPixelFormat pix_fmt, int width, int height, int align);

        [DllImport(dll_name)]
        internal static extern unsafe byte* av_malloc(uint size);

        [DllImport(dll_name)]
        internal static extern unsafe void av_free(void* ptr);
    }
}
