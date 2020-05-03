// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
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
        /// Contains all currently-active <see cref="TextureGLAtlas"/>es.
        /// </summary>
        private static readonly LockedWeakList<TextureGLAtlas> all_atlases = new LockedWeakList<TextureGLAtlas>();

        /// <summary>
        /// The amount of padding around each texture in the atlas.
        /// </summary>
        private readonly int padding;

        /// <summary>
        /// Invoked when a new <see cref="TextureGLAtlas"/> is created.
        /// </summary>
        /// <remarks>
        /// Invocation from the draw or update thread cannot be assumed.
        /// </remarks>
        public static event Action<TextureGLAtlas> TextureCreated;

        public TextureGLAtlas(int width, int height, bool manualMipmaps, All filteringMode = All.Nearest, int padding = 0)
            : base(width, height, manualMipmaps, filteringMode)
        {
            this.padding = padding;

            all_atlases.Add(this);

            TextureCreated?.Invoke(this);
        }

        /// <summary>
        /// Retrieves all currently-active <see cref="TextureGLAtlas"/>es.
        /// </summary>
        public static TextureGLAtlas[] GetAllAtlases() => all_atlases.ToArray();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            all_atlases.Remove(this);
        }

        public override void SetData(ITextureUpload upload)
        {
            // Can only perform padding when the bounds are a sub-part of the texture
            var middleBounds = upload.Bounds;

            if (middleBounds.IsEmpty || middleBounds.Width * middleBounds.Height > upload.Data.Length)
            {
                base.SetData(upload);
                return;
            }

            int actualPadding = padding / (1 << upload.Level);

            RectangleI bounds = new RectangleI(0, 0, Width, Height);
            Rgba32 transparentBlack = new Rgba32(0, 0, 0, 0);

            // Upload padded corners
            var cornerBoundsArray = new[]
            {
                new RectangleI(middleBounds.X - actualPadding, middleBounds.Y - actualPadding, actualPadding, actualPadding).Intersect(bounds), // TopLeft
                new RectangleI(middleBounds.X + middleBounds.Width, middleBounds.Y - actualPadding, actualPadding, actualPadding).Intersect(bounds), // TopRight
                new RectangleI(middleBounds.X - actualPadding, middleBounds.Y + middleBounds.Height, actualPadding, actualPadding).Intersect(bounds), // BottomLeft
                new RectangleI(middleBounds.X + middleBounds.Width, middleBounds.Y + middleBounds.Height, actualPadding, actualPadding).Intersect(bounds), // BottomRight
            };

            int[] cornerIndices = new[]
            {
                0, // TopLeft
                middleBounds.Width - 1, // TopRight
                (middleBounds.Height - 1) * middleBounds.Width, // BottomLeft
                (middleBounds.Height - 1) * middleBounds.Width + middleBounds.Width - 1, // BottomRight
            };

            for (int i = 0; i < 4; ++i)
            {
                var cornerBounds = cornerBoundsArray[i];
                int nCornerPixels = cornerBounds.Width * cornerBounds.Height;
                var cornerPixel = upload.Data[cornerIndices[i]];

                // Only upload if we have a non-zero size and if the colour isn't already transparent black
                if (nCornerPixels > 0 && cornerPixel != transparentBlack)
                {
                    var cornerUpload = new ArrayPoolTextureUpload(cornerBounds.Width, cornerBounds.Height) { Bounds = cornerBounds };
                    for (int j = 0; j < nCornerPixels; ++j)
                        cornerUpload.RawData[j] = cornerPixel;

                    base.SetData(cornerUpload);
                }
            }

            // Upload padded sides
            var sideBoundsArray = new[]
            {
                new RectangleI(middleBounds.X - actualPadding, middleBounds.Y, actualPadding, middleBounds.Height).Intersect(bounds), // Left
                new RectangleI(middleBounds.X + middleBounds.Width, middleBounds.Y, actualPadding, middleBounds.Height).Intersect(bounds), // Right
                new RectangleI(middleBounds.X, middleBounds.Y - actualPadding, middleBounds.Width, actualPadding).Intersect(bounds), // Top
                new RectangleI(middleBounds.X, middleBounds.Y + middleBounds.Height, middleBounds.Width, actualPadding).Intersect(bounds), // Bottom
            };

            var sideIndices = new[]
            {
                0, // Left
                middleBounds.Width - 1, // Right
                0, // Top
                (middleBounds.Height - 1) * middleBounds.Width, // Bottom
            };

            var sideStrides = new[]
            {
                middleBounds.Width,
                middleBounds.Width,
                1,
                1,
            };

            for (int i = 0; i < 4; ++i)
            {
                var sideBounds = sideBoundsArray[i];
                int nSidePixels = sideBounds.Width * sideBounds.Height;

                if (nSidePixels > 0)
                {
                    bool allTransparentBlack = true;
                    int index = sideIndices[i];
                    int stride = sideStrides[i];

                    var cornerUpload = new ArrayPoolTextureUpload(sideBounds.Width, sideBounds.Height) { Bounds = sideBounds };

                    // Right & left
                    if (i < 2)
                    {
                        for (int y = 0; y < sideBounds.Height; ++y)
                        {
                            for (int x = 0; x < sideBounds.Width; ++x)
                            {
                                var pixel = upload.Data[index + y * stride];
                                allTransparentBlack &= pixel == transparentBlack;
                                cornerUpload.RawData[y * sideBounds.Width + x] = pixel;
                            }
                        }
                    }
                    // Top & bottom
                    else
                    {
                        for (int y = 0; y < sideBounds.Height; ++y)
                        {
                            for (int x = 0; x < sideBounds.Width; ++x)
                            {
                                var pixel = upload.Data[index + x * stride];
                                allTransparentBlack &= pixel == transparentBlack;
                                cornerUpload.RawData[y * sideBounds.Width + x] = pixel;
                            }
                        }
                    }

                    // Only upload padding if the border isn't completely transparent.
                    if (!allTransparentBlack)
                        base.SetData(cornerUpload);
                }
            }

            // Upload the middle part of the texture
            base.SetData(upload);
        }
    }
}
