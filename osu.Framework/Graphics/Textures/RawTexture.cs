// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using SixLabors.ImageSharp;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Graphics.Textures
{
    public class RawTexture
    {
        public int Width, Height;
        public PixelFormat PixelFormat;
        public byte[] Pixels;

        public static RawTexture FromStream(Stream stream)
        {
            using (var img = Image.Load(stream))
                return new RawTexture
                {
                    Width = img.Width,
                    Height = img.Height,
                    Pixels = img.SavePixelData(),
                    PixelFormat = PixelFormat.Rgba
                };
        }
    }
}
