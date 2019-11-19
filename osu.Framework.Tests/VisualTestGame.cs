// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests
{
    internal class VisualTestGame : TestGame
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new SafeAreaContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new DrawSizePreservingFillContainer
                {
                    Children = new Drawable[]
                    {
                        new TestBrowser(),
                        new CursorContainer(),
                    },
                }
            };
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            if (host.Window != null)
                host.Window.CursorState |= CursorState.Hidden;
            else if (host.NewWindow != null)
                host.NewWindow.CursorState.Value |= CursorState.Hidden;
        }
    }
}
