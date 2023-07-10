// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    internal abstract class VeldridStagingResourcePool<T>
        where T : class, DeviceResource, IDisposable
    {
        protected readonly VeldridRenderer Renderer;

        private readonly List<PooledUsage> available = new List<PooledUsage>();
        private readonly List<PooledUsage> used = new List<PooledUsage>();

        private readonly GlobalStatistic<ResourcePoolUsageStatistic> usageStat;

        protected VeldridStagingResourcePool(VeldridRenderer renderer, string name)
        {
            Renderer = renderer;

            usageStat = GlobalStatistics.Get<ResourcePoolUsageStatistic>(nameof(VeldridRenderer), $"{name} usage");
            usageStat.Value = new ResourcePoolUsageStatistic();
        }

        protected bool TryGet(Predicate<T> match, [NotNullWhen(true)] out T? resource)
        {
            // Reverse iteration is important to prefer reusing recently returned textures.
            // This avoids the case of a large pool being constantly cycled and therefore never
            // freed.
            for (int i = available.Count - 1; i >= 0; i--)
            {
                var existing = available[i];

                if (match(existing.Resource))
                {
                    existing.FrameUsageIndex = Renderer.FrameIndex;

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
            used.Add(new PooledUsage(resource, Renderer.FrameIndex));
            updateStats();
        }

        /// <summary>
        /// Updates the state of the resources in this pool in two steps:
        /// <list type="bullet">
        /// <item>Returns all textures that the GPU has finished using back to the pool.</item>
        /// <item>Frees any textures that have not been used for <see cref="Rendering.Renderer.RESOURCE_FREE_NO_USAGE_LENGTH"/> frames.</item>
        /// </list>
        /// </summary>
        public void NewFrame()
        {
            // return any resource that the GPU has finished using in the last frame.
            for (int i = 0; i < used.Count; i++)
            {
                var item = used[i];

                // Usages are sequential so we can stop checking after the first non-completed usage.
                if (item.FrameUsageIndex > Renderer.LatestCompletedFrameIndex)
                    break;

                available.Add(item);
                used.RemoveAt(i--);
            }

            // free any resource that hasn't been used for a while.
            for (int i = 0; i < available.Count; i++)
            {
                var item = available[i];

                ulong framesSinceUsage = Renderer.LatestCompletedFrameIndex - item.FrameUsageIndex;

                if (framesSinceUsage >= Rendering.Renderer.RESOURCE_FREE_NO_USAGE_LENGTH)
                {
                    item.Resource.Dispose();
                    available.Remove(item);
                    break;
                }
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
