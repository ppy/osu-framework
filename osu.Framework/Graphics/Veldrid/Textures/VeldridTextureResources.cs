// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Veldrid.Shaders;
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
        private readonly bool isFrameBufferTexture;

        private DeviceBuffer? auxDataBuffer;
        private ResourceSet? set;

        public VeldridTextureResources(Texture texture, Sampler sampler, bool isFrameBufferTexture)
        {
            Texture = texture;
            Sampler = sampler;
            this.isFrameBufferTexture = isFrameBufferTexture;
        }

        /// <summary>
        /// Creates a <see cref="ResourceSet"/> from the <see cref="global::Veldrid.Texture"/> and <see cref="global::Veldrid.Sampler"/>.
        /// </summary>
        /// <param name="renderer">The renderer to create the resource set for.</param>
        /// <param name="layout">The resource layout which this set will be attached to. Assumes a layout with the texture in slot 0 and the sampler in slot 1.</param>
        /// <returns>The resource set.</returns>
        public ResourceSet GetResourceSet(VeldridRenderer renderer, VeldridTextureUniformLayout layout)
        {
            if (set != null)
                return set;

            BindableResource[] resources = layout.HasAuxData
                ? new BindableResource[3]
                : new BindableResource[2];

            resources[0] = Texture;
            resources[1] = Sampler;

            if (layout.HasAuxData)
            {
                auxDataBuffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(default(AuxTextureData)), BufferUsage.UniformBuffer));
                renderer.BufferUpdateCommands.UpdateBuffer(auxDataBuffer, 0, new AuxTextureData { IsFrameBufferTexture = isFrameBufferTexture });

                resources[2] = auxDataBuffer;
            }

            return set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout.Layout, resources));
        }

        public void Dispose()
        {
            Texture.Dispose();
            Sampler.Dispose();
            auxDataBuffer?.Dispose();
            set?.Dispose();
        }
    }
}
