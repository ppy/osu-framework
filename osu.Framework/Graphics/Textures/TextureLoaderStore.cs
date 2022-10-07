// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Handles the parsing of image data from standard image formats into <see cref="TextureUpload"/>s ready for GPU consumption.
    /// </summary>
    public class TextureLoaderStore : IResourceStore<TextureUpload>
    {
        private readonly ResourceStore<byte[]> store;

        public TextureLoaderStore(IResourceStore<byte[]> store)
        {
            this.store = new ResourceStore<byte[]>(store);
            this.store.AddExtension(@"png");
            this.store.AddExtension(@"jpg");
        }

        public Task<TextureUpload> GetAsync(string name, CancellationToken cancellationToken = default) =>
            Task.Run(() => Get(name), cancellationToken);

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
            => TextureUpload.LoadFromStream<TPixel>(stream);

        public IEnumerable<string> GetAvailableResources() => store.GetAvailableResources();

        #region IDisposable Support

        private bool isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            store.Dispose();

            isDisposed = true;
        }

        #endregion
    }
}
