// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public interface ITextureUpload : IDisposable
    {
        /// <summary>
        /// The raw data to be uploaded.
        /// </summary>
        ReadOnlySpan<Rgba32> Data { get; }

        /// <summary>
        /// The target mipmap level to upload into.
        /// </summary>
        int Level { get; }

        /// <summary>
        /// The target bounds for this upload. If not specified, will assume to be (0, 0, width, height).
        /// </summary>
        RectangleI Bounds { get; set; }

        /// <summary>
        /// The texture format for this upload.
        /// </summary>
        PixelFormat Format { get; }
    }
}
