// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.CompilerServices;
using osu.Framework.Allocation;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureRgba32 : RawTexture
    {
        /// <summary>
        /// The texture data.
        /// </summary>
        public readonly Rgba32[] Data;

        /// <summary>
        /// Create an empty raw texture with an optional <see cref="BufferStack{T}"/>. backing.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="data">The raw texture data.</param>
        public RawTextureRgba32(int width, int height, Rgba32[] data = null)
        {
            if (data == null)
                data = new Rgba32[width * height];

            if (width * height != data.Length)
                throw new InvalidOperationException("Provided data does not match dimensions");

            Data = data;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Create a raw texture with arbitrary raw data.
        /// Note that this method requires a memory copy operation to convert from <see cref="Byte"/> to <see cref="Rgba32"/>.
        /// Where possible, use <see cref="RawTextureRgba32"/> instead.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="data">The raw texture data.</param>
        internal RawTextureRgba32(int width, int height, byte[] data)
            : this(width, height, Unsafe.As<Rgba32[]>(data))
        {
        }

        public override ReadOnlySpan<Rgba32> GetImageData() => new Span<Rgba32>(Data);
    }
}
