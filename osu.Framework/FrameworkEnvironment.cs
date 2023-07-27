// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework
{
    public static class FrameworkEnvironment
    {
        public static ExecutionMode? StartupExecutionMode { get; }
        public static bool NoTestTimeout { get; }
        public static bool ForceTestGC { get; }
        public static bool FrameStatisticsViaTouch { get; }
        public static GraphicsSurfaceType? PreferredGraphicsSurface { get; }
        public static string? PreferredGraphicsRenderer { get; }
        public static int? StagingBufferType { get; }
        public static int? VertexBufferCount { get; }

        static FrameworkEnvironment()
        {
            StartupExecutionMode = Enum.TryParse<ExecutionMode>(Environment.GetEnvironmentVariable("OSU_EXECUTION_MODE"), true, out var mode) ? mode : null;
            NoTestTimeout = Environment.GetEnvironmentVariable("OSU_TESTS_NO_TIMEOUT") == "1";
            ForceTestGC = Environment.GetEnvironmentVariable("OSU_TESTS_FORCED_GC") == "1";
            FrameStatisticsViaTouch = Environment.GetEnvironmentVariable("OSU_FRAME_STATISTICS_VIA_TOUCH") == "1";
            PreferredGraphicsSurface = Enum.TryParse<GraphicsSurfaceType>(Environment.GetEnvironmentVariable("OSU_GRAPHICS_SURFACE"), true, out var surface) ? surface : null;
            PreferredGraphicsRenderer = Environment.GetEnvironmentVariable("OSU_GRAPHICS_RENDERER")?.ToLowerInvariant();

            if (int.TryParse(Environment.GetEnvironmentVariable("OSU_GRAPHICS_VBO_COUNT"), out int count))
                VertexBufferCount = count;

            if (int.TryParse(Environment.GetEnvironmentVariable("OSU_GRAPHICS_STAGING_BUFFER_TYPE"), out int stagingBufferImplementation))
                StagingBufferType = stagingBufferImplementation;
        }
    }
}
