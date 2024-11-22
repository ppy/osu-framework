// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Veldrid.Pipelines;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridStagingTexturePool : VeldridStagingResourcePool<Texture>
    {
        public VeldridStagingTexturePool(GraphicsPipeline pipeline)
            : base(pipeline, nameof(VeldridStagingTexturePool))
        {
        }

        public Texture Get(int width, int height, PixelFormat format)
        {
            if (TryGet(match, new TextureLookup(width, height, format), out var texture))
                return texture;

            texture = Pipeline.Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, format, TextureUsage.Staging));
            AddNewResource(texture);
            return texture;
        }

        private static bool match(Texture texture, TextureLookup lookup)
            => texture.Width >= lookup.Width && texture.Height >= lookup.Height && texture.Format == lookup.Format;

        private readonly record struct TextureLookup(int Width, int Height, PixelFormat Format);
    }
}
