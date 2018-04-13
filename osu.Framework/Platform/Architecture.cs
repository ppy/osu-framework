// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform
{
    internal static class Architecture
    {
        public static string NativeIncludePath => $@"{Environment.CurrentDirectory}/{arch}/";
        private static string arch => Is64Bit ? @"x64" : @"x86";

        internal static bool Is64Bit => IntPtr.Size == 8;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        internal static void SetIncludePath()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
                SetDllDirectory(NativeIncludePath);
        }
    }
}
