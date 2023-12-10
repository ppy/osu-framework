// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osuTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// Represents a sprite that is drawn in a triangle shape, instead of a rectangle shape.
    /// </summary>
    public partial class Triangle : Sprite
    {
        /// <summary>
        /// Creates a new triangle with a white pixel as texture.
        /// </summary>
        public Triangle()
        {
            // Setting the texture would normally set a size of (1, 1), but since the texture is set from BDL it needs to be set here instead.
            // RelativeSizeAxes may not behave as expected if this is not done.
            Size = Vector2.One;
        }

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            Texture ??= renderer.WhitePixel;
        }

        public override RectangleF BoundingBox => ToTriangle(ToParentSpace(LayoutRectangle)).AABBFloat;

        /// <summary>
        /// Converts a <see cref="Quad"/> and its vertex to a <see cref="Primitives.Triangle"/>.
        /// </summary>
        /// <param name="q">A quadrilateral boundary, providing four vertices.</param>
        /// <returns>Converted triangle.</returns>
        protected Primitives.Triangle ToTriangle(Quad q) => new Primitives.Triangle(
            (q.TopLeft + q.TopRight) / 2,
            q.BottomLeft,
            q.BottomRight);

        public override bool Contains(Vector2 screenSpacePos) => ToTriangle(ScreenSpaceDrawQuad).Contains(screenSpacePos);

        protected override DrawNode CreateDrawNode() => new TriangleDrawNode(this);

        private class TriangleDrawNode : SpriteDrawNode
        {
            protected new Triangle Source => (Triangle)base.Source;

            public TriangleDrawNode(Triangle source)
                : base(source)
            {
            }

            protected override void Blit(IRenderer renderer)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                renderer.DrawTriangle(Texture, Source.ToTriangle(ScreenSpaceDrawQuad), DrawColourInfo.Colour, null, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
            }

            protected override void BlitOpaqueInterior(IRenderer renderer)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                var triangle = Source.ToTriangle(ConservativeScreenSpaceDrawQuad);

                if (renderer.IsMaskingActive)
                    renderer.DrawClipped(ref triangle, Texture, DrawColourInfo.Colour);
                else
                    renderer.DrawTriangle(Texture, triangle, DrawColourInfo.Colour);
            }
        }
    }
}
