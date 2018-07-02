// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Video
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AVProbeData
    {
        internal byte* filename;

        internal byte* buf;

        internal int buf_size;

        internal char* mime_type;
    }
}
