// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseScrollableFlow : TestCase
    {
        private ScheduledDelegate boxCreator;

        public override string Description => @"A flow container in a scroll container";

        private ScrollContainer scroll;
        private FillFlowContainer flow;
        private Direction scrollDir;

        private void createArea(Direction dir)
        {
            Axes scrollAxis = dir == Direction.Horizontal ? Axes.X : Axes.Y;

            Children = new[]
            {
                scroll = new ScrollContainer(dir)
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        flow = new FillFlowContainer
                        {
                            LayoutDuration = 100,
                            LayoutEasing = EasingTypes.Out,
                            Spacing = new Vector2(1, 1),
                            RelativeSizeAxes = Axes.Both & ~scrollAxis,
                            AutoSizeAxes = scrollAxis,
                            Padding = new MarginPadding(5)
                        }
                    },
                },
            };
        }

        private void createAreaBoth()
        {
            Children = new[]
            {
                new ScrollContainer(Direction.Horizontal)
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = 150 },
                    Children = new[]
                    {
                        scroll = new ScrollContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Children = new[]
                            {
                                flow = new FillFlowContainer
                                {
                                    LayoutDuration = 100,
                                    LayoutEasing = EasingTypes.Out,
                                    Spacing = new Vector2(1, 1),
                                    Size = new Vector2(1000, 0),
                                    AutoSizeAxes = Axes.Y,
                                    Padding = new MarginPadding(5)
                                }
                            }
                        }
                    },
                },
            };

            scroll.ScrollContent.AutoSizeAxes = Axes.None;
            scroll.ScrollContent.RelativeSizeAxes = Axes.None;
            scroll.ScrollContent.AutoSizeAxes = Axes.Both;
        }

        public override void Reset()
        {
            base.Reset();

            createArea(scrollDir = Direction.Vertical);

            AddStep("Vertical", delegate { createArea(scrollDir = Direction.Vertical); });
            AddStep("Horizontal", delegate { createArea(scrollDir = Direction.Horizontal); });
            AddStep("Both", createAreaBoth);

            AddStep("Dragger Anchor 1", delegate { scroll.ScrollDraggerAnchor = scrollDir == Direction.Vertical ? Anchor.TopRight : Anchor.BottomLeft; });
            AddStep("Dragger Anchor 2", delegate { scroll.ScrollDraggerAnchor = Anchor.TopLeft; });

            AddStep("Dragger Visible", delegate { scroll.ScrollDraggerVisible = !scroll.ScrollDraggerVisible; });
            AddStep("Dragger Overlap", delegate { scroll.ScrollDraggerOverlapsContent = !scroll.ScrollDraggerOverlapsContent; });

            boxCreator?.Cancel();
            boxCreator = Scheduler.AddDelayed(delegate
            {
                if (Parent == null) return;

                Box box;
                Container container = new Container
                {
                    Size = new Vector2(80, 80),
                    Children = new[]
                    {
                        box = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = new Color4(RNG.NextSingle(), RNG.NextSingle(), RNG.NextSingle(), 1)
                        }
                    }
                };

                flow.Add(container);

                container.FadeInFromZero(1000);
                using (container.BeginDelayedSequence(RNG.Next(0, 20000), true))
                {
                    container.FadeOutFromOne(4000);
                    box.RotateTo((RNG.NextSingle() - 0.5f) * 90, 4000);
                    box.ScaleTo(0.5f, 4000);
                    container.Expire();
                }
            }, 100, true);
        }
    }
}
