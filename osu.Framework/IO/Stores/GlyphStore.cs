// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using Cyotek.Drawing.BitmapFont;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Textures.Png;

namespace osu.Framework.IO.Stores
{
    public class GlyphStore : IResourceStore<RawTexture>
    {
        private string assetName;

        const float default_size = 96;

        ResourceStore<byte[]> store;
        private BitmapFont font;

        Dictionary<int, RawTexture> texturePages = new Dictionary<int, RawTexture>();

        public GlyphStore(ResourceStore<byte[]> store, string assetName = null)
        {
            this.store = store;
            this.assetName = assetName;

            try
            {
                font = new BitmapFont();
                font.LoadText(store.GetStream($@"{assetName}.fnt"));
            }
            catch
            {
                throw new FontLoadException(assetName);
            }
        }

        public RawTexture Get(string name)
        {
            return Get(name, 1);
        }
        
        public RawTexture Get(string name, float scale = 1)
        {
            Character c;

            if (!font.Characters.TryGetValue(name[0], out c))
                return null;

            RawTexture page = getTexturePage(c.TexturePage);

            int width = c.Bounds.Width + c.Offset.X;
            int height = c.Bounds.Height + c.Offset.Y;
            int length = width * height * 4;
            byte[] pixels = new byte[length];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int desti = y * width * 4 + x * 4;
                    if (x >= c.Offset.X && y >= c.Offset.Y
                        && x < c.Bounds.X && y < c.Bounds.Y)
                    {
                        int srci = (c.Bounds.Y + y - c.Offset.Y) * page.Width * 4
                            + (c.Bounds.X + x - c.Offset.X) * 4;
                        pixels[desti] = page.Pixels[srci];
                        pixels[desti + 1] = page.Pixels[srci + 1];
                        pixels[desti + 2] = page.Pixels[srci + 2];
                        pixels[desti + 3] = page.Pixels[srci + 3];
                    }
                    else
                    {
                        pixels[desti] = 255;
                        pixels[desti + 1] = 255;
                        pixels[desti + 2] = 255;
                        pixels[desti + 3] = 0;
                    }
                }
            }

            return new RawTexture
            {
                Pixels = pixels,
                PixelFormat = OpenTK.Graphics.ES20.PixelFormat.Rgba,
                Width = width,
                Height = height,
            };
        }

        private RawTexture getTexturePage(int texturePage)
        {
            RawTexture t;
            if (!texturePages.TryGetValue(texturePage, out t))
            {
                t = new RawTexture();
                using (var stream = store.GetStream($@"{assetName}_{texturePage}.png"))
                {
                    var reader = new PngReader();
                    t.Pixels = reader.Read(stream);
                    t.PixelFormat = OpenTK.Graphics.ES20.PixelFormat.Rgba;
                    t.Width = reader.Width;
                    t.Height = reader.Height;
                }
                texturePages[texturePage] = t;
            }
            return t;
        }

        public Stream GetStream(string name)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class FontLoadException : Exception
    {
        public FontLoadException(string assetName):
            base($@"Couldn't load font asset from {assetName}.")
        {
        }
    }
}
