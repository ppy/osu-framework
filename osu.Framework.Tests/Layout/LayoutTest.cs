// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Layout
{
    public abstract class LayoutTest
    {
        public static void Run(Drawable child, Func<int, bool> testAction)
        {
            using (var host = new HeadlessGameHost())
            {
                host.Run(new LayoutTestGame(child, testAction));
            }
        }

        private class LayoutTestGame : Game
        {
            private readonly Drawable child;
            private readonly Func<int, bool> callback;

            public LayoutTestGame(Drawable child, Func<int, bool> callback)
            {
                this.child = child;
                this.callback = callback;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Add(child);
            }

            private int currentFrame;

            public override bool UpdateSubTree()
            {
                if (callback?.Invoke(currentFrame++) ?? false)
                    Exit();
                return base.UpdateSubTree();
            }
        }
    }
}
