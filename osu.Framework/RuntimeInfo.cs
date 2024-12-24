// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using osu.Framework.Development;
using osu.Framework.Extensions.ObjectExtensions;

namespace osu.Framework
{
    public static class RuntimeInfo
    {
        /// <summary>
        /// The absolute path to the startup directory of this game.
        /// </summary>
        public static string StartupDirectory { get; } = AppContext.BaseDirectory;

        /// <summary>
        /// Returns the absolute path of osu.Framework.dll.
        /// </summary>
        public static string GetFrameworkAssemblyPath()
        {
            var assembly = Assembly.GetAssembly(typeof(RuntimeInfo));
            Debug.Assert(assembly != null);

            return assembly.Location;
        }

        /// <summary>
        /// Gets the entry assembly.
        /// When running under NUnit, the assembly of the current test will be returned instead.
        /// </summary>
        /// <returns>The entry assembly.</returns>
        public static Assembly EntryAssembly { get; internal set; } = DebugUtils.IsNUnitRunning
            ? DebugUtils.NUnitTestAssembly
#pragma warning disable RS0030
            : Assembly.GetEntryAssembly().AsNonNull();
#pragma warning restore RS0030

        public static Platform OS { get; }

        public static bool IsUnix => OS != Platform.Windows;
        public static bool IsDesktop => OS == Platform.Linux || OS == Platform.macOS || OS == Platform.Windows;
        public static bool IsMobile => OS == Platform.iOS || OS == Platform.Android;
        public static bool IsApple => OS == Platform.iOS || OS == Platform.macOS;

        static RuntimeInfo()
        {
            if (OperatingSystem.IsWindows())
                OS = Platform.Windows;
            if (OperatingSystem.IsIOS())
                OS = OS == 0 ? Platform.iOS : throw new InvalidOperationException($"Tried to set OS Platform to {nameof(Platform.iOS)}, but is already {Enum.GetName(OS)}");
            if (OperatingSystem.IsAndroid())
                OS = OS == 0 ? Platform.Android : throw new InvalidOperationException($"Tried to set OS Platform to {nameof(Platform.Android)}, but is already {Enum.GetName(OS)}");
            if (OperatingSystem.IsMacOS())
                OS = OS == 0 ? Platform.macOS : throw new InvalidOperationException($"Tried to set OS Platform to {nameof(Platform.macOS)}, but is already {Enum.GetName(OS)}");
            if (OperatingSystem.IsLinux())
                OS = OS == 0 ? Platform.Linux : throw new InvalidOperationException($"Tried to set OS Platform to {nameof(Platform.Linux)}, but is already {Enum.GetName(OS)}");

            if (OS == 0)
                throw new PlatformNotSupportedException("Operating system could not be detected correctly.");
        }

        // todo: revisit when we have a way to exclude enum members from naming rules
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum Platform
        {
            Windows = 1,
            Linux = 2,
            macOS = 3,
            iOS = 4,
            Android = 5
        }
    }
}
