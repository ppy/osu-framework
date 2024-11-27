// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    /// <summary>
    /// An <see cref="IRenderer"/> that does nothing. May be used for tests that don't have a visual output.
    /// </summary>
    public sealed class DummyRenderer : Renderer
    {
        protected internal override bool VerticalSync { get; set; } = true;
        protected internal override bool AllowTearing { get; set; }
        public override bool IsDepthRangeZeroToOne => true;
        public override bool IsUvOriginTopLeft => true;
        public override bool IsClipSpaceYInverted => true;

        protected internal override Image<Rgba32> TakeScreenshot()
            => new Image<Rgba32>(1, 1);

        protected override IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType)
            => new DummyShaderPart();

        protected override IShader CreateShader(string name, IShaderPart[] parts, ShaderCompilationStore compilationStore)
            => new DummyShader(this);

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
            => new DummyVertexBatch<TVertex>();

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
            => new DummyVertexBatch<TVertex>();

        protected override IUniformBuffer<TData> CreateUniformBuffer<TData>()
            => new DummyUniformBuffer<TData>();

        protected override IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize)
            => new DummyShaderStorageBufferObject<TData>(ssboSize);

        public Texture CreateTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, WrapMode wrapModeS = WrapMode.None)
            => base.CreateTexture(width, height, manualMipmaps, filteringMode, wrapModeS, wrapModeS, null);

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Color4? initialisationColour = null)
            => new DummyNativeTexture(this, width, height);

        protected override INativeTexture CreateNativeVideoTexture(int width, int height)
            => new DummyNativeTexture(this, width, height);

        protected override void Initialise(IGraphicsSurface graphicsSurface)
        {
        }

        protected internal override void SwapBuffers()
        {
        }

        protected internal override void WaitUntilIdle()
        {
        }

        protected internal override void WaitUntilNextFrameReady()
        {
        }

        protected internal override void MakeCurrent()
        {
        }

        protected internal override void ClearCurrent()
        {
        }

        protected override void ClearImplementation(ClearInfo clearInfo)
        {
        }

        protected override void SetBlendImplementation(BlendingParameters blendingParameters)
        {
        }

        protected override void SetBlendMaskImplementation(BlendingMask blendingMask)
        {
        }

        protected override void SetViewportImplementation(RectangleI viewport)
        {
        }

        protected override void SetScissorImplementation(RectangleI scissor)
        {
        }

        protected override void SetScissorStateImplementation(bool enabled)
        {
        }

        protected override void SetDepthInfoImplementation(DepthInfo depthInfo)
        {
        }

        protected override void SetStencilInfoImplementation(StencilInfo stencilInfo)
        {
        }

        protected override bool SetTextureImplementation(INativeTexture? texture, int unit)
            => true;

        protected override void SetFrameBufferImplementation(IFrameBuffer? frameBuffer)
        {
        }

        protected override void DeleteFrameBufferImplementation(IFrameBuffer frameBuffer)
        {
        }

        public override void DrawVerticesImplementation(PrimitiveTopology topology, int vertexStart, int verticesCount)
        {
        }

        protected override void SetShaderImplementation(IShader shader)
        {
        }

        protected override void SetUniformImplementation<T>(IUniformWithValue<T> uniform)
        {
        }

        protected override void SetUniformBufferImplementation(string blockName, IUniformBuffer buffer)
        {
        }

        public override IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new DummyFrameBuffer(this);
    }
}
