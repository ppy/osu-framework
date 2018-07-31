using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Text;

namespace osu.Framework.Graphics.Video
{
    internal static class FfmpegExtensions
    {
        internal static double GetValue(this AVRational rational) => rational.num / (double)rational.den;
    }
}
