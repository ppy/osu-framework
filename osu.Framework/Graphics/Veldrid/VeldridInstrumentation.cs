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

        private static readonly Lazy<GlobalStatistic<double>> stat_graphics_submit_count = createDoubleStatistic("Graphics submits/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_graphics_submit_ms = createDoubleStatistic("Graphics submit ms/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_buffer_update_submit_count = createDoubleStatistic("Buffer update submits/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_buffer_update_submit_ms = createDoubleStatistic("Buffer update submit ms/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_texture_upload_submit_count = createDoubleStatistic("Texture upload submits/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_texture_upload_submit_ms = createDoubleStatistic("Texture upload submit ms/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_texture_upload_flush_count = createDoubleStatistic("Texture upload flushes/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_pipeline_cache_hits = createDoubleStatistic("Pipeline cache hits/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_pipeline_cache_misses = createDoubleStatistic("Pipeline cache misses/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_pipeline_creates = createDoubleStatistic("Pipelines created/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_graphics_pipeline_binds = createDoubleStatistic("Graphics pipeline binds/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_graphics_pipeline_binds_skipped = createDoubleStatistic("Graphics pipeline binds skipped/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_graphics_resource_set_binds = createDoubleStatistic("Graphics resource set binds/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_graphics_resource_set_binds_skipped = createDoubleStatistic("Graphics resource set binds skipped/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_texture_resource_set_creates = createDoubleStatistic("Texture resource sets/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_uniform_resource_set_creates = createDoubleStatistic("Uniform resource sets/frame");
        private static readonly Lazy<GlobalStatistic<double>> stat_shader_storage_resource_set_creates = createDoubleStatistic("Shader storage resource sets/frame");

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
        private static long graphicsPipelineBindWindow;
        private static long graphicsPipelineBindSkippedWindow;
        private static long graphicsResourceSetBindWindow;
        private static long graphicsResourceSetBindSkippedWindow;
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
                        break;

                    case VeldridPipelineKind.BufferUpdate:
                        bufferUpdateSubmitCountWindow++;
                        bufferUpdateSubmitTicksWindow += elapsedTicks;
                        break;

                    case VeldridPipelineKind.TextureUpload:
                        textureUploadSubmitCountWindow++;
                        textureUploadSubmitTicksWindow += elapsedTicks;
                        break;
                }
            }
        }

        public static void RecordTextureUploadFlush()
        {
            if (!Enabled)
                return;

            lock (sync)
                textureUploadFlushCountWindow++;
        }

        public static void RecordPipelineCacheHit()
        {
            if (!Enabled)
                return;

            lock (sync)
                pipelineCacheHitWindow++;
        }

        public static void RecordPipelineCacheMiss()
        {
            if (!Enabled)
                return;

            lock (sync)
                pipelineCacheMissWindow++;
        }

        public static void RecordPipelineCreated()
        {
            if (!Enabled)
                return;

            lock (sync)
                pipelineCreateWindow++;
        }

        public static void RecordGraphicsPipelineBind(bool emitted)
        {
            if (!Enabled)
                return;

            lock (sync)
            {
                if (emitted)
                    graphicsPipelineBindWindow++;
                else
                    graphicsPipelineBindSkippedWindow++;
            }
        }

        public static void RecordGraphicsResourceSetBind(bool emitted)
        {
            if (!Enabled)
                return;

            lock (sync)
            {
                if (emitted)
                    graphicsResourceSetBindWindow++;
                else
                    graphicsResourceSetBindSkippedWindow++;
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
                        break;

                    case VeldridResourceSetKind.Uniform:
                        uniformResourceSetCreateWindow++;
                        break;

                    case VeldridResourceSetKind.ShaderStorage:
                        shaderStorageResourceSetCreateWindow++;
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
                double graphicsPipelineBindsPerFrame = graphicsPipelineBindWindow / (double)framesInWindow;
                double graphicsPipelineBindsSkippedPerFrame = graphicsPipelineBindSkippedWindow / (double)framesInWindow;
                double graphicsResourceSetBindsPerFrame = graphicsResourceSetBindWindow / (double)framesInWindow;
                double graphicsResourceSetBindsSkippedPerFrame = graphicsResourceSetBindSkippedWindow / (double)framesInWindow;
                double textureResourceSetsPerFrame = textureResourceSetCreateWindow / (double)framesInWindow;
                double uniformResourceSetsPerFrame = uniformResourceSetCreateWindow / (double)framesInWindow;
                double shaderStorageResourceSetsPerFrame = shaderStorageResourceSetCreateWindow / (double)framesInWindow;

                double graphicsSubmitMs = ticksToMilliseconds(graphicsSubmitTicksWindow / (double)Math.Max(1, graphicsSubmitCountWindow));
                double bufferUpdateSubmitMs = ticksToMilliseconds(bufferUpdateSubmitTicksWindow / (double)Math.Max(1, bufferUpdateSubmitCountWindow));
                double textureUploadSubmitMs = ticksToMilliseconds(textureUploadSubmitTicksWindow / (double)Math.Max(1, textureUploadSubmitCountWindow));

                stat_graphics_submit_count.Value.Value = graphicsSubmitsPerFrame;
                stat_graphics_submit_ms.Value.Value = graphicsSubmitMs;
                stat_buffer_update_submit_count.Value.Value = bufferUpdateSubmitsPerFrame;
                stat_buffer_update_submit_ms.Value.Value = bufferUpdateSubmitMs;
                stat_texture_upload_submit_count.Value.Value = textureUploadSubmitsPerFrame;
                stat_texture_upload_submit_ms.Value.Value = textureUploadSubmitMs;
                stat_texture_upload_flush_count.Value.Value = textureUploadFlushesPerFrame;
                stat_pipeline_cache_hits.Value.Value = pipelineHitsPerFrame;
                stat_pipeline_cache_misses.Value.Value = pipelineMissesPerFrame;
                stat_pipeline_creates.Value.Value = pipelineCreatesPerFrame;
                stat_graphics_pipeline_binds.Value.Value = graphicsPipelineBindsPerFrame;
                stat_graphics_pipeline_binds_skipped.Value.Value = graphicsPipelineBindsSkippedPerFrame;
                stat_graphics_resource_set_binds.Value.Value = graphicsResourceSetBindsPerFrame;
                stat_graphics_resource_set_binds_skipped.Value.Value = graphicsResourceSetBindsSkippedPerFrame;
                stat_texture_resource_set_creates.Value.Value = textureResourceSetsPerFrame;
                stat_uniform_resource_set_creates.Value.Value = uniformResourceSetsPerFrame;
                stat_shader_storage_resource_set_creates.Value.Value = shaderStorageResourceSetsPerFrame;

                Logger.Log(
                    $"Veldrid workload summary ({surfaceType}): graphics_submit={graphicsSubmitsPerFrame:0.###}/f@{graphicsSubmitMs:0.###}ms, " +
                    $"buffer_submit={bufferUpdateSubmitsPerFrame:0.###}/f@{bufferUpdateSubmitMs:0.###}ms, " +
                    $"texture_submit={textureUploadSubmitsPerFrame:0.###}/f@{textureUploadSubmitMs:0.###}ms, " +
                    $"texture_flush={textureUploadFlushesPerFrame:0.###}/f, " +
                    $"pipeline_cache={pipelineHitsPerFrame:0.###}h/{pipelineMissesPerFrame:0.###}m/{pipelineCreatesPerFrame:0.###}c, " +
                    $"binds pipeline={graphicsPipelineBindsPerFrame:0.###}/f({graphicsPipelineBindsSkippedPerFrame:0.###} skipped) resources={graphicsResourceSetBindsPerFrame:0.###}/f({graphicsResourceSetBindsSkippedPerFrame:0.###} skipped), " +
                    $"resource_sets tex={textureResourceSetsPerFrame:0.###}/f uni={uniformResourceSetsPerFrame:0.###}/f ssbo={shaderStorageResourceSetsPerFrame:0.###}/f",
                    level: LogLevel.Important);

                resetWindow();
            }
        }

        private static void resetWindow()
        {
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
            graphicsPipelineBindWindow = 0;
            graphicsPipelineBindSkippedWindow = 0;
            graphicsResourceSetBindWindow = 0;
            graphicsResourceSetBindSkippedWindow = 0;
            textureResourceSetCreateWindow = 0;
            uniformResourceSetCreateWindow = 0;
            shaderStorageResourceSetCreateWindow = 0;
        }

        private static Lazy<GlobalStatistic<double>> createDoubleStatistic(string name)
            => new Lazy<GlobalStatistic<double>>(() => GlobalStatistics.Get<double>(nameof(VeldridRenderer), name));

        private static double ticksToMilliseconds(double ticks)
            => ticks * 1000 / Stopwatch.Frequency;
    }
}
