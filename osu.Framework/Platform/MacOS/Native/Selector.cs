// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
