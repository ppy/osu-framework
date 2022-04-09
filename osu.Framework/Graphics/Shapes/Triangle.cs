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

            protected override void Blit(ref VertexGroup<TexturedVertex2D> vertices)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                DrawTriangle(Texture, toTriangle(ScreenSpaceDrawQuad), DrawColourInfo.Colour, ref vertices, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
            }

            protected override void BlitOpaqueInterior(ref VertexGroup<TexturedVertex2D> vertices)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                var triangle = toTriangle(ConservativeScreenSpaceDrawQuad);

                if (GLWrapper.IsMaskingActive)
                    DrawClipped(ref triangle, Texture, DrawColourInfo.Colour, ref vertices);
                else
                    DrawTriangle(Texture, triangle, DrawColourInfo.Colour, ref vertices);
            }
        }
    }
}
