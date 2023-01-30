// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Veldrid.Buffers;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    // Todo: Dispose
    internal class VeldridUniformBlock : IVeldridResourceBlock
    {
        public int Index { get; }
        public ResourceLayout Layout { get; }
        public ResourceSet? Set { get; private set; }

        private readonly VeldridRenderer renderer;
        private IVeldridUniformBuffer? assignedBuffer;

        public VeldridUniformBlock(VeldridRenderer renderer, int index, string name)
        {
            this.renderer = renderer;

            Index = index;
            Layout = renderer.Factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(name, ResourceKind.UniformBuffer, ShaderStages.Fragment | ShaderStages.Vertex)));
        }

        public void Assign(IUniformBuffer buffer)
        {
            if (buffer is not IVeldridUniformBuffer veldridBuffer)
                throw new ArgumentException($"Provided argument must be a {typeof(VeldridUniformBuffer<>)}");

            if (assignedBuffer == veldridBuffer)
                return;

            assignedBuffer = veldridBuffer;
            Set = renderer.Factory.CreateResourceSet(new ResourceSetDescription(Layout, veldridBuffer.Buffer));
        }
    }
}
