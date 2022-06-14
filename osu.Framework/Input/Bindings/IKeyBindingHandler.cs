// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Input.Events;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// A drawable that handles key bindings.
    /// </summary>
    /// <typeparam name="T">The type of bindings, commonly an enum.</typeparam>
    public interface IKeyBindingHandler<T> : IKeyBindingHandler
        where T : struct
    {
        /// <summary>
        /// Triggered when an action is pressed.
        /// </summary>
        /// <param name="e">The event containing information about the pressed action.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        bool OnPressed(KeyBindingPressEvent<T> e);

        /// <summary>
        /// Triggered when an action is released.
        /// </summary>
        /// <param name="e">The event containing information about the released action.</param>
        void OnReleased(KeyBindingReleaseEvent<T> e);
    }

    public interface IKeyBindingHandler : IDrawable
    {
    }
}
