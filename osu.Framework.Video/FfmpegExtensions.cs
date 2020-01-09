// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    internal static class FfmpegExtensions
    {
        internal static double GetValue(this AVRational rational) => rational.num / (double)rational.den;
    }
}
