// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Video
{
    // Touching AGffmpeg or its LibraryLoader in any way on non-Desktop platforms
    // will cause it to throw in static constructor, which can't be bypassed.
    // Define our own constants to avoid touching the class.

    internal static class FFmpegConstants
    {
        public const int AVSEEK_FLAG_BACKWARD = 1;
        public const int AVSEEK_SIZE = 0x10000;
        public const int AVFMT_FLAG_GENPTS = 0x0001;
        public const int AV_TIME_BASE = 1000000;
        public static readonly int EAGAIN = RuntimeInfo.IsApple ? 35 : 11;
        public const int AVERROR_EOF = -('E' + ('O' << 8) + ('F' << 16) + (' ' << 24));
        public const long AV_NOPTS_VALUE = unchecked((long)0x8000000000000000);
        public const int ENOMEM = 12;
    }
}
