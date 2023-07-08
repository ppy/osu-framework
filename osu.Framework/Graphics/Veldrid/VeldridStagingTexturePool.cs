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

        public void ReturnTextures(ulong latestFrame)
        {
            for (int i = 0; i < used.Count; i++)
            {
                var texture = used[i];

                if (texture.FrameUsageIndex <= latestFrame)
                {
                    available.Add(texture);
                    stat_available.Value++;

                    used.RemoveAt(i--);
                    stat_used.Value--;
                }
            }
        }

        public void CleanupUnusedTextures()
        {
            if (renderer.FrameIndex % Renderer.RESOURCE_FREE_CHECK_INTERVAL != 0)
                return;

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

        private record struct StagingTextureCache(Texture Texture, ulong FrameUsageIndex);
    }
}
