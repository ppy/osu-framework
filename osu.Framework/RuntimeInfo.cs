// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using osu.Framework.Development;

namespace osu.Framework
{
    public static class RuntimeInfo
    {
        /// <summary>
        /// The absolute path to the startup directory of this game.
        /// </summary>
        public static string StartupDirectory { get; } = DebugUtils.GetEntryPath();

        /// <summary>
        /// Returns the absolute path of osu.Framework.dll.
        /// </summary>
        public static string GetFrameworkAssemblyPath()
        {
            var assembly = Assembly.GetAssembly(typeof(RuntimeInfo));
            Debug.Assert(assembly != null);

            return assembly.Location;
        }

        public static Platform OS { get; }
        public static bool IsUnix => OS != Platform.Windows;

        public static bool SupportsJIT => OS != Platform.iOS;
        public static bool IsDesktop => OS == Platform.Linux || OS == Platform.MacOsx || OS == Platform.Windows;
        public static bool IsMobile => OS == Platform.iOS || OS == Platform.Android;

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
