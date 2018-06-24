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

            const int rtld_lazy = 0x00001;
            const int rtld_now = 0x00002;
            const int rtld_binding_mask = 0x00003;
            const int rtld_noload = 0x00004;
            const int rtld_deepbind = 0x00008;
            const int rtld_global = 0x00100;
            const int rtld_local = 0x00000;
            const int rtld_nodelete = 0x01000;

            if (flags.ToUpper().Contains("RTLD_LAZY"))
                flag+=rtld_lazy;
            else if (flags.ToUpper().Contains("RTLD_NOW"))
                flag+=rtld_now;

            if (flags.ToUpper().Contains("RTLD_BINDING_MASK"))
                flag+=rtld_binding_mask;
            if (flags.ToUpper().Contains("RTLD_NOLOAD"))
                flag+=rtld_noload;
            if (flags.ToUpper().Contains("RTLD_DEEPBIND"))
                flag+=rtld_deepbind;
            if (flags.ToUpper().Contains("RTLD_GLOBAL"))
                flag+=rtld_global;
            if (flags.ToUpper().Contains("RTLD_LOCAL"))
                flag+=rtld_local;
            if (flags.ToUpper().Contains("RTLD_NODELETE"))
                flag+=rtld_nodelete;

            dlopen(library, flag);
        }
    }
}