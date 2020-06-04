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

        private readonly RectangleI atlasBounds;

        private static readonly Rgba32 transparent_black = new Rgba32(0, 0, 0, 0);

        public TextureGLAtlas(int width, int height, bool manualMipmaps, All filteringMode = All.Linear, int padding = 0)
            : base(width, height, manualMipmaps, filteringMode)
        {
            this.padding = padding;

            atlasBounds = new RectangleI(0, 0, Width, Height);

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

        internal override void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            // Can only perform padding when the bounds are a sub-part of the texture
            RectangleI middleBounds = upload.Bounds;

            if (middleBounds.IsEmpty || middleBounds.Width * middleBounds.Height > upload.Data.Length)
            {
                base.SetData(upload, wrapModeS, wrapModeT);
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
            base.SetData(upload, wrapModeS, wrapModeT);
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

                    var sideUpload = new ArrayPoolTextureUpload(sideBounds.Width, sideBounds.Height) { Bounds = sideBounds };

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
                        base.SetData(sideUpload, WrapMode.None, WrapMode.None);
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

                    var sideUpload = new ArrayPoolTextureUpload(sideBounds.Width, sideBounds.Height) { Bounds = sideBounds };

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
                        base.SetData(sideUpload, WrapMode.None, WrapMode.None);
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
                    var cornerUpload = new ArrayPoolTextureUpload(cornerBounds.Width, cornerBounds.Height) { Bounds = cornerBounds };
                    for (int j = 0; j < nCornerPixels; ++j)
                        cornerUpload.RawData[j] = cornerPixel;

                    base.SetData(cornerUpload, WrapMode.None, WrapMode.None);
                }
            }
        }
    }
}
