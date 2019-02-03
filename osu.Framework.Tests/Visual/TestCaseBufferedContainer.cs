// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseBufferedContainer : TestCaseMasking
    {
        private readonly BufferedContainer buffer;

        public TestCaseBufferedContainer()
        {
            Remove(TestContainer);

            Add(buffer = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[] { TestContainer }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            buffer.BlurTo(new Vector2(20), 1000).Then().BlurTo(Vector2.Zero, 1000).Loop();
        }
    }
}
