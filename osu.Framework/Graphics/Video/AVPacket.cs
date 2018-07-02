// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AVPacket
    {
        internal IntPtr buf;

        internal long pts;

        internal long dts;

        internal IntPtr data;

        internal int size;

        internal int stream_index;

        internal int flags;

        internal IntPtr side_data;

        internal int side_data_elems;

        internal long duration;

        internal long pos;

        internal long convergence_duration;
    }
}
