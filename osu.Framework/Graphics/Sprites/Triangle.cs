// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Sprites
{
    public class Triangle : Sprite
    {
        public Triangle()
        {
            Texture = Texture.WhitePixel;
        }

        public override RectangleF BoundingBox => toTriangle(ToParentSpace(LayoutRectangle)).AABBFloat;

        private static Primitives.Triangle toTriangle(Quad q) => new Primitives.Triangle(
            (q.TopLeft + q.TopRight) / 2,
            q.BottomLeft,
            q.BottomRight);

        public override bool Contains(Vector2 screenSpacePos)
        {
            return toTriangle(ScreenSpaceDrawQuad).Contains(screenSpacePos);
        }

        protected override DrawNode CreateDrawNode() => new TriangleDrawNode();

        class TriangleDrawNode : SpriteDrawNode
        {
            protected override void Blit(Action<TexturedVertex2D> vertexAction)
            {
                Texture.DrawTriangle(toTriangle(ScreenSpaceDrawQuad), DrawInfo.Colour, null, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height));
            }
        }
    }
}
