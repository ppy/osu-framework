// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE
using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Linux.Native
{
    public static class Library
    {
        [DllImport("libdl.so", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen(string library, int flags);

        /// <summary>
        /// Load a library with specified flags as strings, the string only needs to contain the flags you want.
        /// <para/>See 'man dlopen' for more information about the flags.
        /// <para/>See 'man ld.so for more information about how the libraries are loaded.
        /// </summary>
        public static void LoadLibrary(string library, string flags)
        {
            int flag = 0x00000;

            const int RTLD_LAZY = 0x00001;
            const int RTLD_NOW = 0x00002;
            const int RTLD_BINDING_MASK = 0x00003;
            const int RTLD_NOLOAD = 0x00004;
            const int RTLD_DEEPBIND = 0x00008;
            const int RTLD_GLOBAL = 0x00100;
            const int RTLD_LOCAL = 0x00000;
            const int RTLD_NODELETE = 0x01000;

            if (flags.Contains("RTLD_LAZY"))
                flag+=RTLD_LAZY;
            else if (flags.Contains("RTLD_NOW"))
                flag+=RTLD_NOW;

            if (flags.Contains("RTLD_BINDING_MASK"))
                flag+=RTLD_BINDING_MASK;
            if (flags.Contains("RTLD_NOLOAD"))
                flag+=RTLD_NOLOAD;
            if (flags.Contains("RTLD_DEEPBIND"))
                flag+=RTLD_DEEPBIND;
            if (flags.Contains("RTLD_GLOBAL"))
                flag+=RTLD_GLOBAL;
            if (flags.Contains("RTLD_LOCAL"))
                flag+=RTLD_LOCAL;
            if (flags.Contains("RTLD_NODELETE"))
                flag+=RTLD_NODELETE;

            dlopen(library, flag);
        }
    }
}