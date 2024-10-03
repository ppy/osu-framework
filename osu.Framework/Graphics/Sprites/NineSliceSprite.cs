// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A <see cref="Sprite"/> that uses <see href="https://en.wikipedia.org/wiki/9-slice_scaling">9-slice scaling</see> to stretch a Texture.
    /// When resizing a <see cref="NineSliceSprite"/>, the corners will remain unscaled.
    ///
    ///     A                          B
    ///   +---+----------------------+---+
    /// C | 1 |          2           | 3 |
    ///   +---+----------------------+---+
    ///   |   |                      |   |
    ///   | 4 |          5           | 6 |
    ///   |   |                      |   |
    ///   +---+----------------------+---+
    /// D | 7 |          8           | 9 |
    ///   +---+----------------------+---+
    ///
    /// When changing the <see cref="Drawable.Width"/>, areas 1, 4, 7, 3, 6, and 9 (A and B) will remain unscaled.
    /// When changing the <see cref="Drawable.Height"/>, areas 1, 2, 3, 7, 8, and 9 (C and D) will remain unscaled.
    /// </summary>
    public partial class NineSliceSprite : Sprite
    {
        private MarginPadding textureInset;

        /// <summary>
        /// The inset of the texture that will remain unscaled when resizing this <see cref="NineSliceSprite"/>.
        /// May be absolute or relative units (controlled by <see cref="TextureInsetRelativeAxes"/>).
        /// </summary>
        public MarginPadding TextureInset
        {
            get => textureInset;
            set
            {
                if (textureInset.Equals(value))
                    return;

                textureInset = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        private Axes textureInsetRelativeAxes;

        /// <summary>
        /// Controls which <see cref="Axes"/> of <see cref="TextureInset"/> are relative w.r.t.
        /// <see cref="Sprite.Texture"/>'s <see cref="Texture.DisplaySize"/> (from 0 to 1) rather than absolute.
        /// </summary>
        /// <remarks>
        /// When setting this property, the <see cref="TextureInset"/> is converted such that the absolute TextureInset
        /// remains invariant.
        /// </remarks>
        public Axes TextureInsetRelativeAxes
        {
            get => textureInsetRelativeAxes;
            set
            {
                if (textureInsetRelativeAxes == value)
                    return;

                if (Texture != null)
                {
                    Vector2 textureSize = Texture.DisplaySize;

                    Vector2 conversion = Vector2.One;

                    if ((value & Axes.X) > 0 && (textureInsetRelativeAxes & Axes.X) == 0)
                        conversion.X = Precision.AlmostEquals(textureSize.X, 0) ? 0 : 1 / textureSize.X;
                    else if ((value & Axes.X) == 0 && (textureInsetRelativeAxes & Axes.X) > 0)
                        conversion.X = textureSize.X;

                    if ((value & Axes.Y) > 0 && (textureInsetRelativeAxes & Axes.Y) == 0)
                        conversion.Y = Precision.AlmostEquals(textureSize.Y, 0) ? 0 : 1 / textureSize.Y;
                    else if ((value & Axes.Y) == 0 && (textureInsetRelativeAxes & Axes.Y) > 0)
                        conversion.Y = textureSize.Y;

                    textureInset *= conversion;
                }

                textureInsetRelativeAxes = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        internal MarginPadding RelativeTextureInset
        {
            get
            {
                if (Texture == null)
                    return default;

                Vector2 conversion = Vector2.One;

                if ((TextureInsetRelativeAxes & Axes.X) == 0)
                    conversion.X = Precision.AlmostEquals(Texture.DisplayWidth, 0) ? 0 : 1 / Texture.DisplayWidth;

                if ((TextureInsetRelativeAxes & Axes.Y) == 0)
                    conversion.Y = Precision.AlmostEquals(Texture.DisplayHeight, 0) ? 0 : 1 / Texture.DisplayHeight;

                return textureInset * conversion;
            }
        }

        internal MarginPadding RelativeGeometryInset
        {
            get
            {
                if (Texture == null)
                    return default;

                var result = textureInset;

                var conversion = new Vector2(
                    Precision.AlmostEquals(DrawWidth, 0) ? 0 : 1 / DrawWidth,
                    Precision.AlmostEquals(DrawHeight, 0) ? 0 : 1 / DrawHeight
                );

                if ((TextureInsetRelativeAxes & Axes.X) != 0)
                    conversion.X *= Texture.DisplayWidth;
                if ((TextureInsetRelativeAxes & Axes.Y) != 0)
                    conversion.Y *= Texture.DisplayHeight;

                return result * conversion;
            }
        }

        protected override DrawNode CreateDrawNode() => new NineSliceSpriteDrawNode(this);

        private class NineSliceSpriteDrawNode : SpriteDrawNode
        {
            public NineSliceSpriteDrawNode(NineSliceSprite source)
                : base(source)
            {
            }

            protected new NineSliceSprite Source => (NineSliceSprite)base.Source;

            protected override void Blit(IRenderer renderer)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                for (int i = 0; i < DrawQuads.Length; i++)
                    renderer.DrawQuad(Texture, DrawQuads[i], DrawColourInfo.Colour, null, null, Vector2.Zero, null, TextureRects[i]);
            }

            protected readonly RectangleF[] TextureRects = new RectangleF[9];
            protected readonly Quad[] DrawQuads = new Quad[9];

            public override void ApplyState()
            {
                base.ApplyState();

                computeTextureRects(Source.RelativeTextureInset);
                computeDrawQuads(Source.RelativeGeometryInset);
            }

            private void computeDrawQuads(MarginPadding inset)
            {
                DrawQuads[0] = computePart(Anchor.TopLeft);
                DrawQuads[1] = computePart(Anchor.TopCentre);
                DrawQuads[2] = computePart(Anchor.TopRight);
                DrawQuads[3] = computePart(Anchor.CentreLeft);
                DrawQuads[4] = computePart(Anchor.Centre);
                DrawQuads[5] = computePart(Anchor.CentreRight);
                DrawQuads[6] = computePart(Anchor.BottomLeft);
                DrawQuads[7] = computePart(Anchor.BottomCentre);
                DrawQuads[8] = computePart(Anchor.BottomRight);

                Quad computePart(Anchor anchor)
                {
                    Quad drawQuad = ScreenSpaceDrawQuad;

                    if ((anchor & Anchor.x0) > 0)
                        drawQuad = horizontalSlice(drawQuad, 0, inset.Left);
                    else if ((anchor & Anchor.x1) > 0)
                        drawQuad = horizontalSlice(drawQuad, inset.Left, 1 - inset.Right);
                    else if ((anchor & Anchor.x2) > 0)
                        drawQuad = horizontalSlice(drawQuad, 1 - inset.Right, 1);

                    if ((anchor & Anchor.y0) > 0)
                        drawQuad = verticalSlice(drawQuad, 0, inset.Top);
                    else if ((anchor & Anchor.y1) > 0)
                        drawQuad = verticalSlice(drawQuad, inset.Top, 1 - inset.Bottom);
                    else if ((anchor & Anchor.y2) > 0)
                        drawQuad = verticalSlice(drawQuad, 1 - inset.Bottom, 1);

                    return drawQuad;
                }

                static Quad horizontalSlice(Quad quad, float start, float end) =>
                    new Quad(
                        Vector2.Lerp(quad.TopLeft, quad.TopRight, start),
                        Vector2.Lerp(quad.TopLeft, quad.TopRight, end),
                        Vector2.Lerp(quad.BottomLeft, quad.BottomRight, start),
                        Vector2.Lerp(quad.BottomLeft, quad.BottomRight, end)
                    );

                static Quad verticalSlice(Quad quad, float start, float end) =>
                    new Quad(
                        Vector2.Lerp(quad.TopLeft, quad.BottomLeft, start),
                        Vector2.Lerp(quad.TopRight, quad.BottomRight, start),
                        Vector2.Lerp(quad.TopLeft, quad.BottomLeft, end),
                        Vector2.Lerp(quad.TopRight, quad.BottomRight, end)
                    );
            }

            private void computeTextureRects(MarginPadding inset)
            {
                TextureRects[0] = computePart(Anchor.TopLeft);
                TextureRects[1] = computePart(Anchor.TopCentre);
                TextureRects[2] = computePart(Anchor.TopRight);
                TextureRects[3] = computePart(Anchor.CentreLeft);
                TextureRects[4] = computePart(Anchor.Centre);
                TextureRects[5] = computePart(Anchor.CentreRight);
                TextureRects[6] = computePart(Anchor.BottomLeft);
                TextureRects[7] = computePart(Anchor.BottomCentre);
                TextureRects[8] = computePart(Anchor.BottomRight);

                RectangleF computePart(Anchor anchor)
                {
                    var textureCoords = TextureCoords;

                    if ((anchor & Anchor.x0) > 0)
                    {
                        textureCoords.Width *= inset.Left;
                    }
                    else if ((anchor & Anchor.x1) > 0)
                    {
                        textureCoords.X += textureCoords.Width * inset.Left;
                        textureCoords.Width *= 1 - inset.TotalHorizontal;
                    }
                    else if ((anchor & Anchor.x2) > 0)
                    {
                        textureCoords.X += textureCoords.Width * (1 - inset.Right);
                        textureCoords.Width *= inset.Right;
                    }

                    if ((anchor & Anchor.y0) > 0)
                    {
                        textureCoords.Height *= inset.Top;
                    }
                    else if ((anchor & Anchor.y1) > 0)
                    {
                        textureCoords.Y += textureCoords.Height * inset.Top;
                        textureCoords.Height *= 1 - inset.TotalVertical;
                    }
                    else if ((anchor & Anchor.y2) > 0)
                    {
                        textureCoords.Y += textureCoords.Height * (1 - inset.Bottom);
                        textureCoords.Height *= inset.Bottom;
                    }

                    return textureCoords;
                }
            }
        }
    }
}
