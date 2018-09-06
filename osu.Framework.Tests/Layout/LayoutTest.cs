// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Layout
{
    /// <summary>
    /// Base class for a test that runs a game until required.
    /// </summary>
    public abstract class LayoutTest
    {
        /// <summary>
        /// A delegate invoked every <see cref="Drawable.UpdateSubTree"/>. Returning true will cause the game to exit.
        /// </summary>
        /// <param name="frameIndex">The index of invocation of <see cref="Drawable.UpdateSubTree"/>.
        /// A value of 0 indicates the callback happened prior to the first <see cref="Drawable.UpdateSubTree"/>.</param>
        public delegate bool TestDelegate(int frameIndex);

        /// <summary>
        /// Runs the game.
        /// </summary>
        /// <param name="child">The child to add to the hierarchy.</param>
        /// <param name="testAction">A delegate to be executed every <see cref="Drawable.UpdateSubTree"/>. Returning true will cause the game to exit.</param>
        public static void Run(Drawable child, TestDelegate testAction)
        {
            using (var host = new HeadlessGameHost())
            {
                host.Run(new LayoutTestGame(child, testAction));
            }
        }

        private class LayoutTestGame : Game
        {
            private readonly Drawable child;
            private readonly TestDelegate callback;

            public LayoutTestGame(Drawable child, TestDelegate callback)
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
