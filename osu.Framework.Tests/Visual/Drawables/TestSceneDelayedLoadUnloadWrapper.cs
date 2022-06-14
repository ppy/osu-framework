// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Lists;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneDelayedLoadUnloadWrapper : FrameworkTestScene
    {
        private const int panel_count = 1024;

        private FillFlowContainer<Container> flow;
        private TestScrollContainer scroll;

        [Resolved]
        private Game game { get; set; }

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Children = new Drawable[]
            {
                scroll = new TestScrollContainer
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

        [Test]
        public void TestUnloadViaScroll()
        {
            WeakList<Container> references = new WeakList<Container>();

            AddStep("populate panels", () =>
            {
                references.Clear();

                for (int i = 0; i < 16; i++)
                {
                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Children = new Drawable[]
                        {
                            new DelayedLoadUnloadWrapper(() =>
                            {
                                var container = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        new TestBox { RelativeSizeAxes = Axes.Both }
                                    },
                                };

                                references.Add(container);

                                return container;
                            }, 500, 2000),
                            new SpriteText { Text = i.ToString() },
                        }
                    });
                }

                flow.Add(
                    new Container
                    {
                        Size = new Vector2(128, 1280),
                    });
            });

            AddUntilStep("references loaded", () => references.Count() == 16 && references.All(c => c.IsLoaded));

            AddStep("scroll to end", () => scroll.ScrollToEnd());

            AddUntilStep("references lost", () =>
            {
                GC.Collect();
                return !references.Any();
            });

            AddStep("scroll to start", () => scroll.ScrollToStart());

            AddUntilStep("references restored", () => references.Count() == 16);
        }

        [Test]
        public void TestTasksCanceledDuringLoadSequence()
        {
            var references = new WeakList<TestBox>();

            AddStep("populate panels", () =>
            {
                references.Clear();

                for (int i = 0; i < 16; i++)
                {
                    DelayedLoadUnloadWrapper loadUnloadWrapper;

                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Child = loadUnloadWrapper = new DelayedLoadUnloadWrapper(() =>
                        {
                            var content = new TestBox { RelativeSizeAxes = Axes.Both };
                            references.Add(content);
                            return content;
                        }, 0),
                    });

                    // cancel load tasks after the delayed load has started.
                    loadUnloadWrapper.DelayedLoadStarted += _ => game.Schedule(() => loadUnloadWrapper.UnbindAllBindables());
                }
            });

            AddStep("remove all panels", () => flow.Clear(false));

            AddUntilStep("references lost", () =>
            {
                GC.Collect();
                return !references.Any();
            });
        }

        [Test]
        public void TestRemovedStillUnload()
        {
            WeakList<Container> references = new WeakList<Container>();

            AddStep("populate panels", () =>
            {
                references.Clear();

                for (int i = 0; i < 16; i++)
                {
                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Children = new Drawable[]
                        {
                            new DelayedLoadUnloadWrapper(() =>
                            {
                                var container = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        new TestBox { RelativeSizeAxes = Axes.Both }
                                    },
                                };

                                references.Add(container);

                                return container;
                            }, 500, 2000),
                            new SpriteText { Text = i.ToString() },
                        }
                    });
                }
            });

            AddUntilStep("references loaded", () => references.Count() == 16 && references.All(c => c.IsLoaded));

            AddStep("Remove all panels", () => flow.Clear(false));

            AddUntilStep("references lost", () =>
            {
                GC.Collect();
                return !references.Any();
            });
        }

        [Test]
        public void TestRemoveThenAdd()
        {
            WeakList<Container> references = new WeakList<Container>();

            int loadCount = 0;

            AddStep("populate panels", () =>
            {
                references.Clear();
                loadCount = 0;

                for (int i = 0; i < 16; i++)
                {
                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Children = new Drawable[]
                        {
                            new DelayedLoadUnloadWrapper(() =>
                            {
                                TestBox testBox;
                                var container = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        testBox = new TestBox { RelativeSizeAxes = Axes.Both }
                                    },
                                };

                                testBox.OnLoadComplete += _ =>
                                {
                                    references.Add(container);
                                    loadCount++;
                                };

                                return container;
                            }, 500, 2000),
                            new SpriteText { Text = i.ToString() },
                        }
                    });
                }
            });

            IReadOnlyList<Container> previousChildren = null;

            AddUntilStep("all loaded", () => loadCount == 16);

            AddStep("Remove all panels", () =>
            {
                previousChildren = flow.Children.ToList();
                flow.Clear(false);
            });

            AddStep("Add panels back", () => flow.Children = previousChildren);

            AddWaitStep("wait for potential unload", 20);

            AddAssert("load count hasn't changed", () => loadCount == 16);
        }

        [Test]
        [Ignore("Fails intermittently on CI, can't be reproduced locally.")]
        public void TestManyChildrenUnload()
        {
            int loaded = 0;

            AddStep("populate panels", () =>
            {
                loaded = 0;

                for (int i = 1; i < panel_count; i++)
                {
                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Children = new Drawable[]
                        {
                            new DelayedLoadUnloadWrapper(() => new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new TestBox(() => loaded++) { RelativeSizeAxes = Axes.Both }
                                }
                            }, 500, 2000),
                            new SpriteText { Text = i.ToString() },
                        }
                    });
                }
            });

            IEnumerable<Drawable> childrenWithAvatarsLoaded() => flow.Children.Where(c => c.Children.OfType<DelayedLoadWrapper>().First().Content?.IsLoaded ?? false);

            AddUntilStep("wait for load", () => loaded > 0);

            int loadedCount1 = 0;
            Drawable[] loadedChildren1 = null;

            AddStep("scroll down", () =>
            {
                loadedCount1 = loaded;
                loadedChildren1 = childrenWithAvatarsLoaded().ToArray();
                scroll.ScrollToEnd();
            });

            AddUntilStep("more loaded", () => loaded > loadedCount1);

            AddAssert("not too many loaded", () => loaded < panel_count / 4);
            AddUntilStep("wait some unloaded", () => loadedChildren1.Any(c => !childrenWithAvatarsLoaded().Contains(c)));
        }

        [Test]
        public void TestWrapperExpiry()
        {
            var wrappers = new List<DelayedLoadUnloadWrapper>();

            AddStep("populate panels", () =>
            {
                for (int i = 1; i < 16; i++)
                {
                    var wrapper = new DelayedLoadUnloadWrapper(() => new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new TestBox { RelativeSizeAxes = Axes.Both }
                        }
                    }, 500, 2000);

                    wrappers.Add(wrapper);

                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Children = new Drawable[]
                        {
                            wrapper,
                            new SpriteText { Text = i.ToString() },
                        }
                    });
                }
            });

            int childrenWithAvatarsLoaded() => flow.Children.Count(c => c.Children.OfType<DelayedLoadWrapper>().FirstOrDefault()?.Content?.IsLoaded ?? false);

            AddUntilStep("wait some loaded", () => childrenWithAvatarsLoaded() > 5);
            AddStep("expire wrappers", () => wrappers.ForEach(w => w.Expire()));
            AddAssert("all unloaded", () => childrenWithAvatarsLoaded() == 0);
        }

        [Test]
        public void TestUnloadWithNonOptimisingParent()
        {
            DelayedLoadUnloadWrapper wrapper = null;

            AddStep("add panel", () =>
            {
                Add(new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(128),
                    Masking = true,
                    Child = wrapper = new DelayedLoadUnloadWrapper(() => new TestBox { RelativeSizeAxes = Axes.Both }, 0, 1000)
                });
            });

            AddUntilStep("wait for load", () => wrapper.Content?.IsLoaded == true);
            AddStep("move wrapper outside", () => wrapper.X = 129);
            AddUntilStep("wait for unload", () => wrapper.Content?.IsLoaded != true);
        }

        [Test]
        public void TestUnloadWithOffscreenParent()
        {
            Container parent = null;
            DelayedLoadUnloadWrapper wrapper = null;

            AddStep("add panel", () =>
            {
                Add(parent = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(128),
                    Masking = true,
                    Child = wrapper = new DelayedLoadUnloadWrapper(() => new TestBox { RelativeSizeAxes = Axes.Both }, 0, 1000)
                });
            });

            AddUntilStep("wait for load", () => wrapper.Content?.IsLoaded == true);
            AddStep("move parent offscreen", () => parent.X = 1000000); // Should be offscreen
            AddUntilStep("wait for unload", () => wrapper.Content?.IsLoaded != true);
        }

        [Test]
        public void TestUnloadWithParentRemovedFromHierarchy()
        {
            Container parent = null;
            DelayedLoadUnloadWrapper wrapper = null;

            AddStep("add panel", () =>
            {
                Add(parent = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(128),
                    Masking = true,
                    Child = wrapper = new DelayedLoadUnloadWrapper(() => new TestBox { RelativeSizeAxes = Axes.Both }, 0, 1000)
                });
            });

            AddUntilStep("wait for load", () => wrapper.Content?.IsLoaded == true);
            AddStep("remove parent", () => Remove(parent));
            AddUntilStep("wait for unload", () => wrapper.Content?.IsLoaded != true);
        }

        [Test]
        public void TestUnloadedWhenAsyncLoadCompletedAndMaskedAway()
        {
            BasicScrollContainer scrollContainer = null;
            DelayedLoadTestDrawable child = null;

            AddStep("add panel", () =>
            {
                Child = scrollContainer = new BasicScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(128),
                    Child = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 1000,
                        Child = new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 128,
                            Child = new DelayedLoadUnloadWrapper(() => child = new DelayedLoadTestDrawable { RelativeSizeAxes = Axes.Both }, 0, 1000)
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 128
                            }
                        }
                    }
                };
            });

            // Check that the child is disposed when its async-load completes while the wrapper is masked away.
            AddUntilStep("wait for load to begin", () => child?.LoadState == LoadState.Loading);
            AddStep("scroll to end", () => scrollContainer.ScrollToEnd(false));
            AddStep("allow load", () => child.AllowLoad.Set());
            AddUntilStep("drawable disposed", () => child.IsDisposed);

            Drawable lastChild = null;
            AddStep("store child", () => lastChild = child);

            // Check that reuse of the child is not attempted.
            AddStep("scroll to start", () => scrollContainer.ScrollToStart(false));
            AddStep("allow load of new child", () => child.AllowLoad.Set());
            AddUntilStep("new child loaded", () => child.IsLoaded);
            AddAssert("last child not loaded", () => !lastChild.IsLoaded);
        }

        [Test]
        public void TestWrapperStopReceivingUpdatesAfterDelayedLoadCompleted()
        {
            DelayedLoadTestDrawable child = null;

            AddStep("add panel", () =>
            {
                DelayedLoadUnloadWrapper wrapper;

                Child = wrapper = new DelayedLoadUnloadWrapper(() => child = new DelayedLoadTestDrawable { RelativeSizeAxes = Axes.Both }, 0, 1000)
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 128,
                };

                // Prevent the wrapper from receiving updates as soon as load completes, and start making it unload its contents by repositioning it offscreen.
                wrapper.DelayedLoadComplete += _ =>
                {
                    wrapper.Alpha = 0;
                    wrapper.Position = new Vector2(-1000);
                };
            });

            // Check that the child is disposed when its async-load completes while the wrapper is masked away.
            AddUntilStep("wait for load to begin", () => child?.LoadState == LoadState.Loading);
            AddStep("allow load", () => child.AllowLoad.Set());
            AddUntilStep("drawable disposed", () => child.IsDisposed);
        }

        public class TestScrollContainer : BasicScrollContainer
        {
            public new Scheduler Scheduler => base.Scheduler;
        }

        public class TestBox : Container
        {
            private readonly Action onLoadAction;

            public TestBox(Action onLoadAction = null)
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

        public class DelayedLoadTestDrawable : CompositeDrawable
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim(false);

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException();
            }
        }
    }
}
