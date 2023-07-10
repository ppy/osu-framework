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

        private readonly List<StagingResourceCache> available = new List<StagingResourceCache>();
        private readonly List<StagingResourceCache> used = new List<StagingResourceCache>();

        private readonly GlobalStatistic<ResourcePoolUsageStatistic> usageStat;

        protected VeldridStagingResourcePool(VeldridRenderer renderer, string name)
        {
            Renderer = renderer;

            usageStat = GlobalStatistics.Get<ResourcePoolUsageStatistic>(nameof(VeldridRenderer), $"{name} usage");
            usageStat.Value = new ResourcePoolUsageStatistic();
        }

        protected bool TryGet(Predicate<T> match, [NotNullWhen(true)] out T? resource)
        {
            foreach (var existing in available)
            {
                if (match(existing.Resource))
                {
                    available.Remove(existing);
                    usageStat.Value.CurrentPoolSize--;

                    used.Add(existing with { FrameUsageIndex = Renderer.FrameIndex });
                    usageStat.Value.CountInUse++;

                    resource = existing.Resource;
                    return true;
                }
            }

            resource = null;
            return false;
        }

        protected void AddNewResource(T resource)
        {
            used.Add(new StagingResourceCache(resource, Renderer.FrameIndex));
            usageStat.Value.CountInUse++;
        }

        /// <summary>
        /// Updates the state of the resources in this pool in two steps:
        /// <list type="bullet">
        /// <item>Returns all textures that the GPU has finished from back to the pool.</item>
        /// <item>Frees any texture that has not been used for a while, specifically after <see cref="Rendering.Renderer.RESOURCE_FREE_CHECK_INTERVAL"/> number of frames.</item>
        /// </list>
        /// </summary>
        public void NewFrame()
        {
            // return any resource that the GPU has finished from.
            for (int i = 0; i < used.Count; i++)
            {
                var texture = used[i];

                if (texture.FrameUsageIndex <= Renderer.LatestCompletedFrameIndex)
                {
                    available.Add(texture);
                    usageStat.Value.CurrentPoolSize++;

                    used.RemoveAt(i--);
                    usageStat.Value.CountInUse--;
                }
            }

            // dispose of any resource that we haven't used for a while.
            if (Renderer.FrameIndex % Rendering.Renderer.RESOURCE_FREE_CHECK_INTERVAL == 0)
            {
                for (int i = 0; i < available.Count; i++)
                {
                    var texture = available[i];

                    if (Renderer.FrameIndex - texture.FrameUsageIndex < Rendering.Renderer.RESOURCE_FREE_CHECK_INTERVAL)
                        break;

                    texture.Resource.Dispose();
                    available.Remove(texture);
                    usageStat.Value.CurrentPoolSize--;
                }
            }
        }

        private record struct StagingResourceCache(T Resource, ulong FrameUsageIndex);

        private class ResourcePoolUsageStatistic
        {
            /// <summary>
            /// Total number of drawables available for use (in the pool).
            /// </summary>
            public int CurrentPoolSize;

            /// <summary>
            /// Total number of drawables currently in use.
            /// </summary>
            public int CountInUse;

            public override string ToString() => $"{CountInUse}/{CurrentPoolSize}";
        }
    }
}
