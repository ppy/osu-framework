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

        private readonly GlobalStatistic<int> statAvailable;
        private readonly GlobalStatistic<int> statUsed;

        protected VeldridStagingResourcePool(VeldridRenderer renderer, string name)
        {
            Renderer = renderer;

            statAvailable = GlobalStatistics.Get<int>(nameof(VeldridRenderer), $"Total {name} available");
            statUsed = GlobalStatistics.Get<int>(nameof(VeldridRenderer), $"Total {name} used");
        }

        protected bool TryGet(Predicate<T> match, [NotNullWhen(true)] out T? resource)
        {
            foreach (var existing in available)
            {
                if (match(existing.Resource))
                {
                    available.Remove(existing);
                    statAvailable.Value--;

                    used.Add(existing with { FrameUsageIndex = Renderer.FrameIndex });
                    statUsed.Value++;

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
            statUsed.Value++;
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
                    statAvailable.Value++;

                    used.RemoveAt(i--);
                    statUsed.Value--;
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
                    statAvailable.Value--;
                }
            }
        }

        private record struct StagingResourceCache(T Resource, ulong FrameUsageIndex);
    }
}
