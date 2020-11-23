using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A simple hexagon. Can be colored using the <see cref="Drawable.Colour"/> property.
    /// </summary>
    public class Hexagon : Sprite
    {
        public Hexagon()
        {
            Texture = Texture.WhitePixel;
        }

        public override RectangleF BoundingBox => toHexagon(ToParentSpace(LayoutRectangle)).AABBFloat;

        private static Primitives.Hexagon toHexagon(Quad q) => new Primitives.Hexagon(
            (q.TopLeft + q.BottomLeft) / 2,
            (q.TopRight + q.BottomRight) / 2);

        public override bool Contains(Vector2 screenSpacePos) => toHexagon(ScreenSpaceDrawQuad).Contains(screenSpacePos);

        protected override DrawNode CreateDrawNode() => new HexagonDrawNode(this);

        private class HexagonDrawNode : SpriteDrawNode
        {
            public HexagonDrawNode(Hexagon source) : base(source)
            {
            }

            protected override void Blit(Action<TexturedVertex2D> vertexAction)
            {
                Primitives.Hexagon drawingHex = toHexagon(ScreenSpaceDrawQuad);
                DrawTriangle(Texture, drawingHex.FarUpTriangle, DrawColourInfo.Colour, null, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
                DrawTriangle(Texture, drawingHex.NearUpTriangle, DrawColourInfo.Colour, null, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
                DrawTriangle(Texture, drawingHex.NearDownTriangle, DrawColourInfo.Colour, null, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
                DrawTriangle(Texture, drawingHex.FarDownTriangle, DrawColourInfo.Colour, null, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
            }

            protected override void BlitOpaqueInterior(Action<TexturedVertex2D> vertexAction)
            {
                Primitives.Hexagon drawingHex = toHexagon(ScreenSpaceDrawQuad);

                if (GLWrapper.IsMaskingActive)
                {
                    DrawClipped(ref drawingHex, Texture, DrawColourInfo.Colour, vertexAction: vertexAction);
                }
                else
                {
                    DrawTriangle(Texture, drawingHex.FarUpTriangle, DrawColourInfo.Colour, null, null,
                        new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
                    DrawTriangle(Texture, drawingHex.NearUpTriangle, DrawColourInfo.Colour, null, null,
                        new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
                    DrawTriangle(Texture, drawingHex.NearDownTriangle, DrawColourInfo.Colour, null, null,
                        new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
                    DrawTriangle(Texture, drawingHex.FarDownTriangle, DrawColourInfo.Colour, null, null,
                        new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
                }
            }
        }
    }
}
