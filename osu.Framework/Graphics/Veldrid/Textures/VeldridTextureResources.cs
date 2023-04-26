// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    /// <summary>
    /// todo: change description
    /// Stores the underlying <see cref="global::Veldrid.Texture"/> and <see cref="global::Veldrid.Sampler"/> resources of a <see cref="VeldridTexture"/>.
    /// </summary>
    internal class VeldridTextureResources : IDisposable
    {
        public readonly Texture Texture;
        private readonly bool disposeResources;

        private Sampler? sampler;

        public Sampler? Sampler
        {
            get => sampler;
            set
            {
                sampler?.Dispose();
                sampler = value;

                Set?.Dispose();
                Set = null;
            }
        }

        private readonly TextureView?[] textureViews = new TextureView?[IRenderer.MAX_MIPMAP_LEVELS + 1];
        private readonly ResourceSet?[] mipmapSets = new ResourceSet?[IRenderer.MAX_MIPMAP_LEVELS];

        public ResourceSet? Set { get; private set; }

        public VeldridTextureResources(Texture texture, Sampler? sampler, VeldridRenderer renderer, bool disposeResources = true)
        {
            Texture = texture;
            Sampler = sampler;

            if (renderer.Device.Features.SubsetTextureView)
            {
                for (int i = 0; i < textureViews.Length; i++)
                    textureViews[i] = renderer.Factory.CreateTextureView(new TextureViewDescription(texture, (uint)i, 1, 0, 1));
            }

            this.disposeResources = disposeResources;
        }

        /// <summary>
        /// Creates a <see cref="ResourceSet"/> from the <see cref="global::Veldrid.Texture"/> and <see cref="global::Veldrid.Sampler"/>.
        /// </summary>
        /// <param name="renderer">The renderer to create the resource set for.</param>
        /// <param name="layout">The resource layout which this set will be attached to. Assumes a layout with the texture in slot 0 and the sampler in slot 1.</param>
        /// <returns>The resource set.</returns>
        public ResourceSet GetResourceSet(VeldridRenderer renderer, ResourceLayout layout)
        {
            if (Sampler == null)
                throw new InvalidOperationException("Attempting to create resource set without a sampler attached to the resources.");

            return Set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, Texture, Sampler));
        }

        public ResourceSet GetMipmapResourceSet(VeldridRenderer renderer, ResourceLayout layout, Sampler sampler, int level)
        {
            Debug.Assert(renderer.Device.Features.SubsetTextureView);
            return mipmapSets[level - 1] = renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, textureViews[level - 1], sampler, textureViews[level]));
        }

        public void Dispose()
        {
            if (disposeResources)
            {
                Texture.Dispose();
                Sampler?.Dispose();
            }

            foreach (var set in mipmapSets)
                set?.Dispose();

            foreach (var view in textureViews)
                view?.Dispose();

            Set?.Dispose();
        }
    }
}
