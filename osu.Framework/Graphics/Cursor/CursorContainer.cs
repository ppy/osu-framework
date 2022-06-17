// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Cursor
{
    public class CursorContainer : VisibilityContainer, IRequireHighFrequencyMousePosition
    {
        public Drawable ActiveCursor { get; protected set; }

        public CursorContainer()
        {
            Depth = float.MinValue;
            RelativeSizeAxes = Axes.Both;

            State.Value = Visibility.Visible;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(ActiveCursor = CreateCursor());
        }

        protected virtual Drawable CreateCursor() => new Cursor();

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        // make sure we always receive positional input, regardless of our visibility state.
        public override bool PropagatePositionalInputSubTree => true;

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            ActiveCursor.Position = e.MousePosition;
            return base.OnMouseMove(e);
        }

        protected override void PopIn()
        {
            Alpha = 1;
        }

        protected override void PopOut()
        {
            Alpha = 0;
        }

        private class Cursor : CircularContainer
        {
            public Cursor()
            {
                AutoSizeAxes = Axes.Both;
                Origin = Anchor.Centre;

                BorderThickness = 2;
                BorderColour = new Color4(247, 99, 164, 255);

                Masking = true;
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = new Color4(247, 99, 164, 6),
                    Radius = 50
                };

                Child = new Box
                {
                    Size = new Vector2(8, 8),
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                };
            }
        }
    }
}
