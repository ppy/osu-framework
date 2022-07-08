// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

        public const float HEIGHT = 40;

        public TitleBar(string title, string keyHelpText, Drawable movableTarget)
        {
            this.movableTarget = movableTarget;

            RelativeSizeAxes = Axes.X;
            Size = new Vector2(1, HEIGHT);

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
                    Spacing = new Vector2(10),
                    Children = new Drawable[]
                    {
                        new SpriteIcon
                        {
                            Size = new Vector2(20),
                            Margin = new MarginPadding(10) { Right = 0 },
                            Icon = FontAwesome.Regular.Circle,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = title,
                            Font = FrameworkFont.Condensed.With(weight: "Bold"),
                            Colour = FrameworkColour.Yellow,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = keyHelpText,
                            Font = FrameworkFont.Condensed,
                            Colour = FrameworkColour.Yellow,
                            Alpha = 0.5f
                        },
                    }
                },
            };
        }

        protected override bool OnDragStart(DragStartEvent e) => true;

        protected override void OnDrag(DragEvent e)
        {
            movableTarget.Position += e.Delta;
            base.OnDrag(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e) => true;
    }
}
