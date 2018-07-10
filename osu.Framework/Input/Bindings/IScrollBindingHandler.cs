// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// A drawable that handles scroll actions.
    /// </summary>
    /// <typeparam name="T">The type of bindings.</typeparam>
    public interface IScrollBindingHandler<in T> : IKeyBindingHandler<T>
        where T : struct
    {
        /// <summary>
        /// Triggered when a scroll action is pressed.
        /// </summary>
        /// <remarks>
        /// When the action is not handled by any <see cref="IScrollBindingHandler{T}"/>, <see cref="IKeyBindingHandler{T}.OnPressed"/> is called.
        /// In either cases, <see cref="IKeyBindingHandler{T}.OnReleased"/> will be called once.</remarks>
        /// <param name="action">The action.</param>
        /// <param name="amount">The amount of mouse scroll.</param>
        /// <param name="isPrecise">Whether the action is from a precise scrolling.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        bool OnScroll(T action, float amount, bool isPrecise);
    }
}
