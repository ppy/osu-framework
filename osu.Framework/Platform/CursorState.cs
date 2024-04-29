// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Describes our supported states of the OS cursor.
    /// </summary>
    [Flags]
    public enum CursorState
    {
        /// <summary>
        /// The OS cursor is always visible and can move anywhere.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The OS cursor is hidden while hovering the <see cref="IWindow"/>, but can still move anywhere.
        /// </summary>
        Hidden = 1,

        /// <summary>
        /// The OS cursor is confined to the <see cref="IWindow"/> while the window is in focus.
        /// </summary>
        Confined = 2,

        /// <summary>
        /// The OS cursor is hidden while hovering the <see cref="IWindow"/>.
        /// It is confined to the <see cref="IWindow"/> while the window is in focus and can move freely otherwise.
        /// </summary>
        HiddenAndConfined = Hidden | Confined,
    }
}
