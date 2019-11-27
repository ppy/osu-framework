// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StbiSharp;

namespace osu.Framework.Graphics.Textures
{
    public class TextureLoaderStore : IResourceStore<TextureUpload>
    {
        private IResourceStore<byte[]> store { get; }

        public TextureLoaderStore(IResourceStore<byte[]> store)
        {
            this.store = store;
            (store as ResourceStore<byte[]>)?.AddExtension(@"png");
            (store as ResourceStore<byte[]>)?.AddExtension(@"jpg");
        }

        public Task<TextureUpload> GetAsync(string name) => Task.Run(() => Get(name));

        public TextureUpload Get(string name)
        {
            try
            {
                using (var stream = store.GetStream(name))
                {
                    if (stream != null)
                        return new TextureUpload(ImageFromStream<Rgba32>(stream));
                }
            }
            catch
            {
            }

            return null;
        }

        public Stream GetStream(string name) => store.GetStream(name);

        protected virtual Image<TPixel> ImageFromStream<TPixel>(Stream stream) where TPixel : unmanaged, IPixel<TPixel>
        {
            long initialPos = stream.Position;

            try
            {
                using (var m = new MemoryStream())
                {
                    stream.CopyTo(m);
                    using (var stbiImage = Stbi.LoadFromMemory(m, 4))
                        return Image.LoadPixelData(MemoryMarshal.Cast<byte, TPixel>(stbiImage.Data), stbiImage.Width, stbiImage.Height);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Texture could not be loaded via STB; falling back to ImageSharp.");
                stream.Position = initialPos;
                return Image.Load<TPixel>(stream);
            }
        }

        public IEnumerable<string> GetAvailableResources() => store.GetAvailableResources();

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                store.Dispose();
            }
        }

        ~TextureLoaderStore()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
