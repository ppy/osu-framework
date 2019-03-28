// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Development
{
    public static class DebugUtils
    {
        public static bool IsDebugBuild => is_debug_build.Value;

        private static readonly Lazy<bool> is_debug_build = new Lazy<bool>(() =>
            isDebugAssembly(typeof(DebugUtils).Assembly) || isDebugAssembly(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
        );

        // https://stackoverflow.com/a/2186634
        private static bool isDebugAssembly(Assembly assembly) => assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
    }
}
