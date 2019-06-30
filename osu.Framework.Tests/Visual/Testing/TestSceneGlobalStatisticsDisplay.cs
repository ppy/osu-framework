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
                Bindable = { Value = 10 }
            };

            AddStep("Register test statistic", () => GlobalStatistics.Register(stat));
            AddStep("Change value", () => stat.Bindable.Value = 20);

            AddStep("Register statistics non-alphabetically", () =>
            {
                GlobalStatistics.Get<int>("ZZZZZ", "BBBBB");
                GlobalStatistics.Get<int>("ZZZZZ", "AAAAA");
            });

            AddStep("Register groups non-alphabetically", () =>
            {
                GlobalStatistics.Get<int>("XXXXX", "BBBBB");
                GlobalStatistics.Get<int>("TTTTT", "AAAAA");
            });
        }
    }
}
