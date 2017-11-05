// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics
{
    public interface IHandleOnWheel
    {
        /// <summary>
        /// Triggered whenever the mouse wheel was turned over this Drawable.
        /// </summary>
        /// <param name="state">The state after the wheel was turned.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        bool OnWheel(InputState state);
    }
}
