// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Buffers;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public class ArrayPoolTextureUpload : ITextureUpload
    {
        private readonly ArrayPool<Rgba32> arrayPool;

        private readonly Rgba32[] data;

        /// <summary>
        /// Create an empty raw texture with an efficient shared memory backing.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="arrayPool">The source pool to retrieve memory from. Shared default is used if null.</param>
        public ArrayPoolTextureUpload(int width, int height, ArrayPool<Rgba32> arrayPool = null)
        {
            this.arrayPool = arrayPool ?? ArrayPool<Rgba32>.Shared;
            data = this.arrayPool.Rent(width * height);
        }

        public void Dispose()
        {
            arrayPool.Return(data);
        }

        public Span<Rgba32> RawData => data;

        public ReadOnlySpan<Rgba32> Data => data;

        public int Level { get; set; }

        public virtual PixelFormat Format => PixelFormat.Rgba;

        public RectangleI Bounds { get; set; }
    }
}
