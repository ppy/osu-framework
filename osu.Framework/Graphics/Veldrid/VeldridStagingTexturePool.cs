// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Veldrid.Pipelines;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridStagingTexturePool : VeldridStagingResourcePool<Texture>
    {
        public VeldridStagingTexturePool(SimplePipeline pipeline)
            : base(pipeline, nameof(VeldridStagingTexturePool))
        {
        }

        public Texture Get(int width, int height, PixelFormat format)
        {
            if (TryGet(t => t.Width >= width && t.Height >= height && t.Format == format, out var texture))
                return texture;

            texture = Pipeline.Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, format, TextureUsage.Staging));
            AddNewResource(texture);
            return texture;
        }
    }
}
