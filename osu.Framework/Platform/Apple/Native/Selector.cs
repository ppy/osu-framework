// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native
{
    internal static partial class Selector
    {
        [LibraryImport(Interop.LIB_OBJ_C, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr Get(string name);
    }
}
