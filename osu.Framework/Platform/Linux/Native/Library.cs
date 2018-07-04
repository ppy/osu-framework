// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Linux.Native
{
    public static class Library
    {
        [DllImport("libdl.so", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen(string library, LoadFlags flags);

        /// <summary>
        /// Loads a library with an enum of flags to use with dlopen. Uses <see cref="LoadFlags"/> for the flags
        /// </summary>
        /// <param name="flags">See 'man dlopen' for more information.</param>
        /// <param name="locations">Uses ld.so and NATIVE_DLL_SEARCH_DIRECTORIES for library locations</param>
        public static void Load(string library, LoadFlags flags)
        {
            var loadStatus = IntPtr.Zero;
            loadStatus = dlopen(library, flags);

            if (loadStatus == IntPtr.Zero) // True if library failed to load
            {
                string fullPath;
                // List of paths that coreclr uses to find libraries
                var paths = (string)AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES");
                var pathList = paths.Split(':');
                for(var i = 0; i < pathList.Length && loadStatus == IntPtr.Zero; i++)
                {
                    fullPath = pathList[i]+'/'+library;
                    loadStatus = dlopen(fullPath, flags);
                }
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
