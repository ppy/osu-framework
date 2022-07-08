// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Performance;
using osu.Framework.Statistics;
using osu.Framework.Testing;

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
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("set visible", () => display.State.Value = Visibility.Visible);
        }

        [Test]
        public void TestUpdateStats()
        {
            GlobalStatistic<double> stat = null;

            AddStep("Register test statistic", () => stat = GlobalStatistics.Get<double>("TestCase", "Test Statistic"));

            AddStep("Change value once", () => stat.Value = 10);
            AddStep("Change value again", () => stat.Value = 20);

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

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("set hidden", () => display.State.Value = Visibility.Hidden);
        }
    }
}
