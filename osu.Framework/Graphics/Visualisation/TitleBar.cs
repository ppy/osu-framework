// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.Visualisation
{
    internal class TitleBar : CompositeDrawable
    {
        private readonly Drawable movableTarget;

        public TitleBar(Drawable movableTarget)
        {
            this.movableTarget = movableTarget;

            RelativeSizeAxes = Axes.X;
            Size = new Vector2(1, 40);

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = FrameworkColour.BlueDark,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        new SpriteIcon
                        {
                            Size = new Vector2(20),
                            Margin = new MarginPadding(10),
                            Icon = FontAwesome.Regular.Circle,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = "Draw Visualiser",
                            Font = FrameworkFont.Condensed.With(weight: "Bold"),
                            Colour = FrameworkColour.Yellow
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = " (Ctrl+F1 to toggle)",
                            Font = FrameworkFont.Condensed,
                            Colour = FrameworkColour.Yellow,
                            Alpha = 0.5f
                        },
                    }
                },
            };
        }

        protected override bool OnDragStart(DragStartEvent e) => true;

        protected override bool OnDrag(DragEvent e)
        {
            movableTarget.Position += e.Delta;
            return base.OnDrag(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e) => true;
    }
}
