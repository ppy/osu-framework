// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    internal static class FfmpegExtensions
    {
        internal static double GetValue(this AVRational rational) => rational.num / (double)rational.den;
    }
}
