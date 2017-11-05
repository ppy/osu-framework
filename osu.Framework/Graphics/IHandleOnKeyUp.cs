// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics
{
    public interface IHandleOnKeyUp
    {
        /// <summary>
        /// Triggered whenever a key was released.
        /// </summary>
        /// <param name="state">The state after the key was released.</param>
        /// <param name="args">Specific arguments for key up event.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        bool OnKeyUp(InputState state, KeyUpEventArgs args);
    }
}
