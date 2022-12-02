// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents the graphics API surface provided by an <see cref="IWindow"/>.
    /// </summary>
    public interface IGraphicsSurface
    {
        /// <summary>
        /// A pointer representing a handle to this window, provided by the operating system.
        /// </summary>
        IntPtr WindowHandle { get; }

        /// <summary>
        /// A pointer representing a handle to the display containing this window, provided by the operating system.
        /// This is specific to X11/Wayland subsystems.
        /// </summary>
        IntPtr DisplayHandle { get; }

        /// <summary>
        /// The type of surface.
        /// </summary>
        GraphicsSurfaceType Type { get; }

        /// <summary>
        /// Performs an initialisation of the graphics backend after <see cref="IWindow"/> has been created.
        /// </summary>
        void Initialise();

        /// <summary>
        /// Returns the drawable size of the window provided by the graphics backend.
        /// </summary>
        Size GetDrawableSize();
    }
}
