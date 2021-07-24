// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    /// <summary>
    /// A TextureGL which is acting as the backing for an atlas.
    /// </summary>
    internal class TextureGLAtlas : TextureGLSingle
    {
        /// <summary>
        /// The amount of padding around each texture in the atlas.
        /// </summary>
        private readonly int padding;

        private readonly RectangleI atlasBounds;

        private static readonly Rgba32 transparent_black = new Rgba32(0, 0, 0, 0);

        public TextureGLAtlas(int width, int height, bool manualMipmaps, All filteringMode = All.Linear, int padding = 0)
            : base(width, height, manualMipmaps, filteringMode)
        {
            this.padding = padding;

            atlasBounds = new RectangleI(0, 0, Width, Height);
        }

        internal override void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity)
        {
            // Can only perform padding when the bounds are a sub-part of the texture
            RectangleI middleBounds = upload.Bounds;

            if (middleBounds.IsEmpty || middleBounds.Width * middleBounds.Height > upload.Data.Length)
            {
                // For a texture atlas, we don't care about opacity, so we avoid
                // any computations related to it by assuming it to be mixed.
                base.SetData(upload, wrapModeS, wrapModeT, Opacity.Mixed);
                return;
            }

            int actualPadding = padding / (1 << upload.Level);

            if (wrapModeS != WrapMode.None && wrapModeT != WrapMode.None)
                uploadCornerPadding(upload, middleBounds, actualPadding);

            if (wrapModeS != WrapMode.None)
                uploadHorizontalPadding(upload, middleBounds, actualPadding);

            if (wrapModeT != WrapMode.None)
                uploadVerticalPadding(upload, middleBounds, actualPadding);

            // Upload the middle part of the texture
            // For a texture atlas, we don't care about opacity, so we avoid
            // any computations related to it by assuming it to be mixed.
            base.SetData(upload, wrapModeS, wrapModeT, Opacity.Mixed);
        }

        private void uploadVerticalPadding(ITextureUpload upload, RectangleI middleBounds, int actualPadding)
        {
            RectangleI[] sideBoundsArray =
            {
                new RectangleI(middleBounds.X, middleBounds.Y - actualPadding, middleBounds.Width, actualPadding).Intersect(atlasBounds), // Top
                new RectangleI(middleBounds.X, middleBounds.Y + middleBounds.Height, middleBounds.Width, actualPadding).Intersect(atlasBounds), // Bottom
            };

            int[] sideIndices =
            {
                0, // Top
                (middleBounds.Height - 1) * middleBounds.Width, // Bottom
            };

            for (int i = 0; i < 2; ++i)
            {
                RectangleI sideBounds = sideBoundsArray[i];

                if (!sideBounds.IsEmpty)
                {
                    bool allTransparentBlack = true;
                    int index = sideIndices[i];

                    var sideUpload = new MemoryAllocatorTextureUpload(sideBounds.Width, sideBounds.Height) { Bounds = sideBounds };

                    for (int y = 0; y < sideBounds.Height; ++y)
                    {
                        for (int x = 0; x < sideBounds.Width; ++x)
                        {
                            Rgba32 pixel = upload.Data[index + x];
                            allTransparentBlack &= pixel == transparent_black;
                            sideUpload.RawData[y * sideBounds.Width + x] = pixel;
                        }
                    }

                    // Only upload padding if the border isn't completely transparent.
                    if (!allTransparentBlack)
                    {
                        // For a texture atlas, we don't care about opacity, so we avoid
                        // any computations related to it by assuming it to be mixed.
                        base.SetData(sideUpload, WrapMode.None, WrapMode.None, Opacity.Mixed);
                    }
                }
            }
        }

        private void uploadHorizontalPadding(ITextureUpload upload, RectangleI middleBounds, int actualPadding)
        {
            RectangleI[] sideBoundsArray =
            {
                new RectangleI(middleBounds.X - actualPadding, middleBounds.Y, actualPadding, middleBounds.Height).Intersect(atlasBounds), // Left
                new RectangleI(middleBounds.X + middleBounds.Width, middleBounds.Y, actualPadding, middleBounds.Height).Intersect(atlasBounds), // Right
            };

            int[] sideIndices =
            {
                0, // Left
                middleBounds.Width - 1, // Right
            };

            for (int i = 0; i < 2; ++i)
            {
                RectangleI sideBounds = sideBoundsArray[i];

                if (!sideBounds.IsEmpty)
                {
                    bool allTransparentBlack = true;
                    int index = sideIndices[i];

                    var sideUpload = new MemoryAllocatorTextureUpload(sideBounds.Width, sideBounds.Height) { Bounds = sideBounds };

                    int stride = middleBounds.Width;

                    for (int y = 0; y < sideBounds.Height; ++y)
                    {
                        for (int x = 0; x < sideBounds.Width; ++x)
                        {
                            Rgba32 pixel = upload.Data[index + y * stride];
                            allTransparentBlack &= pixel == transparent_black;
                            sideUpload.RawData[y * sideBounds.Width + x] = pixel;
                        }
                    }

                    // Only upload padding if the border isn't completely transparent.
                    if (!allTransparentBlack)
                    {
                        // For a texture atlas, we don't care about opacity, so we avoid
                        // any computations related to it by assuming it to be mixed.
                        base.SetData(sideUpload, WrapMode.None, WrapMode.None, Opacity.Mixed);
                    }
                }
            }
        }

        private void uploadCornerPadding(ITextureUpload upload, RectangleI middleBounds, int actualPadding)
        {
            RectangleI[] cornerBoundsArray =
            {
                new RectangleI(middleBounds.X - actualPadding, middleBounds.Y - actualPadding, actualPadding, actualPadding).Intersect(atlasBounds), // TopLeft
                new RectangleI(middleBounds.X + middleBounds.Width, middleBounds.Y - actualPadding, actualPadding, actualPadding).Intersect(atlasBounds), // TopRight
                new RectangleI(middleBounds.X - actualPadding, middleBounds.Y + middleBounds.Height, actualPadding, actualPadding).Intersect(atlasBounds), // BottomLeft
                new RectangleI(middleBounds.X + middleBounds.Width, middleBounds.Y + middleBounds.Height, actualPadding, actualPadding).Intersect(atlasBounds), // BottomRight
            };

            int[] cornerIndices =
            {
                0, // TopLeft
                middleBounds.Width - 1, // TopRight
                (middleBounds.Height - 1) * middleBounds.Width, // BottomLeft
                (middleBounds.Height - 1) * middleBounds.Width + middleBounds.Width - 1, // BottomRight
            };

            for (int i = 0; i < 4; ++i)
            {
                RectangleI cornerBounds = cornerBoundsArray[i];
                int nCornerPixels = cornerBounds.Width * cornerBounds.Height;
                Rgba32 cornerPixel = upload.Data[cornerIndices[i]];

                // Only upload if we have a non-zero size and if the colour isn't already transparent black
                if (nCornerPixels > 0 && cornerPixel != transparent_black)
                {
                    var cornerUpload = new MemoryAllocatorTextureUpload(cornerBounds.Width, cornerBounds.Height) { Bounds = cornerBounds };
                    for (int j = 0; j < nCornerPixels; ++j)
                        cornerUpload.RawData[j] = cornerPixel;

                    // For a texture atlas, we don't care about opacity, so we avoid
                    // any computations related to it by assuming it to be mixed.
                    base.SetData(cornerUpload, WrapMode.None, WrapMode.None, Opacity.Mixed);
                }
            }
        }
    }
}
