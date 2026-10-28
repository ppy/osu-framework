// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.Events;
using osuTK.Input;

namespace osu.Framework.Graphics.Containers
{
    public partial class TabbableContainer : TabbableContainer<Drawable>
    {
    }

    /// <summary>
    /// This interface is used for recognizing <see cref="TabbableContainer{T}"/> of any type without reflection.
    /// </summary>
    public interface ITabbableContainer
    {
        /// <summary>
        /// Whether this <see cref="ITabbableContainer"/> can be tabbed to.
        /// </summary>
        bool CanBeTabbedTo { get; }
    }

    public partial class TabbableContainer<T> : Container<T>, ITabbableContainer
        where T : Drawable
    {
        /// <summary>
        /// Whether this <see cref="TabbableContainer{T}"/> can be tabbed to.
        /// </summary>
        public virtual bool CanBeTabbedTo => true;

        /// <summary>
        /// Allows for tabbing between multiple levels within the TabbableContentContainer.
        /// </summary>
        public CompositeDrawable TabbableContentContainer { private get; set; }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (TabbableContentContainer == null || e.Key != Key.Tab)
                return false;

            TabbableContentContainer.MoveFocusToNextTabStop(e.ShiftPressed);
            return true;
        }
    }
}
