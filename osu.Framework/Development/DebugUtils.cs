// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
            // https://stackoverflow.com/a/2186634
            (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled)
        );
    }
}
