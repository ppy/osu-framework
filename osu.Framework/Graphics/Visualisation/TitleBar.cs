// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Visualisation
{
    internal class TitleBar : CompositeDrawable
    {
        private readonly Drawable movableTarget;

        public TitleBar(string title, Drawable movableTarget)
        {
            this.movableTarget = movableTarget;

            RelativeSizeAxes = Axes.X;
            Size = new Vector2(1, 25);

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.BlueViolet,
                },
                new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = title,
                    Alpha = 0.8f,
                },
            };
        }

        protected override bool Handle(PositionalEvent e)
        {
            switch (e)
            {
                case DragStartEvent _:
                    return true;

                case DragEvent dragEvent:
                    movableTarget.Position += dragEvent.Delta;
                    return base.Handle(dragEvent);

                case MouseDownEvent _:
                    return true;

                default:
                    return base.Handle(e);
            }
        }
    }
}
