// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL.Textures;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public class TextureAtlas
    {
        // We are adding an extra padding on top of the padding required by
        // mipmap blending in order to support smooth edges without antialiasing which requires
        // inflating texture rectangles.
        internal const int PADDING = (1 << TextureGLSingle.MAX_MIPMAP_LEVELS) * Sprite.MAX_EDGE_SMOOTHNESS;
        internal const int WHITE_PIXEL_SIZE = 3 * (1 << TextureGLSingle.MAX_MIPMAP_LEVELS);

        private readonly List<RectangleI> subTextureBounds = new List<RectangleI>();
        internal TextureGLSingle AtlasTexture;

        private readonly int atlasWidth;
        private readonly int atlasHeight;

        private Vector2I currentPosition;

        private int mipmapLevels => (int)Math.Log(atlasWidth, 2);

        internal TextureWhitePixel WhitePixel
        {
            get
            {
                if (AtlasTexture == null)
                    Reset();

                return new TextureWhitePixel(new TextureGLAtlasWhite(AtlasTexture));
            }
        }

        private readonly bool manualMipmaps;
        private readonly All filteringMode;
        private readonly object textureRetrievalLock = new object();

        public TextureAtlas(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear)
        {
            atlasWidth = width;
            atlasHeight = height;
            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
        }

        private int exceedCount;

        public void Reset()
        {
            subTextureBounds.Clear();
            currentPosition = Vector2I.Zero;

            AtlasTexture = new TextureGLAtlas(atlasWidth, atlasHeight, manualMipmaps, filteringMode);

            using (var whiteTex = Add(WHITE_PIXEL_SIZE, WHITE_PIXEL_SIZE))
                whiteTex.SetData(new TextureUpload(new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, whiteTex.Width, whiteTex.Height, Rgba32.White)));

            currentPosition = new Vector2I(Math.Max(currentPosition.X, PADDING), PADDING);
        }

        private Vector2I findPosition(int width, int height)
        {
            if (AtlasTexture == null)
            {
                Logger.Log($"TextureAtlas initialised ({atlasWidth}x{atlasHeight})", LoggingTarget.Performance);
                Reset();
            }
            else if (currentPosition.Y + height > atlasHeight - PADDING)
            {
                Logger.Log($"TextureAtlas size exceeded {++exceedCount} time(s); generating new texture ({atlasWidth}x{atlasHeight})", LoggingTarget.Performance);
                Reset();
            }
            else if (currentPosition.X + width > atlasWidth - PADDING)
            {
                int maxY = 0;

                foreach (RectangleI bounds in subTextureBounds)
                    maxY = Math.Max(maxY, bounds.Bottom + PADDING);

                subTextureBounds.Clear();
                currentPosition = new Vector2I(PADDING, maxY);

                return findPosition(width, height);
            }

            var result = currentPosition;
            currentPosition.X += width + PADDING;

            return result;
        }

        /// <summary>
        /// Add (allocate) a new texture in the atlas.
        /// </summary>
        /// <param name="width">The width of the requested texture.</param>
        /// <param name="height">The height of the requested texture.</param>
        /// <returns>A texture, or null if the requested size exceeds the atlas' bounds.</returns>
        internal TextureGL Add(int width, int height)
        {
            if (width > atlasWidth || height > atlasHeight)
                return null;

            lock (textureRetrievalLock)
            {
                Vector2I position = findPosition(width, height);
                RectangleI bounds = new RectangleI(position.X, position.Y, width, height);
                subTextureBounds.Add(bounds);

                return new TextureGLSub(bounds, AtlasTexture);
            }
        }
    }
}
