// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using SixLabors.ImageSharp.PixelFormats;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Texture data in a raw Rgba32 format.
    /// </summary>
    public abstract class RawTexture : IDisposable
    {
        /// <summary>
        /// The width of the texture data.
        /// </summary>
        public int Width;

        /// <summary>
        /// They height of the texture data.
        /// </summary>
        public int Height;

        /// <summary>
        /// The pixel format of the texture data.
        /// </summary>
        public readonly PixelFormat PixelFormat = PixelFormat.Rgba;

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
        }

        ~RawTexture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public abstract ReadOnlySpan<Rgba32> GetImageData();
    }
}
