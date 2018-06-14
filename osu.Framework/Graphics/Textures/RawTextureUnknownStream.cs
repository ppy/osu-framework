// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using System.IO;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Reads an image from an arbitrary data stream.
    /// </summary>
    public class RawTextureUnknownStream : RawTextureByteArray
    {
        public RawTextureUnknownStream(Stream stream)
        {
            using (var img = Image.Load(SixLabors.ImageSharp.Configuration.Default, stream))
            {
                Bytes = img.SavePixelData();
                Dimensions = new Rectangle(0, 0, img.Width, img.Height);
            }

            PixelFormat = PixelFormat.Rgba;
        }
    }
}
