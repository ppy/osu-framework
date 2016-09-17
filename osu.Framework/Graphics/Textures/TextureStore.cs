//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using osu.Framework.IO.Stores;
using ImageMagick;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Textures
{
    public class TextureStore : ResourceStore<byte[]>
    {
        Dictionary<string, Texture> textureCache = new Dictionary<string, Texture>();

        private TextureAtlas atlas = new TextureAtlas(GLWrapper.MaxTextureSize, GLWrapper.MaxTextureSize);

        public float ScaleAdjust = 1;

        public TextureStore(IResourceStore<byte[]> store) : base(store)
        {
            AddExtension(@"png");
            AddExtension(@"jpg");
        }

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public new virtual Texture Get(string name)
        {
            Texture tex = null;

            try
            {
                if (textureCache.TryGetValue(name, out tex))
                {
                    //use existing TextureGL (but provide a new texture instance).
                    tex = tex != null ? new Texture(tex.TextureGL) : null;
                    return tex;
                }

                Stream s = base.GetStream($@"{name}");
                if (s == null) return null;

                using (MagickImage mainImage = new MagickImage(s))
                {
                    tex = atlas != null ? atlas.Add(mainImage.Width, mainImage.Height) : new Texture(mainImage.Width, mainImage.Height);
                    TextureUpload upload = new TextureUpload(mainImage.Width * mainImage.Height * 4);
                    mainImage.Write(new MemoryStream(upload.Data), MagickFormat.Rgba);
                    tex.SetData(upload);
                }


                //load available mipmaps
                //int level = 1;
                //int div = 2;

                //while (tex.Width / div > 0)
                //{
                //    s = base.GetStream($@"{name}/{div}");

                //    if (s == null) break;

                //    int w = tex.Width / div;
                //    int h = tex.Height / div;

                //    TextureUpload upload = new TextureUpload(w * h * 4)
                //    {
                //        Level = level
                //    };

                //    using (MagickImage image = new MagickImage(s))
                //    {
                //        if (image.Width != w || image.Height != h)
                //        {
                //            image.Resize(new MagickGeometry($"{w}x{h}!"));
                //        }

                //        image.Write(new MemoryStream(upload.Data), MagickFormat.Rgba);
                //    }

                //    tex.SetData(upload);

                //    level++;
                //    div *= 2;
                //}

                textureCache[name] = tex;

                return tex;
            }
            finally
            {
                if (tex != null && ScaleAdjust != 1)
                    tex.DpiScale = 1 / ScaleAdjust;
            }
        }
    }
}

