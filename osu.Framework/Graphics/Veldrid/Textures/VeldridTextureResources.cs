// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    /// <summary>
    /// Stores device resources revolving around a <see cref="global::Veldrid.Texture"/>.
    /// </summary>
    internal class VeldridTextureResources : IDisposable
    {
        public readonly Texture Texture;

        private readonly TextureView?[] textureViews = new TextureView?[IRenderer.MAX_MIPMAP_LEVELS + 1];

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

        private readonly bool disposeResources;

        private readonly ResourceSet?[] mipmapSets = new ResourceSet?[IRenderer.MAX_MIPMAP_LEVELS];

        public ResourceSet? Set { get; private set; }

        public VeldridTextureResources(Texture texture, Sampler? sampler, VeldridRenderer renderer, bool disposeResources = true)
        {
            Texture = texture;
            Sampler = sampler;

            if (renderer.Device.Features.SubsetTextureView)
            {
                for (int i = 0; i < Math.Min(texture.MipLevels, IRenderer.MAX_MIPMAP_LEVELS + 1); i++)
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

        /// <summary>
        /// Creates a special <see cref="ResourceSet"/> used specifically for mipmap generation.
        /// The <see cref="ResourceSet"/> contains a <see cref="TextureView"/> for the previous mipmap level, a <see cref="Sampler"/> provided by the caller, and a <see cref="TextureView"/> for the current mipmap level.
        /// </summary>
        /// <param name="renderer">The renderer to create the resource set for.</param>
        /// <param name="layout">The resource layout which this set will be attached to. Assumes a layout with a read-only texture view in slot 0, a sampler in slot 1, and read-write texture view in slot 2.</param>
        /// <param name="level">The mipmap level of the texture that will be written to.</param>
        /// <returns>The resource set.</returns>
        public ResourceSet GetMipmapResourceSet(VeldridRenderer renderer, ResourceLayout layout, int level)
        {
            Debug.Assert(renderer.Device.Features.SubsetTextureView);
            return mipmapSets[level - 1] = renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, textureViews[level - 1], renderer.Device.LinearSampler, textureViews[level]));
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
