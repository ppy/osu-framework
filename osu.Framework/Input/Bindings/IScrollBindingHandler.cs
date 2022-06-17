// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.Events;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// A drawable that handles scroll actions.
    /// </summary>
    /// <typeparam name="T">The type of bindings.</typeparam>
    public interface IScrollBindingHandler<T> : IKeyBindingHandler<T>
        where T : struct
    {
        /// <summary>
        /// Triggered when a scroll action is pressed.
        /// </summary>
        /// <remarks>
        /// When the action is not handled by any <see cref="IScrollBindingHandler{T}"/>, <see cref="IKeyBindingHandler{T}.OnPressed"/> is called.
        /// In either cases, <see cref="IKeyBindingHandler{T}.OnReleased"/> will be called once.</remarks>
        /// <param name="e">The event containing information about the scroll.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        bool OnScroll(KeyBindingScrollEvent<T> e);
    }
}
