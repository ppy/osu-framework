﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    public class TextureStore : ResourceStore<RawTexture>
    {
        Dictionary<string, Texture> textureCache = new Dictionary<string, Texture>();

        private TextureAtlas atlas = new TextureAtlas(GLWrapper.MaxTextureSize, GLWrapper.MaxTextureSize);

        public float ScaleAdjust = 1;

        public TextureStore(IResourceStore<RawTexture> store = null) : base(store)
        {
            AddExtension(@"png");
            AddExtension(@"jpg");
        }

        private Texture getTexture(string name)
        {
            RawTexture raw = base.Get($@"{name}");
            if (raw == null) return null;
            
            Texture tex = atlas != null ? atlas.Add(raw.Width, raw.Height) : new Texture(raw.Width, raw.Height);
            tex.SetData(new TextureUpload(raw.Pixels)
            {
                Bounds = new Rectangle(0, 0, raw.Width, raw.Height),
                Format = raw.PixelFormat,
            });

            return tex;
        }

        public async Task<Texture> GetAsync(string name) => await Task.Run(() => Get(name));

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public virtual new Texture Get(string name)
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

                tex = getTexture(name);
                    
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
