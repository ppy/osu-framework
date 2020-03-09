// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Visualisation
{
    /// <summary>
    /// Tracks global game statistics.
    /// </summary>
    internal class ClockOverviewDisplay : ToolWindow
    {
        [Resolved]
        private Game game { get; set; }

        private readonly FillFlowContainer flow;

        public ClockOverviewDisplay()
            : base("Clock Overview", "(Ctrl+F3 to toggle)")
        {
            ScrollContent.Children = new Drawable[]
            {
                flow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Full,
                },
            };

            AddButton("update", findClocks);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            findClocks();
        }

        private void findClocks() => Schedule(() =>
        {
            flow.Clear();
            findClocks(game, flow);
        });

        private void findClocks(CompositeDrawable drawable, FillFlowContainer target, IClock clock = null)
        {
            foreach (var child in drawable.InternalChildren)
            {
                var childTarget = target;

                if (child.Clock != clock)
                {
                    var representation = new DrawableWithClock(child) { UnderlyingDrawableDisposed = findClocks };
                    target.Add(representation);

                    childTarget = representation.ChildComponents;
                    clock = child.Clock;
                }

                if (child is CompositeDrawable composite)
                    findClocks(composite, childTarget, clock);
            }
        }

        private class DrawableWithClock : CompositeDrawable
        {
            private readonly Drawable drawable;
            public FillFlowContainer ChildComponents { get; }

            public Action UnderlyingDrawableDisposed;

            public DrawableWithClock(Drawable drawable)
            {
                this.drawable = drawable;
                FillFlowContainer clockFlow;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                Padding = new MarginPadding(5) { Left = 10 };

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.White.Opacity(0.1f),
                        RelativeSizeAxes = Axes.Both,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Padding = new MarginPadding(5),
                                Text = drawable.ToString()
                            },
                            clockFlow = new FillFlowContainer
                            {
                                Y = 30,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Full,
                                Padding = new MarginPadding(5),
                                Spacing = new Vector2(5)
                            },
                            ChildComponents = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                            },
                        }
                    },
                };

                IClock clock = drawable.Clock;
                clockFlow.Add(new VisualClock(clock) { Scale = new Vector2(0.6f) });
                while ((clock = clock.Source) != null)
                    clockFlow.Add(new VisualClock(clock) { Scale = new Vector2(0.5f) });
            }

            protected override void Update()
            {
                base.Update();

                if (drawable.IsDisposed || drawable.Parent == null)
                    UnderlyingDrawableDisposed?.Invoke();
            }
        }
    }
}
