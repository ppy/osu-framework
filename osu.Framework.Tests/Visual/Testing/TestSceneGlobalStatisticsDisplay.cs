// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Performance;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneGlobalStatisticsDisplay : FrameworkTestScene
    {
        private GlobalStatisticsDisplay display;

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new[]
            {
                display = new GlobalStatisticsDisplay(),
            };

            display.ToggleVisibility();

            display.Register(new GlobalStatistic<double>("Drawable", "DelayedLoadWrapper")
            {
                Value = { Value = 10 }
            });
        }
    }
}
