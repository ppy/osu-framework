// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    internal static class SWScale
    {
        private const string dllName = "x64/swscale-5.dll";

        [DllImport(dllName)]
        internal static extern IntPtr sws_getContext(int srcW, int srcH, AVPixelFormat srcFormat, int dstW, int dstH, AVPixelFormat dstFormat, int flags, IntPtr srcFilter, IntPtr dstFilter, IntPtr param);

        [DllImport(dllName)]
        internal static extern unsafe int sws_scale(IntPtr c, byte** srcSlice, int* srcStride, int srcSliceY, int srcSliceH, byte** dst, int* dstStride);

        [DllImport(dllName)]
        internal static extern void sws_freeContext(IntPtr swsContext);
    }
}
