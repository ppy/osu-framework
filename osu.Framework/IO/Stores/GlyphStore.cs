//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Cyotek.Drawing.BitmapFont;

namespace osu.Framework.IO.Stores
{
    public class GlyphStore : IResourceStore<byte[]>
    {
        private string assetName;

        const float default_size = 96;

        ResourceStore<byte[]> store;
        private BitmapFont font;

        Dictionary<int, Bitmap> texturePages = new Dictionary<int, Bitmap>();

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
                throw new Exception($@"Couldn't load font asset from {assetName}.");
            }
        }

        public byte[] Get(string name)
        {
            string[] parts = name.Split('/');
            return Get(parts[0], parts.Length == 1 ? 1 : 1f / Int32.Parse(parts[1]));
        }

        public byte[] Get(string name, float scale = 1)
        {
            Character c;

            //face.SetCharSize(0, default_size * scale, 0, 96);

            if (!font.Characters.TryGetValue(name[0], out c))
                return null;

            Bitmap page = getTexturePage(c.TexturePage);

            Bitmap glyphTexture = new Bitmap(c.Bounds.Width + c.Offset.X, c.Bounds.Height + c.Offset.Y);
            using (var g = System.Drawing.Graphics.FromImage(glyphTexture))
                g.DrawImage(page, new Rectangle(c.Offset.X, c.Offset.Y, c.Bounds.Width, c.Bounds.Height), c.Bounds, GraphicsUnit.Pixel);

            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(glyphTexture, typeof(byte[]));
        }

        private Bitmap getTexturePage(int texturePage)
        {
            Bitmap t;


            if (!texturePages.TryGetValue(texturePage, out t))
            {
                texturePages[texturePage] = t = new Bitmap(store.GetStream($@"{assetName}_{texturePage}.png"));
            }

            return t;
        }

        public Stream GetStream(string name)
        {
            return new MemoryStream(Get(name));
        }
    }
}
