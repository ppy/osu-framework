// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace osu.Framework.Platform.Apple.Native.Accelerate
{
    internal static unsafe partial class vImage
    {
        [LibraryImport(Interop.LIB_ACCELERATE, EntryPoint = "vImageBuffer_Init")]
        internal static partial vImage_Error Init(vImage_Buffer* buf, uint height, uint width, uint pixelBits, vImage_Flags flags);

        [LibraryImport(Interop.LIB_ACCELERATE, EntryPoint = "vImageBuffer_InitWithCGImage")]
        internal static partial vImage_Error InitWithCGImage(vImage_Buffer* buf, vImage_CGImageFormat* format, double* backgroundColour, IntPtr image, vImage_Flags flags);
    }
}
