// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        /// <summary>
        /// Returns the absolute path of osu.Framework.dll.
        /// </summary>
        public static string GetFrameworkAssemblyPath() =>
            System.Reflection.Assembly.GetAssembly(typeof(RuntimeInfo)).Location;

        public static bool Is32Bit { get; }
        public static bool Is64Bit { get; }
        public static Platform OS { get; }
        public static bool IsUnix => OS == Platform.Linux || OS == Platform.MacOsx || OS == Platform.iOS;
        public static bool IsWine { get; }
        public static bool SupportsJIT => OS != Platform.iOS;

        static RuntimeInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                OS = Platform.Windows;
            if (osuTK.Configuration.RunningOnIOS)
                OS = OS == 0 ? Platform.iOS : throw new InvalidOperationException($"Tried to set OS Platform to {nameof(Platform.iOS)}, but is already {Enum.GetName(typeof(Platform), OS)}");
            if (osuTK.Configuration.RunningOnAndroid)
                OS = OS == 0 ? Platform.Android : throw new InvalidOperationException($"Tried to set OS Platform to {nameof(Platform.Android)}, but is already {Enum.GetName(typeof(Platform), OS)}");
            if (OS != Platform.iOS && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                OS = OS == 0 ? Platform.MacOsx : throw new InvalidOperationException($"Tried to set OS Platform to {nameof(Platform.MacOsx)}, but is already {Enum.GetName(typeof(Platform), OS)}");
            if (OS != Platform.Android && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                OS = OS == 0 ? Platform.Linux : throw new InvalidOperationException($"Tried to set OS Platform to {nameof(Platform.Linux)}, but is already {Enum.GetName(typeof(Platform), OS)}");

            if (OS == 0)
                throw new PlatformNotSupportedException("Operating system could not be detected correctly.");

            Is32Bit = IntPtr.Size == 4;
            Is64Bit = IntPtr.Size == 8;

            if (OS == Platform.Windows)
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

        public enum Platform
        {
            Windows = 1,
            Linux = 2,
            MacOsx = 3,
            iOS = 4,
            Android = 5
        }
    }
}
