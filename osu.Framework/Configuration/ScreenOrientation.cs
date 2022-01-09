// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// Screen orientation setting
    /// </summary>
    /// <remarks>
    /// Primarily intended for use with mobile platforms
    /// </remarks>
    [Flags]
    public enum ScreenOrientation
    {
        /// <summary>
        /// Let the device handle rotation
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Locked landscape, with device's screen rotated 90degree CLOCKWISE from default portrait orientation
        /// </summary>
        LandscapeLeft = 1,

        /// <summary>
        /// Locked landscape, with device's screen rotated 90degree COUNTER-CLOCKWISE from default portrait orientation
        /// </summary>
        LandscapeRight = 1 << 1,

        /// <summary>
        /// Locked standing portrait orientation
        /// </summary>
        Portrait = 1 << 2,

        /// <summary>
        /// Locked reverse portrait orientation
        /// </summary>
        ReversePortrait = 1 << 3,

        /// <summary>
        /// Landscape orientation, allows any landscape screen rotation
        /// </summary>
        AnyLandscape = LandscapeLeft | LandscapeRight,

        /// <summary>
        /// Portrait orientation, allows any portrait screen rotation
        /// </summary>
        AnyPortrait = Portrait | ReversePortrait,

        /// <summary>
        /// Allows all 4 orientation
        /// </summary>
        Any = AnyLandscape | AnyPortrait
    }
}
