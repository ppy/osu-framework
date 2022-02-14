// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;

namespace osu.Framework.Timing
{
    public struct FrameTimeInfo
    {
        /// <summary>
        /// Elapsed time during last frame in milliseconds.
        /// </summary>
        public double Elapsed;

        /// <summary>
        /// Begin time of this frame.
        /// </summary>
        public double Current;

        public override readonly string ToString() => Math.Truncate(Current).ToString(CultureInfo.InvariantCulture);
    }
}
