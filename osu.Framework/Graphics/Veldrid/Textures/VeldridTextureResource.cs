// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal class VeldridTextureResource
    {
        private readonly Texture texture;
        private readonly Sampler sampler;
        private ResourceSet? set;

        public VeldridTextureResource(Texture texture, Sampler sampler)
        {
            this.texture = texture;
            this.sampler = sampler;
        }

        public ResourceSet GetResourceSet(VeldridRenderer renderer, ResourceLayout layout)
            => set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, texture, sampler));
    }
}
