// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform
{
    public interface ILinuxGraphicsSurface
    {
        /// <summary>
        /// Whether the current display server is Wayland.
        /// </summary>
        bool IsWayland { get; }

        /// <summary>
        /// A pointer representing a handle to the display containing this window, provided by the operating system.
        /// This is specific to X11/Wayland subsystems.
        /// </summary>
        IntPtr DisplayHandle { get; }
    }
}
