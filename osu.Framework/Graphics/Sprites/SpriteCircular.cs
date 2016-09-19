// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteCircular : Sprite
    {
        public float HoverRadius;

        internal override bool Contains(Vector2 screenSpacePos)
        {
            float hoverRadius = HoverRadius > 0 ? HoverRadius : Texture.DisplayWidth / 2f;
            Vector2 screenHoverRadius = new Vector2(hoverRadius, hoverRadius) * DrawInfo.Matrix.ExtractScale().Xy;
            return Vector2.DistanceSquared(screenSpacePos, ScreenSpaceDrawQuad.Centre) < Vector2.Dot(screenHoverRadius, screenHoverRadius);
        }
    }
}
