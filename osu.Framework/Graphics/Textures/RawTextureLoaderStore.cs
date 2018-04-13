// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureLoaderStore : ResourceStore<RawTexture>
    {
        private IResourceStore<byte[]> store { get; }

        public RawTextureLoaderStore(IResourceStore<byte[]> store)
        {
            this.store = store;
            (store as ResourceStore<byte[]>)?.AddExtension(@"png");
            (store as ResourceStore<byte[]>)?.AddExtension(@"jpg");
        }

        private RawTexture loadOther(Stream stream)
        {
            RawTexture t = new RawTexture();
            using (var bmp = (Bitmap)Image.FromStream(stream))
            {
                t.Pixels = new byte[bmp.Width * bmp.Height * 4];
                t.Width = bmp.Width;
                t.Height = bmp.Height;
                t.PixelFormat = OpenTK.Graphics.ES30.PixelFormat.Rgba;
                var pixels = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    unsafe
                    {
                        byte* p = (byte*)pixels.Scan0;

                        Debug.Assert(p != null);

                        int i = 0;
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            for (int x = 0; x < bmp.Width; x++, i++)
                            {
                                int desti = i * 4;
                                int srci = y * pixels.Stride + x * 3;
                                // ReSharper disable once PossibleNullReferenceException
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
            try
            {
                using (var stream = store.GetStream(name))
                {
                    if (stream == null) return null;

                    return RawTexture.FromStream(stream);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
