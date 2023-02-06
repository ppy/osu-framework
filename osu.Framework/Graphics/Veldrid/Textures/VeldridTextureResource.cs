// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal class VeldridTextureResource : IDisposable
    {
        public readonly Texture Texture;
        public readonly Sampler Sampler;
        private ResourceSet? set;

        public VeldridTextureResource(Texture texture, Sampler sampler)
        {
            Texture = texture;
            Sampler = sampler;
        }

        public ResourceSet GetResourceSet(VeldridRenderer renderer, ResourceLayout layout)
            => set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, Texture, Sampler));

        public void Dispose()
        {
            Texture.Dispose();
            Sampler.Dispose();
            set?.Dispose();
        }
    }
}
