// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework
{
    public static class RuntimeInfo
    {
        [DllImport(@"kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport(@"kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public static bool Is32Bit { get; }
        public static bool Is64Bit { get; }
        public static bool IsMono { get; }
        public static bool IsWindows { get; }
        public static bool IsUnix { get; }
        public static bool IsLinux { get; }
        public static bool IsMacOsx { get; }
        public static bool IsWine { get; }

        static RuntimeInfo()
        {
            IsMono = Type.GetType("Mono.Runtime") != null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                IsWindows = true;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                IsMacOsx = true;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                IsLinux = true;
            IsUnix = IsMacOsx || IsUnix;

            Is32Bit = IntPtr.Size == 4;
            Is64Bit = IntPtr.Size == 8;

            if (IsWindows)
            {
                IntPtr hModule = GetModuleHandle(@"ntdll.dll");
                if (hModule == IntPtr.Zero)
                    IsWine = false;
                else
                {
                    IntPtr fptr = GetProcAddress(hModule, @"wine_get_version");
                    IsWine = fptr != IntPtr.Zero;
                }
            }
        }
    }
}
