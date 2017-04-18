// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using OpenTK;
using osu.Framework.Graphics.Sprites;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Cursor
{
    public class CursorContainer : OverlayContainer, IRequireHighFrequencyMousePosition
    {
        protected Drawable ActiveCursor;

        protected override bool BlockPassThroughMouse => false;

        //OverlayContainer tried to be smart about this, but we don't want none of that.
        public override bool HandleInput => IsPresent;

        protected override bool HideOnEscape => false;

        public CursorContainer()
        {
            AlwaysReceiveInput = true;
            Depth = float.MinValue;
            RelativeSizeAxes = Axes.Both;

            Add(ActiveCursor = CreateCursor());

            State = Visibility.Visible;
        }

        protected virtual Drawable CreateCursor() => new Cursor();

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
                EdgeEffect = new EdgeEffect
                {
                    Type = EdgeEffectType.Glow,
                    Colour = new Color4(247, 99, 164, 6),
                    Radius = 50
                };

                Children = new[]
                {
                    new Box
                    {
                        Size = new Vector2(8, 8),
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                    }
                };
            }
        }
    }
}
