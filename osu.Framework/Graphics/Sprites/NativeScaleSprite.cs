// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Graphics.Sprites
{
    class NativeScaleSprite : Sprite
    {
        public override Vector2 DrawSize
        {
            get
            {
                if (Texture == null) return Vector2.Zero;
                Vector3 comp = DrawInfo.Matrix.ExtractScale();
                return base.DrawSize * new Vector2(1 / comp.X, 1 / comp.Y);
            }
        }
    }
}
