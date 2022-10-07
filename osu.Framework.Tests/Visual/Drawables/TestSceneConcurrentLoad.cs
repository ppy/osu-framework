// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneConcurrentLoad : FrameworkTestScene
    {
        private const int panel_count = 6;

        private FillFlowContainer flow;

        [SetUp]
        public void SetUp()
        {
            Child = flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };
        }

        [Test]
        [Ignore("pointless with LoadComponentAsync concurrency limiting")]
        public void LoadManyThreaded()
        {
            AddStep("load many thread", () =>
            {
                for (int i = 0; i < panel_count; i++)
                    LoadComponentAsync(new DelayedTestBox(), flow.Add);
            });

            AddAssert("check none loaded", () => !flow.Children.OfType<DelayedTestBox>().Any());

            AddUntilStep("check not all loaded", () => flow.Children.OfType<DelayedTestBox>().Any() && flow.Children.OfType<DelayedTestBox>().Count() < panel_count);

            AddUntilStep("wait all loaded", () => flow.Children.Count == panel_count);
        }

        [Test]
        [Ignore("pointless with LoadComponentAsync concurrency limiting")]
        public void LoadManyAsync()
        {
            AddStep("load many async", () =>
            {
                for (int i = 0; i < panel_count; i++)
                    LoadComponentAsync(new DelayedTestBoxAsync(), flow.Add);
            });

            AddAssert("check none loaded", () => !flow.Children.OfType<DelayedTestBoxAsync>().Any());

            AddUntilStep("wait some loaded", () => flow.Children.OfType<DelayedTestBoxAsync>().Any());

            // due to thread yielding all should be loaded straight after any are loaded.
            AddAssert("check all loaded", () => flow.Children.OfType<DelayedTestBoxAsync>().Count() == panel_count);
        }

        public class DelayedTestBox : Box
        {
            public DelayedTestBox()
            {
                Size = new Vector2(50);
                Colour = Color4.Blue;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Thread.Sleep((int)(1000 / Clock.Rate));
            }
        }

        public class DelayedTestBoxAsync : Box
        {
            public DelayedTestBoxAsync()
            {
                Size = new Vector2(50);
                Colour = Color4.Green;
            }

            [BackgroundDependencyLoader]
            private async Task load()
            {
                await Task.Delay((int)(1000 / Clock.Rate)).ConfigureAwait(false);
            }
        }
    }
}
