// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public interface ITextureUpload : IDisposable
    {
        ReadOnlySpan<Rgba32> Data { get; }
        int Level { get; }
        RectangleI Bounds { get; set; }
        PixelFormat Format { get; }
    }
}
