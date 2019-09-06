// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Development
{
    public static class DebugUtils
    {
        internal static Assembly HostAssembly { get; set; }

        public static bool IsNUnitRunning => is_nunit_running.Value;

        private static readonly Lazy<bool> is_nunit_running = new Lazy<bool>(() =>
            {
                var entry = Assembly.GetEntryAssembly();

                // when running under nunit + netcore, entry assembly becomes nunit itself (testhost, Version=15.0.0.0), which isn't what we want.
                return entry == null || entry.Location.Contains("testhost");
            }
        );

        public static bool IsDebugBuild => is_debug_build.Value;

        private static readonly Lazy<bool> is_debug_build = new Lazy<bool>(() =>
            isDebugAssembly(typeof(DebugUtils).Assembly) || isDebugAssembly(GetEntryAssembly())
        );

        /// <summary>
        /// Whether the framework is currently logging performance issues via <see cref="FrameworkSetting.PerformanceLogging"/>.
        /// This should be used only when a configuration is not available via DI or otherwise (ie. in a static context).
        /// </summary>
        public static bool LogPerformanceIssues { get; internal set; }

        // https://stackoverflow.com/a/2186634
        private static bool isDebugAssembly(Assembly assembly) => assembly?.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled) ?? false;

        /// <summary>
        /// Get the entry assembly, even when running under nUnit.
        /// Will fall back to calling assembly if there is no Entry assembly.
        /// </summary>
        /// <returns>The entry assembly (usually obtained via <see cref="Assembly.GetEntryAssembly()"/>.</returns>
        public static Assembly GetEntryAssembly()
        {
            if (IsNUnitRunning && HostAssembly != null)
                return HostAssembly;

            return Assembly.GetEntryAssembly() ?? HostAssembly ?? Assembly.GetCallingAssembly();
        }

        /// <summary>
        /// Get the entry path, even when running under nUnit.
        /// </summary>
        /// <returns>The entry assembly (usually obtained via the entry assembly's <see cref="Assembly.Location"/>.</returns>
        public static string GetEntryPath() =>
            IsNUnitRunning ? TestContext.CurrentContext.TestDirectory : Path.GetDirectoryName(GetEntryAssembly().Location);
    }
}
