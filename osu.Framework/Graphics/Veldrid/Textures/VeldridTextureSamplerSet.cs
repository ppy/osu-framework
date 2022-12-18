// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    /// <summary>
    /// Represents a texture-sampler resource usable by <see cref="VeldridRenderer"/> to bind textures, acting similar to an OpenGL texture ID.
    /// </summary>
    internal class VeldridTextureSamplerSet : IDisposable
    {
        public IReadOnlyList<Texture> Textures { get; }
        public Texture Texture => Textures.Single();

        public Sampler Sampler { get; }

        public ResourceLayout Layout { get; }

        private readonly ResourceSet resourceSet;

        public VeldridTextureSamplerSet(VeldridRenderer renderer, Texture texture, Sampler sampler)
            : this(renderer, new[] { texture }, sampler)
        {
        }

        public VeldridTextureSamplerSet(VeldridRenderer renderer, Texture[] textures, Sampler sampler)
        {
            Textures = textures;
            Sampler = sampler;

            Layout = renderer.GetTextureResourceLayout(textures.Length);

            resourceSet = renderer.Factory.CreateResourceSet(new ResourceSetDescription(Layout, textures.Append<BindableResource>(sampler).ToArray()));
        }

        public void Dispose()
        {
            resourceSet?.Dispose();
        }

        public static implicit operator ResourceSet(VeldridTextureSamplerSet veldridTextureSet) => veldridTextureSet.resourceSet;
    }
}
