// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Performance;
using osu.Framework.Statistics;

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

            var stat = new GlobalStatistic<double>("TestCase", "Test Statistic")
            {
                Value = { Value = 10 }
            };

            AddStep("Register test statistic", () => GlobalStatistics.Register(stat));
            AddStep("Change value", () => stat.Value.Value = 20);
        }
    }
}
