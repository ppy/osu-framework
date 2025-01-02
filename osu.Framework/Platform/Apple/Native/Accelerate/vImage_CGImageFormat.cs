// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable InconsistentNaming

using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native.Accelerate
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct vImage_CGImageFormat
    {
        public uint BitsPerComponent;
        public uint BitsPerPixel;
        public CGColorSpace ColorSpace;
        public CGBitmapInfo BitmapInfo;
        public uint Version;
        public double* Decode;
        public CGColorRenderingIntent RenderingIntent;
    }
}
