// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using osu.Framework.Graphics.Textures.Png;
using osu.Framework.IO.Stores;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureLoaderStore : ResourceStore<RawTexture>
    {
        private IResourceStore<byte[]> Store { get; set; }
    
        public RawTextureLoaderStore(IResourceStore<byte[]> store)
        {
            Store = store;
            (store as ResourceStore<byte[]>)?.AddExtension(@"png");
            (store as ResourceStore<byte[]>)?.AddExtension(@"jpg");
        }

        private RawTexture loadPng(Stream stream)
        {
            RawTexture t = new RawTexture();
            var reader = new PngReader();
            t.Pixels = reader.Read(stream);
            t.PixelFormat = OpenTK.Graphics.ES20.PixelFormat.Rgba;
            t.Width = reader.Width;
            t.Height = reader.Height;
            return t;
        }

        private RawTexture loadOther(Stream stream)
        {
            RawTexture t = new RawTexture();
            using (var bmp = (Bitmap)Image.FromStream(stream))
            {
                t.Pixels = new byte[bmp.Width * bmp.Height * 4];
                t.Width = bmp.Width;
                t.Height = bmp.Height;
                t.PixelFormat = OpenTK.Graphics.ES20.PixelFormat.Rgba;
                var pixels = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    unsafe
                    {
                        byte* p = (byte*)pixels.Scan0;
                        int i = 0;
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            for (int x = 0; x < bmp.Width; x++, i++)
                            {
                                int desti = i * 4;
                                int srci = y * pixels.Stride + x * 3;
                                t.Pixels[desti] = p[srci + 2];
                                t.Pixels[desti + 1] = p[srci + 1];
                                t.Pixels[desti + 2] = p[srci + 0];
                                t.Pixels[desti + 3] = 255;
                            }
                        }
                    }
                }
                finally
                {
                    bmp.UnlockBits(pixels);
                }
            }
            return t;
        }

        public override RawTexture Get(string name)
        {
            
            using (var stream = Store.GetStream(name))
            {
                if (stream == null) return null;

                if (!stream.CanSeek)
                {
                    // We need to be able to seek to do format detection
                    var memStream = new MemoryStream();
                    stream.CopyTo(memStream);
                    if (PngReader.IsPngImage(memStream))
                        return loadPng(memStream);
                    return loadOther(memStream);
                }
                if (PngReader.IsPngImage(stream))
                    return loadPng(stream);
                return loadOther(stream);
            }
        }
    }
}