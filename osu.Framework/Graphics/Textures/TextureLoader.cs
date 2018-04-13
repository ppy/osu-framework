// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.IO;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL.Textures;

namespace osu.Framework.Graphics.Textures
{
    public static class TextureLoader
    {
        /// <summary>
        /// Creates a texture from a bitmap.
        /// </summary>
        /// <param name="bitmap">The bitmap to create the texture from.</param>
        /// <param name="atlas">The atlas to add the texture to.</param>
        /// <returns>The created texture.</returns>
        public static Texture FromBitmap(Bitmap bitmap, TextureAtlas atlas = null)
        {
            if (bitmap == null)
                return null;

            //int usableWidth = Math.Min(GLWrapper.MaxTextureSize, bitmap.Width);
            //int usableHeight = Math.Min(GLWrapper.MaxTextureSize, bitmap.Height);

            Texture tex = atlas == null ? new Texture(bitmap.Width, bitmap.Height) : atlas.Add(bitmap.Width, bitmap.Height);
            tex.SetData(bitmap);

            return tex;
        }

        /// <summary>
        /// Creates a texture from a data stream representing a bitmap.
        /// </summary>
        /// <param name="stream">The data stream containing the texture data.</param>
        /// <param name="atlas">The atlas to add the texture to.</param>
        /// <returns>The created texture.</returns>
        public static Texture FromStream(Stream stream, TextureAtlas atlas = null)
        {
            if (stream == null || stream.Length == 0)
                return null;

            try
            {
                using (Bitmap b = (Bitmap)Image.FromStream(stream, false, false))
                    return FromBitmap(b, atlas);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a texture from bytes representing a bitmap.
        /// </summary>
        /// <param name="data">The bytes representing a bitmap.</param>
        /// <param name="atlas">The atlas to add the texture to.</param>
        /// <returns>The created texture.</returns>
        public static Texture FromBytes(byte[] data, TextureAtlas atlas = null)
        {
            //todo: can be optimised with TextureUpload here to avoid allocations.
            if (data == null)
                return null;

            using (MemoryStream ms = new MemoryStream(data))
                return FromStream(ms, atlas);
        }

        /// <summary>
        /// Creates a texture from bytes laid out in BGRA format, row major.
        /// </summary>
        /// <param name="data">The raw bytes containing the texture in provided format, row major.</param>
        /// <param name="width">Width of the texture in pixels.</param>
        /// <param name="height">Height of the texture in pixels.</param>
        /// <param name="atlas">The atlas to add the texture to.</param>
        /// <param name="format">The pixel format of the data.</param>
        /// <returns>The created texture.</returns>
        public static Texture FromRawBytes(byte[] data, int width, int height, TextureAtlas atlas = null, PixelFormat format = PixelFormat.Rgba)
        {
            if (data == null)
                return null;

            Texture tex = atlas == null ? new Texture(width, height) : atlas.Add(width, height);

            var upload = new TextureUpload(data)
            {
                Format = format
            };
            tex.SetData(upload);
            return tex;
        }
    }
}
