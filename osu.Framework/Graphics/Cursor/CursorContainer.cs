// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using OpenTK;
using osu.Framework.Graphics.Sprites;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Cursor
{
    public class CursorContainer : Container
    {
        protected Drawable ActiveCursor;

        public override bool Contains(Vector2 screenSpacePos) => true;

        public CursorContainer()
        {
            Depth = float.MinValue;
            RelativeSizeAxes = Axes.Both;

            Add(ActiveCursor = CreateCursor());
        }

        protected virtual Drawable CreateCursor() => new Cursor();

        protected override bool OnMouseMove(InputState state)
        {
            ActiveCursor.Position = state.Mouse.Position;
            return base.OnMouseMove(state);
        }

        class Cursor : CircularContainer
        {
            public Cursor()
            {
                AutoSizeAxes = Axes.Both;

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
