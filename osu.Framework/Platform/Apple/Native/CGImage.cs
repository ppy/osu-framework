// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native
{
    internal readonly partial struct CGImage
    {
        internal IntPtr Handle { get; }

        public CGImage(IntPtr handle)
        {
            Handle = handle;
        }

        internal nuint Width => GetWidth(this);
        internal nuint Height => GetHeight(this);

        [LibraryImport(Interop.LIB_CORE_GRAPHICS, EntryPoint = "CGImageGetWidth")]
        internal static partial nuint GetWidth(CGImage image);

        [LibraryImport(Interop.LIB_CORE_GRAPHICS, EntryPoint = "CGImageGetHeight")]
        internal static partial nuint GetHeight(CGImage image);

        [LibraryImport(Interop.LIB_CORE_GRAPHICS, EntryPoint = "CGImageRelease")]
        internal static partial void Release(CGImage image);

        [LibraryImport(Interop.LIB_CORE_FOUNDATION, EntryPoint = "CFGetRetainCount")]
        internal static partial int GetRetainCount(CGImage image);
    }
}
