// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Textures;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class FrameBufferTextureCache
    {
        private static readonly List<TextureGLSingle> available_textures = new List<TextureGLSingle>();

        private static readonly GlobalStatistic<int> stat_total = GlobalStatistics.Get<int>("Native", $"{nameof(FrameBufferTextureCache)} total");
        private static readonly GlobalStatistic<int> stat_in_use = GlobalStatistics.Get<int>("Native", $"{nameof(FrameBufferTextureCache)} in use");

        public static TextureGLSingle Get(int width, int height, All filteringMode = All.Linear)
        {
            lock (available_textures)
            {
                var tex = available_textures.FirstOrDefault(t => t.Width >= width && t.Height >= height && t.FilteringMode == filteringMode) ?? available_textures.FirstOrDefault();

                stat_in_use.Value++;

                if (tex != null)
                {
                    available_textures.Remove(tex);
                    return tex;
                }

                stat_total.Value++;
                return new FrameBufferTexture(width, height, filteringMode);
            }
        }

        public static void Return(TextureGLSingle texture)
        {
            lock (available_textures)
            {
                available_textures.Add(texture);
                stat_in_use.Value--;
            }
        }

        private class FrameBufferTexture : TextureGLSingle
        {
            public FrameBufferTexture(int width, int height, All filteringMode = All.Linear)
                : base(width, height, true, filteringMode)
            {
                SetData(new TextureUpload());
                Upload();
            }
        }
    }
}
