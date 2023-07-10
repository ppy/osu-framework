// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridStagingTexturePool
    {
        private readonly VeldridRenderer renderer;

        private readonly List<StagingTextureCache> available = new List<StagingTextureCache>();
        private readonly List<StagingTextureCache> used = new List<StagingTextureCache>();

        private static readonly GlobalStatistic<int> stat_available = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Total staging textures available");
        private static readonly GlobalStatistic<int> stat_used = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Total staging textures used");

        public VeldridStagingTexturePool(VeldridRenderer renderer)
        {
            this.renderer = renderer;
        }

        /// <summary>
        /// Retrieves a staging texture to use as an intermediate storage for uploading textures to the GPU.
        /// This should be written once by the CPU as it is handed over to the GPU for copying its data to the target texture,
        /// once the GPU has finished copying, the staging texture will eventually return back to the pool for reuse.
        /// </summary>
        /// <param name="width">The minimum width of the texture required for uploading.</param>
        /// <param name="height">The minimum height of the texture required for uploading.</param>
        /// <param name="format">The pixel format of the texture.</param>
        public Texture Get(int width, int height, PixelFormat format)
        {
            foreach (var existing in available)
            {
                if (existing.Texture.Format == format && existing.Texture.Width >= width && existing.Texture.Height >= height)
                {
                    available.Remove(existing);
                    stat_available.Value--;

                    used.Add(existing with { FrameUsageIndex = renderer.FrameIndex });
                    stat_used.Value++;

                    return existing.Texture;
                }
            }

            var texture = renderer.Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, format, TextureUsage.Staging));
            used.Add(new StagingTextureCache(texture, renderer.FrameIndex));
            stat_used.Value++;
            return texture;
        }

        /// <summary>
        /// Updates the state of the resources in this pool in two steps:
        /// <list type="bullet">
        /// <item>Returns all textures that the GPU has finished from back to the pool.</item>
        /// <item>Frees any texture that has not been used for a while, specifically after <see cref="Renderer.RESOURCE_FREE_CHECK_INTERVAL"/> number of frames.</item>
        /// </list>
        /// </summary>
        public void NewFrame()
        {
            // return any resource that the GPU has finished from.
            for (int i = 0; i < used.Count; i++)
            {
                var texture = used[i];

                if (texture.FrameUsageIndex <= renderer.LatestCompletedFrameIndex)
                {
                    available.Add(texture);
                    stat_available.Value++;

                    used.RemoveAt(i--);
                    stat_used.Value--;
                }
            }

            // dispose of any resource that we haven't used for a while.
            if (renderer.FrameIndex % Renderer.RESOURCE_FREE_CHECK_INTERVAL == 0)
            {
                for (int i = 0; i < available.Count; i++)
                {
                    var texture = available[i];

                    if (renderer.FrameIndex - texture.FrameUsageIndex < Renderer.RESOURCE_FREE_CHECK_INTERVAL)
                        break;

                    texture.Texture.Dispose();
                    available.Remove(texture);
                    stat_available.Value--;
                }
            }
        }

        private record struct StagingTextureCache(Texture Texture, ulong FrameUsageIndex);
    }
}
