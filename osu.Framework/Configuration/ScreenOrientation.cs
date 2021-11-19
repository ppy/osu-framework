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
        /// Locked landscape, with top-down goes from left to right on the portrait screen
        /// </summary>
        LandscapeLeft = 1,
        /// <summary>
        /// Locked landscape, with top-down goes from right to left on the portrait screen
        /// </summary>
        LandscapeRight = 2,
        /// <summary>
        /// Locked standing portrait orientation
        /// </summary>
        Portrait = 4,
        /// <summary>
        /// Locked reverse portrait orientation
        /// </summary>
        ReversePortrait = 8,
        /// <summary>
        /// Landscape orientation, allows landscape screen rotation
        /// </summary>
        AnyLandscape = LandscapeLeft | LandscapeRight, // 3
        /// <summary>
        /// Portrait orientation, allows portrait screen rotation
        /// </summary>
        AnyPortrait = Portrait | ReversePortrait, // 12
        /// <summary>
        /// Allows all 4 orientation.
        /// </summary>
        Any = AnyLandscape | AnyPortrait // 15
    }
}
