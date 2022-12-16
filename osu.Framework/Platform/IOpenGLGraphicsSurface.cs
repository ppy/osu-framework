// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents OpenGL-specific graphics API provided by an <see cref="IWindow"/>.
    /// </summary>
    public interface IOpenGLGraphicsSurface
    {
        /// <summary>
        /// Enables or disables vertical sync.
        /// </summary>
        bool VerticalSync { get; set; }

        /// <summary>
        /// Returns the graphics context associated with the window.
        /// </summary>
        IntPtr WindowContext { get; }

        /// <summary>
        /// Returns the current graphics context.
        /// </summary>
        IntPtr CurrentContext { get; }

        /// <summary>
        /// Swaps the back buffer with the front buffer.
        /// </summary>
        void SwapBuffers();

        /// <summary>
        /// Creates a new graphics context associated with this window.
        /// </summary>
        void CreateContext();

        /// <summary>
        /// Marks the specified context as current.
        /// </summary>
        void MakeCurrent(IntPtr context);

        /// <summary>
        /// Clears the current graphics context.
        /// </summary>
        void ClearCurrent();

        /// <summary>
        /// Deletes the specified context.
        /// </summary>
        void DeleteContext(IntPtr context);

        /// <summary>
        /// Returns a pointer to the named OpenGL function.
        /// </summary>
        /// <param name="symbol">The symbolic name of the OpenGL function.</param>
        IntPtr GetProcAddress(string symbol);
    }
}
