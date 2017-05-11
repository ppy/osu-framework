// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;

namespace osu.Framework.Input
{
    /// <summary>
    /// A simple placeholder container which allows handling keyboard input at a higher level than otherwise possible.
    /// </summary>
    public class GlobalHotkeys : Drawable
    {
        public Func<InputState, KeyDownEventArgs, bool> Handler;

        public override bool HandleInput => true;

        public GlobalHotkeys()
        {
            RelativeSizeAxes = Axes.Both;
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            return Handler(state, args);
        }
    }
}
