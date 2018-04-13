// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Cursor
{
    public class CursorContainer : VisibilityContainer, IRequireHighFrequencyMousePosition
    {
        public Drawable ActiveCursor { get; protected set; }

        public CursorContainer()
        {
            Depth = float.MinValue;
            RelativeSizeAxes = Axes.Both;

            State = Visibility.Visible;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(ActiveCursor = CreateCursor());
        }

        protected virtual Drawable CreateCursor() => new Cursor();

        public override bool ReceiveMouseInputAt(Vector2 screenSpacePos) => true;

        protected override bool OnMouseMove(InputState state)
        {
            ActiveCursor.Position = state.Mouse.Position;
            return base.OnMouseMove(state);
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
