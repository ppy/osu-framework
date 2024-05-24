// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Framework.Platform.Windows
{
    internal interface IWindowsWindow : ISDLWindow
    {
        /// <summary>
        /// The last mouse position as reported by <see cref="WindowsMouseHandler.FeedbackMousePositionChange"/>.
        /// </summary>
        Vector2? LastMousePosition { get; set; }
    }
}
