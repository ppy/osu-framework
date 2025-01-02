// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native
{
    internal readonly partial struct CGBitmapContext
    {
        internal IntPtr Handle { get; }

        internal CGBitmapContext(IntPtr handle)
        {
            Handle = handle;
        }

        [LibraryImport(Interop.LIB_CORE_GRAPHICS, EntryPoint = "CGBitmapContextCreate")]
        public static partial CGBitmapContext Create(IntPtr data, nuint width, nuint height, nuint bitsPerComponent, nuint bytesPerRow, CGColorSpace colorSpace, CGBitmapInfo bitmapInfo);

        [LibraryImport(Interop.LIB_CORE_GRAPHICS, EntryPoint = "CGContextDrawImage")]
        public static partial void DrawImage(CGBitmapContext context, CGRect rect, CGImage image);
    }
}
