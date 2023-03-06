// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework
{
    public static class FrameworkEnvironment
    {
        public static readonly ExecutionMode? STARTUP_EXECUTION_MODE;
        public static readonly bool NO_TEST_TIMEOUT;
        public static readonly bool FORCE_TEST_GC;
        public static readonly GraphicsSurfaceType? PREFERRED_GRAPHICS_SURFACE;
        public static readonly string? PREFERRED_GRAPHICS_RENDERER;

        static FrameworkEnvironment()
        {
            STARTUP_EXECUTION_MODE = Enum.TryParse<ExecutionMode>(Environment.GetEnvironmentVariable("OSU_EXECUTION_MODE"), true, out var mode) ? mode : null;
            NO_TEST_TIMEOUT = Environment.GetEnvironmentVariable("OSU_TESTS_NO_TIMEOUT") == "1";
            FORCE_TEST_GC = Environment.GetEnvironmentVariable("OSU_TESTS_FORCED_GC") == "1";
            PREFERRED_GRAPHICS_SURFACE = Enum.TryParse<GraphicsSurfaceType>(Environment.GetEnvironmentVariable("OSU_GRAPHICS_SURFACE"), true, out var surface) ? surface : null;
            PREFERRED_GRAPHICS_RENDERER = Environment.GetEnvironmentVariable("OSU_GRAPHICS_RENDERER")?.ToLowerInvariant();
        }
    }
}
