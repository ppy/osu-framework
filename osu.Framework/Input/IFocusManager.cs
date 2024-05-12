// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;

namespace osu.Framework.Input
{
    public interface IFocusManager : IDrawable
    {
        /// <summary>
        /// The currently focused <see cref="Drawable"/>. Null if there is no current focus.
        /// </summary>
        Drawable FocusedDrawable { get; }

        /// <summary>
        /// Reset current focused drawable to the top-most drawable which is <see cref="Drawable.RequestsFocus"/>.
        /// </summary>
        /// <param name="triggerSource">The source which triggered this event.</param>
        void TriggerFocusContention(Drawable? triggerSource);

        /// <summary>
        /// Changes the currently-focused drawable. First checks that <paramref name="potentialFocusTarget"/> is in a valid state to receive focus,
        /// then unfocuses the current <see cref="FocusedDrawable"/> and focuses <paramref name="potentialFocusTarget"/>.
        /// <paramref name="potentialFocusTarget"/> can be null to reset focus.
        /// If the given drawable is already focused, nothing happens and no events are fired.
        /// </summary>
        /// <param name="potentialFocusTarget">The drawable to become focused.</param>
        /// <returns>True if the given drawable is now focused (or focus is dropped in the case of a null target).</returns>
        bool ChangeFocus(Drawable? potentialFocusTarget);
    }
}
