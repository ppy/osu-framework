// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Platform
{
    /// <summary>
    /// Enumerates the available window states in the operating system.
    /// </summary>
    public enum WindowState
    {
        /// <summary>
        /// The window is movable and takes up a subsection of the screen.
        /// This is the default state.
        /// </summary>
        Normal,

        /// <summary>
        /// The window is running in exclusive fullscreen and is potentially using a
        /// different resolution to the desktop.
        /// </summary>
        Fullscreen,

        /// <summary>
        /// The window is running in non-exclusive fullscreen, where it expands to fill the screen
        /// at the native desktop resolution.
        /// </summary>
        FullscreenBorderless,

        /// <summary>
        /// The window is running in maximised mode, usually triggered by clicking the operating
        /// system's maximise button.
        /// </summary>
        Maximised,

        /// <summary>
        /// The window is running in minimised mode, usually triggered by clicking the operating
        /// system's minimise button.
        /// </summary>
        Minimised
    }
}
