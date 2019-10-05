// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSuiteFrontToBack : TestSuite<TestSceneFrontToBack>
    {
        [BackgroundDependencyLoader]
        private void load(FrameworkDebugConfigManager debugConfig)
        {
            AddStep("add more drawables", addMoreDrawables);
            AddToggleStep("disable front to back", val =>
            {
                debugConfig.Set(DebugSetting.BypassFrontToBackPass, val);
                Invalidate(Invalidation.DrawNode); // reset counts
            });
        }

        private void addMoreDrawables()
        {
            for (int i = 0; i < 100; i++)
            {
                TestScene.Cell(i % TestSceneFrontToBack.CELL_COUNT).Add(new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                    RelativeSizeAxes = Axes.Both,
                    Scale = new Vector2(TestScene.CurrentScale)
                });

                TestScene.CurrentScale -= 0.001f;
                if (TestScene.CurrentScale < 0)
                    TestScene.CurrentScale = 1;
            }
        }
    }
}
