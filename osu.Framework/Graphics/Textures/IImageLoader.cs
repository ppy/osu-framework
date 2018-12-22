// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// General interface that all image loaders should implement.
    /// </summary>
    public interface IImageLoader
    {
        Image<TPixel> FromStream<TPixel>(Stream stream) where TPixel : struct, IPixel<TPixel>;
    }
}
