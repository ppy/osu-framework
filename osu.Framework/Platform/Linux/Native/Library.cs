// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE
using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Linux.Native
{
    public static class Library
    {
        [DllImport("libdl.so", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen(string filename, int flags);
        public static void LoadLazyLocal(string filename)
        {
            dlopen(filename, 0x001); // RTLD_LOCAL + RTLD_LAZY
        }
        public static void LoadNowLocal(string filename)
        {
            dlopen(filename, 0x002); // RTLD_LOCAL + RTLD_NOW
        }
        public static void LoadLazyGlobal(string filename)
        {
            dlopen(filename, 0x101); // RTLD_GLOBAL + RTLD_LAZY
        }
        public static void LoadNowGlobal(string filename)
        {
            dlopen(filename, 0x102); // RTLD_GLOBAL + RTLD_NOW
        }
    }
}