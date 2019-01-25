// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Visualisation;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseDrawVisualiser : TestCase
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            DrawVisualiser vis;
            Drawable target;

            Children = new[]
            {
                target = new TestCaseDynamicDepth
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Size = new Vector2(0.5f)
                },
                vis = new DrawVisualiser(),
            };

            vis.Show();
            vis.Target = target;
        }
    }
}
