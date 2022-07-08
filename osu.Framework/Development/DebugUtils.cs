// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace osu.Framework.Development
{
    public static class DebugUtils
    {
        public static bool IsNUnitRunning => is_nunit_running.Value;

        private static readonly Lazy<bool> is_nunit_running = new Lazy<bool>(() =>
            {
                var entry = Assembly.GetEntryAssembly();

                // when running under nunit + netcore, entry assembly becomes nunit itself (testhost, Version=15.0.0.0), which isn't what we want.
                // when running under nunit + Rider > 2020.2 EAP6, entry assembly becomes ReSharperTestRunner[32|64], which isn't what we want.
                bool entryIsKnownTestAssembly = entry != null && (entry.Location.Contains("testhost") || entry.Location.Contains("ReSharperTestRunner"));

                // null assembly can indicate nunit, but it can also indicate native code (e.g. android).
                // to distinguish nunit runs from android launches, check the class name of the current test.
                // if no actual test is running, nunit will make up an ad-hoc test context, which we can match on
                // to eliminate such false positives.
                bool nullEntryWithActualTestContext = entry == null && TestContext.CurrentContext.Test.ClassName != typeof(TestExecutionContext.AdhocContext).FullName;

                return entryIsKnownTestAssembly || nullEntryWithActualTestContext;
            }
        );

        private static readonly Lazy<Assembly> nunit_test_assembly = new Lazy<Assembly>(() =>
            {
                Debug.Assert(IsNUnitRunning);

                string testName = TestContext.CurrentContext.Test.ClassName;
                return AppDomain.CurrentDomain.GetAssemblies().First(asm => asm.GetType(testName) != null);
            }
        );

        public static bool IsDebugBuild => is_debug_build.Value;

        private static readonly Lazy<bool> is_debug_build = new Lazy<bool>(() =>
            isDebugAssembly(typeof(DebugUtils).Assembly) || isDebugAssembly(GetEntryAssembly())
        );

        /// <summary>
        /// Whether the framework is currently logging performance issues.
        /// This should be used only when a configuration is not available via DI or otherwise (ie. in a static context).
        /// </summary>
        public static bool LogPerformanceIssues { get; internal set; }

        // https://stackoverflow.com/a/2186634
        private static bool isDebugAssembly(Assembly assembly) => assembly?.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled) ?? false;

        /// <summary>
        /// Gets the entry assembly, or calling assembly otherwise.
        /// When running under NUnit, the assembly of the current test will be returned instead.
        /// </summary>
        /// <returns>The entry assembly (usually obtained via <see cref="Assembly.GetEntryAssembly()"/>.</returns>
        public static Assembly GetEntryAssembly()
        {
            if (IsNUnitRunning)
                return nunit_test_assembly.Value;

            return Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        }

        /// <summary>
        /// Gets the absolute path to the directory containing the assembly determined by <see cref="GetEntryAssembly"/>.
        /// </summary>
        /// <returns>The entry path (usually obtained via the entry assembly's <see cref="Assembly.Location"/> directory.</returns>
        [Obsolete("Use AppContext.BaseDirectory instead")] // Can be removed 20220211
        public static string GetEntryPath() => AppContext.BaseDirectory;
    }
}
