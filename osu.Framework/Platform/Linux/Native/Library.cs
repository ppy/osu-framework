// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Linux.Native
{
    public static class Library
    {
        [DllImport("libdl.so.2", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen(string library, LoadFlags flags);

        /// <summary>
        /// Loads a library with flags to use with dlopen. Uses <see cref="LoadFlags"/> for the flags
        ///
        /// Uses NATIVE_DLL_SEARCH_DIRECTORIES and then ld.so for library paths
        /// </summary>
        /// <param name="library">Full name of the library</param>
        /// <param name="flags">See 'man dlopen' for more information.</param>
        public static void Load(string library, LoadFlags flags)
        {
            string paths = (string)AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES");
            Debug.Assert(paths != null);

            foreach (string path in paths.Split(':'))
            {
                if (dlopen(Path.Combine(path, library), flags) != IntPtr.Zero)
                    break;
            }
        }

        [Flags]
        public enum LoadFlags
        {
            RTLD_LAZY = 0x00001,
            RTLD_NOW = 0x00002,
            RTLD_BINDING_MASK = 0x00003,
            RTLD_NOLOAD = 0x00004,
            RTLD_DEEPBIND = 0x00008,
            RTLD_GLOBAL = 0x00100,
            RTLD_LOCAL = 0x00000,
            RTLD_NODELETE = 0x01000
        }
    }
}
