// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    /// <summary>
    /// Stores the underlying <see cref="global::Veldrid.Texture"/> and <see cref="global::Veldrid.Sampler"/> resources of a <see cref="VeldridTexture"/>.
    /// </summary>
    internal class VeldridTextureResources : IDisposable
    {
        public readonly Texture Texture;
        public readonly Sampler Sampler;
        private ResourceSet? set;

        public VeldridTextureResources(Texture texture, Sampler sampler)
        {
            Texture = texture;
            Sampler = sampler;
        }

        /// <summary>
        /// Creates a <see cref="ResourceSet"/> from the <see cref="global::Veldrid.Texture"/> and <see cref="global::Veldrid.Sampler"/>.
        /// </summary>
        /// <param name="renderer">The renderer to create the resource set for.</param>
        /// <param name="layout">The resource layout which this set will be attached to. Assumes a layout with the texture in slot 0 and the sampler in slot 1.</param>
        /// <returns>The resource set.</returns>
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
