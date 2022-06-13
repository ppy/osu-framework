// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneDelayedLoadWrapper : FrameworkTestScene
    {
        private FillFlowContainer<Container> flow;
        private TestSceneDelayedLoadUnloadWrapper.TestScrollContainer scroll;
        private int loaded;

        private const int panel_count = 2048;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create scroll container", () =>
            {
                loaded = 0;

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
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestManyChildren(bool instant)
        {
            AddStep("create children", () =>
            {
                for (int i = 1; i < panel_count; i++)
                {
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
                                    new TestBox(() => loaded++) { RelativeSizeAxes = Axes.Both }
                                }
                            }, instant ? 0 : 500),
                            new SpriteText { Text = i.ToString() },
                        }
                    });
                }
            });

            var childrenWithAvatarsLoaded = new Func<IEnumerable<Drawable>>(() => flow.Children.Where(c => c.Children.OfType<DelayedLoadWrapper>().First().Content?.IsLoaded ?? false));

            int loadCount1 = 0;

            AddUntilStep("wait for load", () => loaded > 0);

            AddStep("scroll down", () =>
            {
                loadCount1 = loaded;
                scroll.ScrollToEnd();
            });

            AddWaitStep("wait some more", 10);

            AddUntilStep("more loaded", () => loaded > loadCount1);
            AddAssert("not too many loaded", () => childrenWithAvatarsLoaded().Count() < panel_count / 4);

            AddStep("Remove all panels", () => flow.Clear(false));

            AddUntilStep("repeating schedulers removed", () => !scroll.Scheduler.HasPendingTasks);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestManyChildrenFunction(bool instant)
        {
            AddStep("create children", () =>
            {
                for (int i = 1; i < panel_count; i++)
                {
                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Children = new Drawable[]
                        {
                            new DelayedLoadWrapper(() => new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new TestBox(() => loaded++) { RelativeSizeAxes = Axes.Both }
                                }
                            }, instant ? 0 : 500),
                            new SpriteText { Text = i.ToString() },
                        }
                    });
                }
            });

            var childrenWithAvatarsLoaded = new Func<IEnumerable<Drawable>>(() => flow.Children.Where(c => c.Children.OfType<DelayedLoadWrapper>().First().Content?.IsLoaded ?? false));

            int loadCount1 = 0;

            AddUntilStep("wait for load", () => loaded > 0);

            AddStep("scroll down", () =>
            {
                loadCount1 = loaded;
                scroll.ScrollToEnd();
            });

            AddWaitStep("wait some more", 10);

            AddUntilStep("more loaded", () => loaded > loadCount1);
            AddAssert("not too many loaded", () => childrenWithAvatarsLoaded().Count() < panel_count / 4);

            AddStep("Remove all panels", () => flow.Clear(false));

            AddUntilStep("repeating schedulers removed", () => !scroll.Scheduler.HasPendingTasks);
        }

        public class TestBox : Container
        {
            private readonly Action onLoadAction;

            public TestBox(Action onLoadAction)
            {
                this.onLoadAction = onLoadAction;
                RelativeSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                onLoadAction?.Invoke();

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
