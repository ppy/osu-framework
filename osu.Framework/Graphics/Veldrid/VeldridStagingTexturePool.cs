// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridStagingTexturePool : VeldridStagingResourcePool<Texture>
    {
        public VeldridStagingTexturePool(VeldridRenderer renderer)
            : base(renderer, nameof(VeldridStagingTexturePool))
        {
        }

        /// <summary>
        /// Retrieves a staging texture to use as an intermediate storage for uploading textures to the GPU.
        /// This should be written once by the CPU as it is handed over to the GPU for copying its data to the target texture,
        /// once the GPU has finished copying, the staging texture will eventually return back to the pool for reuse.
        /// </summary>
        public Texture Get(int width, int height, PixelFormat format)
        {
            if (TryGet(t => t.Width >= width && t.Height >= height && t.Format == format, out var texture))
                return texture;

            texture = Renderer.Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, format, TextureUsage.Staging));
            AddNewResource(texture);
            return texture;
        }
    }
}
