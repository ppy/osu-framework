// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE
using System;
using System.Runtime.InteropServices;
using osu.Framework.Logging;

namespace osu.Framework.Platform.Linux.Native
{
    public static class Library
    {
        [Flags]
        public enum Flags
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
        [DllImport("libdl.so", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen(string library, int flags);

        /// <summary>
        /// Loads a library with an enum of flags to use with dlopen. Uses <see cref="Flags"/> for flags
        /// <para/>See 'man dlopen' for more information about the flags.
        /// <para/>See 'man ld.so' for more information about how the libraries are loaded.
        /// </summary>
        public static void LoadLibrary(string library, Flags flags)
        {
            int used_flag = 0x00000;
            // Combine flags that were used with LoadLibrary into one integer.
            foreach (var flag in (Flags[]) Enum.GetValues(typeof(Flags)))
            {
                if(flags.HasFlag(flag))
                    used_flag+=(int)flag;
            }
            dlopen(library, used_flag);
            try
            {
                var BASS_Version = ManagedBass.Bass.Version;
            }
            catch (Exception e)
            {
                Logger.Log("Failed to load libbass, trace: \n" + e, LoggingTarget.Runtime, LogLevel.Important);
            }
            try
            {
                var BASS_FX_Version = ManagedBass.Fx.BassFx.Version;
            }
            catch (Exception e)
            {
                Logger.Log("Failed to load libbass_fx, trace:\n" + e, LoggingTarget.Runtime, LogLevel.Important);
            }
        }
    }
}