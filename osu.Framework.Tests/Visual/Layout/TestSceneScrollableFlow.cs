// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    public class TestSceneScrollableFlow : FrameworkTestScene
    {
        private ScrollContainer<Drawable> scroll;
        private FillFlowContainer flow;

        private void createArea(Direction dir)
        {
            Axes scrollAxis = dir == Direction.Horizontal ? Axes.X : Axes.Y;

            Children = new[]
            {
                scroll = new BasicScrollContainer(dir)
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        flow = new FillFlowContainer
                        {
                            LayoutDuration = 100,
                            LayoutEasing = Easing.Out,
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
                new BasicScrollContainer(Direction.Horizontal)
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = 150 },
                    Children = new[]
                    {
                        scroll = new BasicScrollContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Children = new[]
                            {
                                flow = new FillFlowContainer
                                {
                                    LayoutDuration = 100,
                                    LayoutEasing = Easing.Out,
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

        public TestSceneScrollableFlow()
        {
            Direction scrollDir;

            createArea(scrollDir = Direction.Vertical);

            AddStep("Vertical", delegate { createArea(scrollDir = Direction.Vertical); });
            AddStep("Horizontal", delegate { createArea(scrollDir = Direction.Horizontal); });
            AddStep("Both", createAreaBoth);

            AddStep("Dragger Anchor 1", delegate { scroll.ScrollbarAnchor = scrollDir == Direction.Vertical ? Anchor.TopRight : Anchor.BottomLeft; });
            AddStep("Dragger Anchor 2", delegate { scroll.ScrollbarAnchor = Anchor.TopLeft; });

            AddStep("Dragger Visible", delegate { scroll.ScrollbarVisible = !scroll.ScrollbarVisible; });
            AddStep("Dragger Overlap", delegate { scroll.ScrollbarOverlapsContent = !scroll.ScrollbarOverlapsContent; });

            Scheduler.AddDelayed(delegate
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

                double displayTime = RNG.Next(0, 20000);
                box.Delay(displayTime).ScaleTo(0.5f, 4000).RotateTo((RNG.NextSingle() - 0.5f) * 90, 4000);
                container.Delay(displayTime).FadeOut(4000).Expire();
            }, 100, true);
        }
    }
}
