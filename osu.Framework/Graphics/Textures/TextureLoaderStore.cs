// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

        protected virtual Image<TPixel> ImageFromStream<TPixel>(Stream stream) where TPixel : struct, IPixel<TPixel>
            => Image.Load<TPixel>(stream);

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
