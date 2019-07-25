// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class FrameBufferTextureCache
    {
        private static readonly List<TextureGLSingle> available_textures = new List<TextureGLSingle>();

        private static readonly GlobalStatistic<int> stat_total = GlobalStatistics.Get<int>("Native", $"{nameof(FrameBufferTextureCache)} total");
        private static readonly GlobalStatistic<int> stat_in_use = GlobalStatistics.Get<int>("Native", $"{nameof(FrameBufferTextureCache)} in use");

        private static readonly IComparer<TextureGLSingle> comparer = new ComparisonComparer<TextureGLSingle>(((x, y) => (x.Width * x.Height).CompareTo(y.Width * y.Height)));

        /// <summary>
        /// Retrieve a texture matching the specified criteria. A new texture will be allocated if no match is available.
        /// </summary>
        /// <param name="width">The width requested.</param>
        /// <param name="height">The height requested.</param>
        /// <param name="filteringMode">The filtering mode requested.</param>
        /// <returns>A texture matching the criteria.</returns>
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

        /// <summary>
        /// Return a previously retrieved texture for potential reuse.
        /// </summary>
        /// <param name="texture"></param>
        public static void Return(TextureGLSingle texture)
        {
            if (!(texture is FrameBufferTexture))
                throw new InvalidOperationException($"Returned texture type ({texture.GetType()}) is not a {nameof(FrameBufferTexture)}.");

            lock (available_textures)
            {
                if (available_textures.Contains(texture))
                    throw new InvalidOperationException("Returned texture was previously returned");

                int index = available_textures.BinarySearch(texture, comparer);

                if (index < 0)
                {
                    index = ~index;
                }

                available_textures.Insert(index, texture);
                stat_in_use.Value--;
            }
        }

        /// <summary>
        /// Purge any textures which are not currently in use.
        /// </summary>
        public static void Purge()
        {
            lock (available_textures)
            {
                foreach (var tex in available_textures)
                    tex.Dispose();
                available_textures.Clear();
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

            protected override void Dispose(bool isDisposing)
            {
                throw new InvalidOperationException($"{nameof(FrameBufferTexture)} should not be disposed");
            }
        }
    }
}
