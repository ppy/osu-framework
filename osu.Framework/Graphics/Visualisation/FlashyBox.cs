// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Visualisation
{
    class FlashyBox : Box
    {
        Drawable target;

        public FlashyBox()
        {
            Size = new Vector2(4);
            Origin = Anchor.Centre;
            Colour = Color4.Red;
            Alpha = 0.5f;
        }

        public Drawable Target
        {
            set
            {
                target = value;
                Invalidate(Invalidation.Geometry);
            }
        }

        internal override DrawInfo DrawInfo
        {
            get
            {
                // Use own DrawInfo for color, use target DrawInfo for shape.
                DrawInfo result = base.DrawInfo;
                DrawInfo targetDrawInfo = target.DrawInfo;
                result.Matrix = targetDrawInfo.Matrix;
                result.MatrixInverse = targetDrawInfo.MatrixInverse;
                return result;
            }
        }

        internal override RectangleF DrawRectangle => target.DrawRectangle;

        public override void Load(BaseGame game)
        {
            base.Load(game);

            FadeColour(Color4.Red, 500);
            Delay(500);
            FadeColour(Color4.White, 500);
            Delay(500);
            Loop();
        }
    }
}