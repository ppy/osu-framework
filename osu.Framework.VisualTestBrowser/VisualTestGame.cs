﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests
{
    internal class VisualTestGame : Game
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new TestBrowser(),
                new CursorContainer(),
            };
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            host.Window.CursorState |= CursorState.Hidden;
        }
    }
}
