// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using osu.Framework.Graphics.Veldrid.Pipelines;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Veldrid
{
    internal abstract class VeldridStagingResourcePool<T>
        where T : class, IDisposable
    {
        protected readonly GraphicsPipeline Pipeline;

        private readonly List<PooledUsage> available = new List<PooledUsage>();
        private readonly List<PooledUsage> used = new List<PooledUsage>();

        private readonly GlobalStatistic<ResourcePoolUsageStatistic> usageStat;

        private ulong currentExecutionIndex;

        protected VeldridStagingResourcePool(GraphicsPipeline pipeline, string name)
        {
            Pipeline = pipeline;

            usageStat = GlobalStatistics.Get<ResourcePoolUsageStatistic>(nameof(VeldridRenderer), $"{name} usage");
            usageStat.Value = new ResourcePoolUsageStatistic();

            pipeline.ExecutionStarted += executionStarted;
            pipeline.ExecutionFinished += executionFinished;
        }

        protected bool TryGet([NotNullWhen(true)] out T? resource)
            => TryGet<object>(static (_, _) => true, null, out resource);

        protected bool TryGet<TState>(Func<T, TState?, bool> match, TState? state, [NotNullWhen(true)] out T? resource)
        {
            // Reverse iteration is important to prefer reusing recently returned textures.
            // This avoids the case of a large pool being constantly cycled and therefore never
            // freed.
            for (int i = available.Count - 1; i >= 0; i--)
            {
                var existing = available[i];

                if (match(existing.Resource, state))
                {
                    existing.FrameUsageIndex = currentExecutionIndex;

                    available.Remove(existing);
                    used.Add(existing);

                    resource = existing.Resource;

                    updateStats();
                    return true;
                }
            }

            resource = null;
            return false;
        }

        protected void AddNewResource(T resource)
        {
            used.Add(new PooledUsage(resource, currentExecutionIndex));
            updateStats();
        }

        /// <summary>
        /// Updates the current execution index and frees any resources that have not been used for
        /// <see cref="Rendering.Renderer.RESOURCE_FREE_NO_USAGE_LENGTH"/> executions.
        /// </summary>
        /// <param name="executionIndex">The current execution index.</param>
        private void executionStarted(ulong executionIndex)
        {
            currentExecutionIndex = executionIndex;

            if (available.Count > 0)
            {
                var item = available[0];
                ulong framesSinceUsage = executionIndex - item.FrameUsageIndex;

                if (framesSinceUsage >= Rendering.Renderer.RESOURCE_FREE_NO_USAGE_LENGTH)
                {
                    item.Resource.Dispose();
                    available.Remove(item);
                }
            }

            updateStats();
        }

        /// <summary>
        /// Returns all resources that the GPU has finished using back to the pool.
        /// </summary>
        /// <param name="executionIndex">The finished execution index.</param>
        private void executionFinished(ulong executionIndex)
        {
            for (int i = 0; i < used.Count; i++)
            {
                var item = used[i];

                // Usages are sequential so we can stop checking after the usage exceeding the index.
                if (item.FrameUsageIndex > executionIndex)
                    break;

                if (item.FrameUsageIndex != executionIndex)
                    continue;

                available.Add(item);
                used.RemoveAt(i--);
            }

            updateStats();
        }

        private void updateStats()
        {
            usageStat.Value.CountAvailable = available.Count;
            usageStat.Value.CountInUse = used.Count;
        }

        private class PooledUsage
        {
            /// <summary>
            /// The tracked resource.
            /// </summary>
            public T Resource { get; }

            /// <summary>
            /// The draw frame at which the usage occurred.
            /// </summary>
            public ulong FrameUsageIndex { get; set; }

            public PooledUsage(T resource, ulong frameUsageIndex)
            {
                Resource = resource;
                FrameUsageIndex = frameUsageIndex;
            }
        }

        private class ResourcePoolUsageStatistic
        {
            /// <summary>
            /// Total number of drawables available for use (in the pool).
            /// </summary>
            public int CountAvailable;

            /// <summary>
            /// Total number of drawables currently in use.
            /// </summary>
            public int CountInUse;

            public override string ToString() => $"{CountInUse}/{CountAvailable + CountInUse}";
        }
    }
}
