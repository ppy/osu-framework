// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Veldrid.Textures;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    // Todo: Dispose
    internal class VeldridTextureBlock : IVeldridResourceBlock
    {
        public int Index { get; }
        public ResourceLayout Layout { get; }
        public ResourceSet? Set { get; private set; }

        private readonly VeldridRenderer renderer;

        public VeldridTextureBlock(VeldridRenderer renderer, int index, string textureName, string samplerName)
        {
            this.renderer = renderer;

            Index = index;
            Layout = renderer.Factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(textureName, ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription(samplerName, ResourceKind.Sampler, ShaderStages.Fragment)));
        }

        public void Assign(VeldridTexture texture)
        {
            Set = renderer.Factory.CreateResourceSet(new ResourceSetDescription(Layout, texture.TextureResource, texture.SamplerResource));
        }
    }
}
