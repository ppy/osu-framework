// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        private readonly FillFlowContainer clockFlow;

        public ClockOverviewDisplay()
            : base("Clock Overview", "(Ctrl+F3 to toggle)")
        {
            ScrollContent.Children = new Drawable[]
            {
                clockFlow = new FillFlowContainer
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

        private void findClocks()
        {
            clockFlow.Clear();
            findClocks(game, clockFlow);
        }

        private void findClocks(CompositeDrawable drawable, FillFlowContainer target, IClock clock = null)
        {
            foreach (var child in drawable.InternalChildren)
            {
                var childTarget = target;

                if (child.Clock != clock)
                {
                    var representation = new DrawableWithClock(child);
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
            public FillFlowContainer ChildComponents { get; private set; }

            public DrawableWithClock(Drawable drawable)
            {
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
                    new SpriteText
                    {
                        Text = drawable.ToString()
                    },
                    new VisualClock(drawable.Clock)
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Scale = new Vector2(0.5f),
                    },
                    ChildComponents = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Y = 100,
                    },
                };
            }
        }
    }
}
