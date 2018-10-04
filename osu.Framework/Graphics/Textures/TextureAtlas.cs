// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL.Textures;
using OpenTK.Graphics.ES30;
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

        public void Reset()
        {
            subTextureBounds.Clear();
            currentY = 0;

            //may be zero in a headless context.
            if (atlasWidth == 0 || atlasHeight == 0)
                return;

            if (AtlasTexture == null)
                Logger.Log($"New TextureAtlas initialised {atlasWidth}x{atlasHeight}", LoggingTarget.Runtime, LogLevel.Debug);

            AtlasTexture = new TextureGLAtlas(atlasWidth, atlasHeight, manualMipmaps, filteringMode);

            using (var whiteTex = Add(3, 3))
                whiteTex.SetData(new TextureUpload(new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, whiteTex.Width, whiteTex.Height, Rgba32.White)));
        }

        private Vector2I findPosition(int width, int height)
        {
            if (atlasHeight == 0 || atlasWidth == 0) return Vector2I.Zero;

            if (currentY + height > atlasHeight)
            {
                Logger.Log($"TextureAtlas size exceeded; generating new {atlasWidth}x{atlasHeight} texture", LoggingTarget.Performance);
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

        internal TextureGL Add(int width, int height)
        {
            lock (textureRetrievalLock)
            {
                if (AtlasTexture == null)
                    Reset();

                Vector2I position = findPosition(width, height);
                RectangleI bounds = new RectangleI(position.X, position.Y, width, height);
                subTextureBounds.Add(bounds);

                return new TextureGLSub(bounds, AtlasTexture);
            }
        }
    }
}
