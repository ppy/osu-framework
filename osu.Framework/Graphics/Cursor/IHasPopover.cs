// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// Interface to be implemented by UI controls that show a <see cref="Popover"/> on click.
    /// </summary>
    public interface IHasPopover : IDrawable
    {
        /// <summary>
        /// Creates the <see cref="Popover"/> to display for this control.
        /// Supports returning <see langword="null"/> if the popover should only display in some cases
        /// (e.g. if the control is not disabled).
        /// </summary>
        Popover? GetPopover();
    }
}
