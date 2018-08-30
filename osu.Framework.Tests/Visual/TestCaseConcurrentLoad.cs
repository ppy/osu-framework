// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseConcurrentLoad : TestCase
    {
        private const int panel_count = 6;

        private FillFlowContainerNoInput flow;

        [SetUp]
        public void SetUp()
        {
            Child = flow = new FillFlowContainerNoInput
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

            AddUntilStep(() => flow.Children.OfType<DelayedTestBox>().Any() && flow.Children.OfType<DelayedTestBox>().Count() < panel_count, "check not all loaded");

            AddUntilStep(() => flow.Children.Count == panel_count, "wait all loaded");
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

            AddUntilStep(() => flow.Children.OfType<DelayedTestBoxAsync>().Any(), "wait some loaded");

            // due to thread yielding all should be loaded straight after any are loaded.
            AddAssert("check all loaded", () => flow.Children.OfType<DelayedTestBoxAsync>().Count() == panel_count);
        }

        private class FillFlowContainerNoInput : FillFlowContainer<Drawable>
        {
            public override bool HandleKeyboardInput => false;
            public override bool HandleMouseInput => false;
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
                await Task.Delay((int)(1000 / Clock.Rate));
            }
        }
    }
}
