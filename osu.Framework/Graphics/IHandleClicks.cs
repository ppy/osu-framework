﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics
{
    public interface IHandleClicks
    {
        /// <summary>
        /// Triggered whenever a mouse click occurs on top of this Drawable.
        /// </summary>
        /// <param name="state">The state after the click.</param>
        /// <returns>True if this Drawable handled the event. If false, then the event
        /// is propagated up the scene graph to the next eligible Drawable.</returns>
        bool OnClick(InputState state);
    }
}
