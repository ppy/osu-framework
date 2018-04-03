// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

extern alias IOS;

using System.Diagnostics;
using System.IO;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;
using IOS::UIKit;
using IOS::CoreGraphics;
using IOS::Foundation;
using System.Runtime.InteropServices;
using System;

namespace osu.Framework.Graphics.Textures
{
    public class RawTexture
    {
        public int Width, Height;
        public PixelFormat PixelFormat;
        public byte[] Pixels;

        public unsafe static RawTexture FromUIImage(UIImage image)
        {
            if (image == null)
                return null;

            int width = (int)image.Size.Width;
            int height = (int)image.Size.Height;

            IntPtr data = Marshal.AllocHGlobal(width * height * 4);
            byte* bytes = (byte*)data;
            for (int i = width * height * 4 - 1; i >= 0; i--)
                bytes[i] = 0;

            using (CGBitmapContext textureContext = new CGBitmapContext(data, width, height, 8, width * 4, image.CGImage.ColorSpace, CGImageAlphaInfo.PremultipliedLast))
                textureContext.DrawImage(new IOS::System.Drawing.RectangleF(0, 0, width, height), image.CGImage);

            RawTexture t = new RawTexture
            {
                Width = width,
                Height = height,
                Pixels = new byte[width * height * 4],
                PixelFormat = PixelFormat.Rgba
            };

            unsafe
            {
                //convert from BGRA (System.Drawing) to RGBA
                //don't need to consider stride because we're in a raw format

                fixed (byte* pixels = t.Pixels)
                {
                    var dest = pixels;

                    int length = t.Pixels.Length / 4;
                    for (int i = 0; i < length; i++)
                    {
                        //BGRA -> RGBA
                        // ReSharper disable once PossibleNullReferenceException
                        dest[0] = bytes[2];
                        dest[1] = bytes[1];
                        dest[2] = bytes[0];
                        dest[3] = bytes[3];

                        bytes += 4;
                        dest += 4;
                    }
                }
            }

            Marshal.FreeHGlobal(data);

            return t;
        }

        public static RawTexture FromStream(Stream stream)
        {
            return FromUIImage(UIImage.LoadFromData(NSData.FromStream(stream)));
        }
    }
}
