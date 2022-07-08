// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class Selector
    {
        [DllImport(Cocoa.LIB_OBJ_C, EntryPoint = "sel_registerName")]
        public static extern IntPtr Get(string name);
    }
}
