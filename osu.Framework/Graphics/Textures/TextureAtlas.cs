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
        private const int padding = (1 << TextureGLSingle.MAX_MIPMAP_LEVELS) + Sprite.MAX_EDGE_SMOOTHNESS * 2;

        private readonly List<RectangleI> subTextureBounds = new List<RectangleI>();
        internal TextureGLSingle AtlasTexture;

        private readonly int atlasWidth;
        private readonly int atlasHeight;

        private int currentY;

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
            currentY = 0;

            AtlasTexture = new TextureGLAtlas(atlasWidth, atlasHeight, manualMipmaps, filteringMode);

            using (var whiteTex = Add(3, 3))
                whiteTex.SetData(new TextureUpload(new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, whiteTex.Width, whiteTex.Height, Rgba32.White)));
        }

        private Vector2I findPosition(int width, int height)
        {
            if (AtlasTexture == null)
            {
                Logger.Log($"TextureAtlas initialised ({atlasWidth}x{atlasHeight})", LoggingTarget.Performance);
                Reset();
            }
            else if (currentY + height > atlasHeight)
            {
                Logger.Log($"TextureAtlas size exceeded {++exceedCount} time(s); generating new texture ({atlasWidth}x{atlasHeight})", LoggingTarget.Performance);
                Reset();
            }

            // Super naive implementation only going from left to right.
            Vector2I res = new Vector2I(0, currentY);

            int maxY = currentY;

            foreach (RectangleI bounds in subTextureBounds)
            {
                // +1 is required to prevent aliasing issues with sub-pixel positions while drawing. Bordering edged of other textures can show without it.
                res.X = Math.Max(res.X, bounds.Right + padding);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            if (res.X + width > atlasWidth)
            {
                // +1 is required to prevent aliasing issues with sub-pixel positions while drawing. Bordering edged of other textures can show without it.
                currentY = maxY + padding;
                subTextureBounds.Clear();
                res = findPosition(width, height);
            }

            return res;
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
