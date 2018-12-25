// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.iOS.Graphics.Textures
{
    public class IOSTextureLoaderStore : TextureLoaderStore
    {
        public IOSTextureLoaderStore(IResourceStore<byte[]> store)
            : base(store)
        {
        }

        public override TextureUpload Get(string name)
        {
            try
            {
                using (var stream = store.GetStream(name))
                {
                    if (stream != null)
                        return new TextureUpload(imageFromStream<Rgba32>(stream));
                }
            }
            catch
            {
            }

            return null;
        }

        private Image<TPixel> imageFromStream<TPixel>(Stream stream)
            where TPixel : struct, IPixel<TPixel>
        {
            var uiImage = UIImage.LoadFromData(NSData.FromStream(stream));
            int width = (int)uiImage.Size.Width;
            int height = (int)uiImage.Size.Height;
            IntPtr data = Marshal.AllocHGlobal(width * height * 4);

            using (CGBitmapContext textureContext = new CGBitmapContext(data, width, height, 8, width * 4, uiImage.CGImage.ColorSpace, CGImageAlphaInfo.PremultipliedLast))
                textureContext.DrawImage(new CGRect(0, 0, width, height), uiImage.CGImage);

            var pixels = new byte[width * height * 4];
            Marshal.Copy(data, pixels, 0, pixels.Length);
            Marshal.FreeHGlobal(data);

            // NOTE: this will probably only be correct for Rgba32, will need to look into other pixel formats
            return Image.LoadPixelData<TPixel>(pixels, width, height);
        }
    }
}
