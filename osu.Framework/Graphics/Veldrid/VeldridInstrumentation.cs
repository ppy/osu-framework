// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Veldrid
{
    internal enum VeldridPipelineKind
    {
        Graphics,
        BufferUpdate,
        TextureUpload,
    }

    internal enum VeldridResourceSetKind
    {
        Texture,
        Uniform,
        ShaderStorage,
    }

    internal static class VeldridInstrumentation
    {
        public static bool Enabled => FrameworkEnvironment.LogVeldridFrameTimings;

        private static readonly object sync = new object();

        private static readonly GlobalStatistic<double> stat_graphics_submit_count = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Graphics submits/frame");
        private static readonly GlobalStatistic<double> stat_graphics_submit_ms = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Graphics submit ms/frame");
        private static readonly GlobalStatistic<double> stat_buffer_update_submit_count = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Buffer update submits/frame");
        private static readonly GlobalStatistic<double> stat_buffer_update_submit_ms = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Buffer update submit ms/frame");
        private static readonly GlobalStatistic<double> stat_texture_upload_submit_count = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Texture upload submits/frame");
        private static readonly GlobalStatistic<double> stat_texture_upload_submit_ms = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Texture upload submit ms/frame");
        private static readonly GlobalStatistic<double> stat_texture_upload_flush_count = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Texture upload flushes/frame");
        private static readonly GlobalStatistic<double> stat_pipeline_cache_hits = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Pipeline cache hits/frame");
        private static readonly GlobalStatistic<double> stat_pipeline_cache_misses = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Pipeline cache misses/frame");
        private static readonly GlobalStatistic<double> stat_pipeline_creates = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Pipelines created/frame");
        private static readonly GlobalStatistic<double> stat_texture_resource_set_creates = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Texture resource sets/frame");
        private static readonly GlobalStatistic<double> stat_uniform_resource_set_creates = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Uniform resource sets/frame");
        private static readonly GlobalStatistic<double> stat_shader_storage_resource_set_creates = GlobalStatistics.Get<double>(nameof(VeldridRenderer), "Shader storage resource sets/frame");

        private static readonly GlobalStatistic<long> stat_total_graphics_submits = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total graphics submits");
        private static readonly GlobalStatistic<long> stat_total_buffer_update_submits = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total buffer update submits");
        private static readonly GlobalStatistic<long> stat_total_texture_upload_submits = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total texture upload submits");
        private static readonly GlobalStatistic<long> stat_total_texture_upload_flushes = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total texture upload flushes");
        private static readonly GlobalStatistic<long> stat_total_pipeline_cache_hits = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total pipeline cache hits");
        private static readonly GlobalStatistic<long> stat_total_pipeline_cache_misses = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total pipeline cache misses");
        private static readonly GlobalStatistic<long> stat_total_pipeline_creates = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total pipelines created (windowed)");
        private static readonly GlobalStatistic<long> stat_total_texture_resource_sets = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total texture resource sets");
        private static readonly GlobalStatistic<long> stat_total_uniform_resource_sets = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total uniform resource sets");
        private static readonly GlobalStatistic<long> stat_total_shader_storage_resource_sets = GlobalStatistics.Get<long>(nameof(VeldridRenderer), "Total shader storage resource sets");

        private static int framesInWindow;
        private static long graphicsSubmitCountWindow;
        private static long graphicsSubmitTicksWindow;
        private static long bufferUpdateSubmitCountWindow;
        private static long bufferUpdateSubmitTicksWindow;
        private static long textureUploadSubmitCountWindow;
        private static long textureUploadSubmitTicksWindow;
        private static long textureUploadFlushCountWindow;
        private static long pipelineCacheHitWindow;
        private static long pipelineCacheMissWindow;
        private static long pipelineCreateWindow;
        private static long textureResourceSetCreateWindow;
        private static long uniformResourceSetCreateWindow;
        private static long shaderStorageResourceSetCreateWindow;

        public static void RecordSubmit(VeldridPipelineKind kind, long elapsedTicks)
        {
            if (!Enabled)
                return;

            lock (sync)
            {
                switch (kind)
                {
                    case VeldridPipelineKind.Graphics:
                        graphicsSubmitCountWindow++;
                        graphicsSubmitTicksWindow += elapsedTicks;
                        stat_total_graphics_submits.Value++;
                        break;

                    case VeldridPipelineKind.BufferUpdate:
                        bufferUpdateSubmitCountWindow++;
                        bufferUpdateSubmitTicksWindow += elapsedTicks;
                        stat_total_buffer_update_submits.Value++;
                        break;

                    case VeldridPipelineKind.TextureUpload:
                        textureUploadSubmitCountWindow++;
                        textureUploadSubmitTicksWindow += elapsedTicks;
                        stat_total_texture_upload_submits.Value++;
                        break;
                }
            }
        }

        public static void RecordTextureUploadFlush()
        {
            if (!Enabled)
                return;

            lock (sync)
            {
                textureUploadFlushCountWindow++;
                stat_total_texture_upload_flushes.Value++;
            }
        }

        public static void RecordPipelineCacheHit()
        {
            if (!Enabled)
                return;

            lock (sync)
            {
                pipelineCacheHitWindow++;
                stat_total_pipeline_cache_hits.Value++;
            }
        }

        public static void RecordPipelineCacheMiss()
        {
            if (!Enabled)
                return;

            lock (sync)
            {
                pipelineCacheMissWindow++;
                stat_total_pipeline_cache_misses.Value++;
            }
        }

        public static void RecordPipelineCreated()
        {
            if (!Enabled)
                return;

            lock (sync)
            {
                pipelineCreateWindow++;
                stat_total_pipeline_creates.Value++;
            }
        }

        public static void RecordResourceSetCreated(VeldridResourceSetKind kind)
        {
            if (!Enabled)
                return;

            lock (sync)
            {
                switch (kind)
                {
                    case VeldridResourceSetKind.Texture:
                        textureResourceSetCreateWindow++;
                        stat_total_texture_resource_sets.Value++;
                        break;

                    case VeldridResourceSetKind.Uniform:
                        uniformResourceSetCreateWindow++;
                        stat_total_uniform_resource_sets.Value++;
                        break;

                    case VeldridResourceSetKind.ShaderStorage:
                        shaderStorageResourceSetCreateWindow++;
                        stat_total_shader_storage_resource_sets.Value++;
                        break;
                }
            }
        }

        public static void EndFrame(GraphicsSurfaceType surfaceType)
        {
            if (!Enabled)
                return;

            lock (sync)
            {
                framesInWindow++;

                if (framesInWindow % 240 != 0)
                    return;

                double graphicsSubmitsPerFrame = graphicsSubmitCountWindow / (double)framesInWindow;
                double bufferUpdateSubmitsPerFrame = bufferUpdateSubmitCountWindow / (double)framesInWindow;
                double textureUploadSubmitsPerFrame = textureUploadSubmitCountWindow / (double)framesInWindow;
                double textureUploadFlushesPerFrame = textureUploadFlushCountWindow / (double)framesInWindow;
                double pipelineHitsPerFrame = pipelineCacheHitWindow / (double)framesInWindow;
                double pipelineMissesPerFrame = pipelineCacheMissWindow / (double)framesInWindow;
                double pipelineCreatesPerFrame = pipelineCreateWindow / (double)framesInWindow;
                double textureResourceSetsPerFrame = textureResourceSetCreateWindow / (double)framesInWindow;
                double uniformResourceSetsPerFrame = uniformResourceSetCreateWindow / (double)framesInWindow;
                double shaderStorageResourceSetsPerFrame = shaderStorageResourceSetCreateWindow / (double)framesInWindow;

                double graphicsSubmitMs = ticksToMilliseconds(graphicsSubmitTicksWindow / (double)Math.Max(1, graphicsSubmitCountWindow));
                double bufferUpdateSubmitMs = ticksToMilliseconds(bufferUpdateSubmitTicksWindow / (double)Math.Max(1, bufferUpdateSubmitCountWindow));
                double textureUploadSubmitMs = ticksToMilliseconds(textureUploadSubmitTicksWindow / (double)Math.Max(1, textureUploadSubmitCountWindow));

                stat_graphics_submit_count.Value = graphicsSubmitsPerFrame;
                stat_graphics_submit_ms.Value = graphicsSubmitMs;
                stat_buffer_update_submit_count.Value = bufferUpdateSubmitsPerFrame;
                stat_buffer_update_submit_ms.Value = bufferUpdateSubmitMs;
                stat_texture_upload_submit_count.Value = textureUploadSubmitsPerFrame;
                stat_texture_upload_submit_ms.Value = textureUploadSubmitMs;
                stat_texture_upload_flush_count.Value = textureUploadFlushesPerFrame;
                stat_pipeline_cache_hits.Value = pipelineHitsPerFrame;
                stat_pipeline_cache_misses.Value = pipelineMissesPerFrame;
                stat_pipeline_creates.Value = pipelineCreatesPerFrame;
                stat_texture_resource_set_creates.Value = textureResourceSetsPerFrame;
                stat_uniform_resource_set_creates.Value = uniformResourceSetsPerFrame;
                stat_shader_storage_resource_set_creates.Value = shaderStorageResourceSetsPerFrame;

                Logger.Log(
                    $"Veldrid workload summary ({surfaceType}): graphics_submit={graphicsSubmitsPerFrame:0.###}/f@{graphicsSubmitMs:0.###}ms, " +
                    $"buffer_submit={bufferUpdateSubmitsPerFrame:0.###}/f@{bufferUpdateSubmitMs:0.###}ms, " +
                    $"texture_submit={textureUploadSubmitsPerFrame:0.###}/f@{textureUploadSubmitMs:0.###}ms, " +
                    $"texture_flush={textureUploadFlushesPerFrame:0.###}/f, " +
                    $"pipeline_cache={pipelineHitsPerFrame:0.###}h/{pipelineMissesPerFrame:0.###}m/{pipelineCreatesPerFrame:0.###}c, " +
                    $"resource_sets tex={textureResourceSetsPerFrame:0.###}/f uni={uniformResourceSetsPerFrame:0.###}/f ssbo={shaderStorageResourceSetsPerFrame:0.###}/f",
                    level: LogLevel.Important);

                framesInWindow = 0;
                graphicsSubmitCountWindow = 0;
                graphicsSubmitTicksWindow = 0;
                bufferUpdateSubmitCountWindow = 0;
                bufferUpdateSubmitTicksWindow = 0;
                textureUploadSubmitCountWindow = 0;
                textureUploadSubmitTicksWindow = 0;
                textureUploadFlushCountWindow = 0;
                pipelineCacheHitWindow = 0;
                pipelineCacheMissWindow = 0;
                pipelineCreateWindow = 0;
                textureResourceSetCreateWindow = 0;
                uniformResourceSetCreateWindow = 0;
                shaderStorageResourceSetCreateWindow = 0;
            }
        }

        private static double ticksToMilliseconds(double ticks)
            => ticks * 1000 / Stopwatch.Frequency;
    }
}
