// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
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

        public CursorContainer()
        {
            Depth = float.MaxValue;
            RelativeSizeAxes = Axes.Both;
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);

            Add(ActiveCursor = CreateCursor());
        }

        protected virtual Drawable CreateCursor() => new Cursor();

        public override bool Contains(Vector2 screenSpacePos) => true;

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
                GlowColour = new Color4(247, 99, 164, 6);
                GlowRadius = 50;

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
