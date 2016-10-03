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

        private RawTexture LoadPng(Stream stream)
        {
            RawTexture t = new RawTexture();
            var reader = new PngReader();
            t.Pixels = reader.Read(stream);
            t.PixelFormat = OpenTK.Graphics.ES20.PixelFormat.Rgba;
            t.Width = reader.Width;
            t.Height = reader.Height;
            return t;
        }

        private RawTexture LoadOther(Stream stream)
        {
            RawTexture t = new RawTexture();
            using (var bmp = (Bitmap)Image.FromStream(stream))
            {
                t.Pixels = new byte[bmp.Width * bmp.Height * 3];
                t.Width = bmp.Width;
                t.Height = bmp.Height;
                t.PixelFormat = OpenTK.Graphics.ES20.PixelFormat.Rgb;
                var pixels = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Marshal.Copy(pixels.Scan0, t.Pixels, 0, t.Pixels.Length);
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
                if (PngReader.IsPngImage(stream))
                    return LoadPng(stream);
                return LoadOther(stream);
            }
        }
    }
}