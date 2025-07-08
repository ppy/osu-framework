// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using osu.Framework.Timing;

namespace osu.Framework.Development
{
    public static class DebugUtils
    {
        /// <summary>
        /// This represents a clock that runs at a faster-than-realtime pace during unit testing,
        /// and is intended to substitute for a <see cref="StopwatchClock"/> that would normally
        /// be used to track a realtime reference.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>
        ///     Components such as <see cref="DecouplingFramedClock"/> use this to advance time at a faster
        ///     pace in order to not induce artificial delays from having to way on wall clock time to elapse.
        ///   </item>
        ///   <item>
        ///     Note that this property is only populated after the <see cref="GameHost"/> is run.
        ///   </item>
        /// </list>
        /// </remarks>
        internal static IClock? RealtimeClock { get; set; }

        public static bool IsNUnitRunning => is_nunit_running.Value;

        private static readonly Lazy<bool> is_nunit_running = new Lazy<bool>(() =>
            {
#pragma warning disable RS0030
                var entry = Assembly.GetEntryAssembly();
#pragma warning restore RS0030

                string? assemblyName = entry?.GetName().Name;

                // when running under nunit + netcore, entry assembly becomes nunit itself (testhost, Version=15.0.0.0), which isn't what we want.
                // when running under nunit + Rider > 2020.2 EAP6, entry assembly becomes ReSharperTestRunner[32|64], which isn't what we want.
                bool entryIsKnownTestAssembly = entry != null && (assemblyName!.Contains("testhost") || assemblyName.Contains("ReSharperTestRunner"));

                // null assembly can indicate nunit, but it can also indicate native code (e.g. android).
                // to distinguish nunit runs from android launches, check the class name of the current test.
                // if no actual test is running, nunit will make up an ad-hoc test context, which we can match on
                // to eliminate such false positives.
                bool nullEntryWithActualTestContext = entry == null && TestContext.CurrentContext.Test.ClassName != typeof(TestExecutionContext.AdhocContext).FullName;

                return entryIsKnownTestAssembly || nullEntryWithActualTestContext;
            }
        );

        internal static Assembly NUnitTestAssembly => nunit_test_assembly.Value;

        private static readonly Lazy<Assembly> nunit_test_assembly = new Lazy<Assembly>(() =>
            {
                Debug.Assert(IsNUnitRunning);

                string testName = TestContext.CurrentContext.Test.ClassName.AsNonNull();
                return AppDomain.CurrentDomain.GetAssemblies().First(asm => asm.GetType(testName) != null);
            }
        );

        public static bool IsDebugBuild => is_debug_build.Value;

        private static readonly Lazy<bool> is_debug_build = new Lazy<bool>(() =>
            isDebugAssembly(typeof(DebugUtils).Assembly) || isDebugAssembly(RuntimeInfo.EntryAssembly)
        );

        /// <summary>
        /// Whether the framework is currently logging performance issues.
        /// This should be used only when a configuration is not available via DI or otherwise (ie. in a static context).
        /// </summary>
        public static bool LogPerformanceIssues { get; internal set; }

        // https://stackoverflow.com/a/2186634
        private static bool isDebugAssembly(Assembly? assembly) => assembly?.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled) ?? false;
    }
}
