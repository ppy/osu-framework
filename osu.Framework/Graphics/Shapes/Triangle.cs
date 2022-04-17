// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// Represents a sprite that is drawn in a triangle shape, instead of a rectangle shape.
    /// </summary>
    public class Triangle : Sprite
    {
        /// <summary>
        /// Creates a new triangle with a white pixel as texture.
        /// </summary>
        public Triangle()
        {
            Texture = Texture.WhitePixel;
        }

        public override RectangleF BoundingBox => toTriangle(ToParentSpace(LayoutRectangle)).AABBFloat;

        private static Primitives.Triangle toTriangle(Quad q) => new Primitives.Triangle(
            (q.TopLeft + q.TopRight) / 2,
            q.BottomLeft,
            q.BottomRight);

        public override bool Contains(Vector2 screenSpacePos) => toTriangle(ScreenSpaceDrawQuad).Contains(screenSpacePos);

        protected override DrawNode CreateDrawNode() => new TriangleDrawNode(this);

        private class TriangleDrawNode : SpriteDrawNode
        {
            public TriangleDrawNode(Triangle source)
                : base(source)
            {
            }

            protected override void Blit(in VertexGroupUsage<TexturedVertex2D> usage)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                DrawTriangle(usage, Texture,
                    toTriangle(ScreenSpaceDrawQuad), DrawColourInfo.Colour, null, new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
            }

            protected override void BlitOpaqueInterior(in VertexGroupUsage<TexturedVertex2D> usage)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                var triangle = toTriangle(ConservativeScreenSpaceDrawQuad);

                if (GLWrapper.IsMaskingActive)
                    DrawClipped(usage, ref triangle, Texture, DrawColourInfo.Colour);
                else
                    DrawTriangle(usage, Texture, triangle, DrawColourInfo.Colour);
            }
        }
    }
}
