// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;

namespace osu.Framework.VisualTests
{
    class VisualTestGame : BaseGame
    {
        public VisualTestGame()
        {
            Children = new Drawable[]
            {
                new TestBrowser(),
                new CursorContainer(),
            };
        }
    }
}
