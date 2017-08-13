// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Handles key bindings.
    /// </summary>
    /// <typeparam name="T">The type of bindings, commonly an enum.</typeparam>
    public interface IKeyBindingHandler<in T>
        where T : struct
    {
        /// <summary>
        /// Triggered when an action is pressed.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        bool OnPressed(T action);

        /// <summary>
        /// Triggered when an action is released.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        bool OnReleased(T action);
    }
}