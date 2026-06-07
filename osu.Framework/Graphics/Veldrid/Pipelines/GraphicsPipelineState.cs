// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Pipelines
{
    /// <summary>
    /// A snapshot of <see cref="GraphicsPipeline"/> CPU-side state, used to restore bindings after a mid-frame pipeline restart.
    /// </summary>
    internal sealed class GraphicsPipelineState
    {
        public GraphicsPipelineDescription PipelineDescription { get; init; }
        public IVeldridFrameBuffer? CurrentFrameBuffer { get; init; }
        public VeldridShader? CurrentShader { get; init; }
        public VeldridIndexBuffer? CurrentIndexBuffer { get; init; }
        public DeviceBuffer? CurrentVertexBuffer { get; init; }
        public VertexLayoutDescription CurrentVertexLayout { get; init; }
        public RectangleI Viewport { get; init; }
        public RectangleI Scissor { get; init; }
        public bool ViewportDefined { get; init; }
        public bool ScissorDefined { get; init; }
        public Dictionary<int, VeldridTextureResources> AttachedTextures { get; init; } = null!;
        public Dictionary<string, IVeldridUniformBuffer> AttachedUniformBuffers { get; init; } = null!;
        public Dictionary<IVeldridUniformBuffer, uint> UniformBufferOffsets { get; init; } = null!;
    }
}
