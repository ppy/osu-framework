// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace osu.Framework.Development
{
    public static class DebugUtils
    {
        public static bool IsDebugBuild => is_debug_build.Value;

        private static readonly Lazy<bool> is_debug_build = new Lazy<bool>(() =>
            isDebugAssembly(typeof(DebugUtils).Assembly) || isDebugAssembly(GetEntryAssembly())
        );

        // https://stackoverflow.com/a/2186634
        private static bool isDebugAssembly(Assembly assembly) => assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);

        /// <summary>
        /// Get the entry assembly, even when running under nUnit.
        /// </summary>
        /// <returns>The entry assembly (usually obtained via <see cref="Assembly.GetEntryAssembly()"/>.</returns>
        public static Assembly GetEntryAssembly()
        {
            var assembly = Assembly.GetEntryAssembly();

            // when running under nunit + netcore, entry assembly becomes nunit itself (testhost, Version=15.0.0.0), which isn't what we want.
            if (assembly == null || assembly.Location.Contains("testhost"))
                assembly = Assembly.GetExecutingAssembly();

            return assembly;
        }

        /// <summary>
        /// Get the entry path, even when running under nUnit.
        /// </summary>
        /// <returns>The entry assembly (usually obtained via the entry assembly's <see cref="Assembly.Location"/>.</returns>
        public static string GetEntryPath()
        {
            string assemblyPath;

            var assembly = Assembly.GetEntryAssembly();

            // when running under nunit + netcore, entry assembly becomes nunit itself (testhost, Version=15.0.0.0), which isn't what we want.
            if (assembly == null || assembly.Location.Contains("testhost"))
            {
                // From nuget, the executing assembly will also be wrong
                assemblyPath = TestContext.CurrentContext.TestDirectory;
            }
            else
                assemblyPath = Path.GetDirectoryName(assembly.Location);

            return assemblyPath;
        }
    }
}
