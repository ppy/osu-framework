// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using OpenTK.Graphics.ES30;
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
