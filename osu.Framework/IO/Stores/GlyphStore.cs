// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using Cyotek.Drawing.BitmapFont;
using ImageMagick;

namespace osu.Framework.IO.Stores
{
    public class GlyphStore : IResourceStore<byte[]>
    {
        private string assetName;

        ResourceStore<byte[]> store;
        private BitmapFont font;

        Dictionary<int, MagickImage> texturePages = new Dictionary<int, MagickImage>();

        public GlyphStore(ResourceStore<byte[]> store, string assetName = null)
        {
            this.store = store;
            this.assetName = assetName;

            try
            {
                font = new BitmapFont();
                font.LoadText(store.GetStream($@"{assetName}.fnt"));
                //ScaleAdjust = font.StretchedHeight;
            }
            catch
            {
                throw new FontLoadException(assetName);
            }
        }

        public byte[] Get(string name)
        {
            string[] parts = name.Split('/');
            return Get(parts[0], parts.Length == 1 ? 1 : 1f / int.Parse(parts[1]));
        }

        public byte[] Get(string name, float scale = 1)
        {
            Character c;

            if (!font.Characters.TryGetValue(name[0], out c))
                return null;

            MagickImage page = getTexturePage(c.TexturePage);

            MagickImage glyph = new MagickImage(new MagickColor(65535, 65535, 65535, 0), c.Bounds.Width + c.Offset.X, c.Bounds.Height + c.Offset.Y);

            glyph.CopyPixels(page, new MagickGeometry(c.Bounds.X, c.Bounds.Y, c.Bounds.Width, c.Bounds.Height), c.Offset.X, c.Offset.Y);
            glyph.RePage();

            //todo: we can return MagickImage here instead of Bmp with a bit of refactoring.
            return glyph.ToByteArray(MagickFormat.Bmp);
        }

        private MagickImage getTexturePage(int texturePage)
        {
            MagickImage t;

            if (!texturePages.TryGetValue(texturePage, out t))
                texturePages[texturePage] = t = new MagickImage(store.GetStream($@"{assetName}_{texturePage}.png"));

            return t;
        }

        public Stream GetStream(string name)
        {
            return new MemoryStream(Get(name));
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
