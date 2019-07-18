// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneDelayedLoadWrapper : FrameworkTestScene
    {
        private const int panel_count = 2048;

        [TestCase(false)]
        [TestCase(true)]
        public void TestManyChildren(bool instant)
        {
            FillFlowContainer<Container> flow = null;
            TestSceneDelayedLoadUnloadWrapper.TestScrollContainer scroll = null;

            AddStep("create children", () =>
            {
                Children = new Drawable[]
                {
                    scroll = new TestSceneDelayedLoadUnloadWrapper.TestScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            flow = new FillFlowContainer<Container>
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                            }
                        }
                    }
                };

                for (int i = 1; i < panel_count; i++)
                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Children = new Drawable[]
                        {
                            new DelayedLoadWrapper(new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new TestBox { RelativeSizeAxes = Axes.Both }
                                }
                            }, instant ? 0 : 500),
                            new SpriteText { Text = i.ToString() },
                        }
                    });
            });

            var childrenWithAvatarsLoaded = new Func<IEnumerable<Drawable>>(() => flow.Children.Where(c => c.Children.OfType<DelayedLoadWrapper>().First().Content?.IsLoaded ?? false));

            AddWaitStep("wait for load", 10);
            AddStep("scroll down", () => scroll.ScrollToEnd());
            AddWaitStep("wait more", 10);
            AddAssert("some loaded", () => childrenWithAvatarsLoaded().Count() > 5);
            AddAssert("not too many loaded", () => childrenWithAvatarsLoaded().Count() < panel_count / 4);

            AddStep("Remove all panels", () => flow.Clear(false));

            AddUntilStep("repeating schedulers removed", () => !scroll.Scheduler.HasPendingTasks);
        }

        public class TestBox : Container
        {
            public TestBox()
            {
                RelativeSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Child = new SpriteText
                {
                    Colour = Color4.Yellow,
                    Text = @"loaded",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };
            }
        }
    }
}
