// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredRenderer : Renderer, IVeldridRenderer
    {
        public VeldridDevice VeldridDevice { get; private set; } = null!;
        public DeferredContext Context { get; private set; } = null!;

        private readonly HashSet<IVeldridUniformBuffer> uniformBufferResetList = new HashSet<IVeldridUniformBuffer>();
        private readonly Stack<DrawNode> drawNodeStack = new Stack<DrawNode>();

        protected override void Initialise(IGraphicsSurface graphicsSurface)
        {
            VeldridDevice = new VeldridDevice(graphicsSurface);
            Context = new DeferredContext(this);

            MaxTextureSize = VeldridDevice.MaxTextureSize;
        }

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            foreach (var ubo in uniformBufferResetList)
                ubo.ResetCounters();

            uniformBufferResetList.Clear();
            drawNodeStack.Clear();
            Context.NewFrame();

            VeldridDevice.NewFrame(new Vector2I((int)windowSize.X, (int)windowSize.Y));

            base.BeginFrame(windowSize);
        }

        protected internal override void FinishFrame()
        {
            base.FinishFrame();

            new EventProcessor(Context).ProcessEvents();

            VeldridDevice.FinishFrame();
        }

        protected override bool SetTextureImplementation(INativeTexture? texture, int unit)
        {
            if (texture == null)
                return false;

            Context.EnqueueEvent(new SetTextureEvent(Context.Reference(texture), unit));
            return true;
        }

        protected override void SetFrameBufferImplementation(IFrameBuffer? frameBuffer)
            => Context.EnqueueEvent(new SetFrameBufferEvent(frameBuffer == null ? Context.NullReference() : Context.Reference(frameBuffer)));

        protected override void SetUniformBufferImplementation(string blockName, IUniformBuffer buffer)
            => Context.EnqueueEvent(new SetUniformBufferEvent(Context.Reference(blockName), Context.Reference(buffer)));

        public override void DrawVerticesImplementation(PrimitiveTopology type, int vertexStart, int verticesCount)
        {
            // Handled by the batch...
        }

        protected override void ClearImplementation(ClearInfo clearInfo)
            => Context.EnqueueEvent(new ClearEvent(clearInfo));

        protected override void SetScissorStateImplementation(bool enabled)
            => Context.EnqueueEvent(new SetScissorStateEvent(enabled));

        protected override void SetBlendImplementation(BlendingParameters blendingParameters)
            => Context.EnqueueEvent(new SetBlendEvent(blendingParameters));

        protected override void SetBlendMaskImplementation(BlendingMask blendingMask)
            => Context.EnqueueEvent(new SetBlendMaskEvent(blendingMask));

        protected override void SetViewportImplementation(RectangleI viewport)
            => Context.EnqueueEvent(new SetViewportEvent(viewport));

        protected override void SetScissorImplementation(RectangleI scissor)
            => Context.EnqueueEvent(new SetScissorEvent(scissor));

        protected override void SetDepthInfoImplementation(DepthInfo depthInfo)
            => Context.EnqueueEvent(new SetDepthInfoEvent(depthInfo));

        protected override void SetStencilInfoImplementation(StencilInfo stencilInfo)
            => Context.EnqueueEvent(new SetStencilInfoEvent(stencilInfo));

        protected override void SetShaderImplementation(IShader shader)
            => Context.EnqueueEvent(new SetShaderEvent(Context.Reference(shader)));

        protected override void SetUniformImplementation<T>(IUniformWithValue<T> uniform)
            => throw new NotSupportedException();

        void IVeldridRenderer.BindShader(VeldridShader shader)
            => BindShader(shader);

        void IVeldridRenderer.UnbindShader(VeldridShader shader)
            => UnbindShader(shader);

        void IVeldridRenderer.UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data)
            => VeldridDevice.Graphics.UpdateTexture(texture, x, y, width, height, level, data);

        void IVeldridRenderer.UpdateTexture(Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes)
            => VeldridDevice.Graphics.UpdateTexture(texture, x, y, width, height, level, data, rowLengthInBytes);

        void IVeldridRenderer.EnqueueTextureUpload(VeldridTexture texture)
            => EnqueueTextureUpload(texture);

        void IVeldridRenderer.GenerateMipmaps(VeldridTexture texture)
            => VeldridDevice.Graphics.Commands.GenerateMipmaps(texture.GetResourceList().Single().Texture);

        public void RegisterUniformBufferForReset(IVeldridUniformBuffer veldridUniformBuffer)
            => uniformBufferResetList.Add(veldridUniformBuffer);

        bool IVeldridRenderer.UseStructuredBuffers
            => VeldridDevice.UseStructuredBuffers;

        GraphicsSurfaceType IVeldridRenderer.SurfaceType
            => VeldridDevice.SurfaceType;

        public ResourceFactory Factory
            => VeldridDevice.Factory;

        public GraphicsDevice Device
            => VeldridDevice.Device;

        protected internal override bool VerticalSync
        {
            get => VeldridDevice.VerticalSync;
            set => VeldridDevice.VerticalSync = value;
        }

        protected internal override bool AllowTearing
        {
            get => VeldridDevice.AllowTearing;
            set => VeldridDevice.AllowTearing = value;
        }

        public override bool IsDepthRangeZeroToOne
            => VeldridDevice.IsDepthRangeZeroToOne;

        public override bool IsUvOriginTopLeft
            => VeldridDevice.IsUvOriginTopLeft;

        public override bool IsClipSpaceYInverted
            => VeldridDevice.IsClipSpaceYInverted;

        protected internal override void SwapBuffers()
            => VeldridDevice.SwapBuffers();

        protected internal override void WaitUntilIdle()
            => VeldridDevice.WaitUntilIdle();

        protected internal override void WaitUntilNextFrameReady()
            => VeldridDevice.WaitUntilNextFrameReady();

        protected internal override void MakeCurrent()
            => VeldridDevice.MakeCurrent();

        protected internal override void ClearCurrent()
            => VeldridDevice.ClearCurrent();

        protected internal override Image<Rgba32> TakeScreenshot()
            => VeldridDevice.TakeScreenshot();

        protected override IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType)
            => new VeldridShaderPart(this, rawData, partType, store);

        protected override IShader CreateShader(string name, IShaderPart[] parts, ShaderCompilationStore compilationStore)
            => new DeferredShader(this, new VeldridShader(this, name, parts.Cast<VeldridShaderPart>().ToArray(), compilationStore));

        public override IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new DeferredFrameBuffer(this, renderBufferFormats?.ToPixelFormats(), filteringMode.ToSamplerFilter());

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Color4? initialisationColour = null)
            => new VeldridTexture(this, width, height, manualMipmaps, filteringMode.ToSamplerFilter(), initialisationColour);

        protected override INativeTexture CreateNativeVideoTexture(int width, int height)
            => new VeldridVideoTexture(this, width, height);

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
            => new DeferredVertexBatch<TVertex>(this, topology, VeldridIndexLayout.Linear);

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
            => new DeferredVertexBatch<TVertex>(this, PrimitiveTopology.Triangles, VeldridIndexLayout.Quad);

        protected override IUniformBuffer<TData> CreateUniformBuffer<TData>()
            => new DeferredUniformBuffer<TData>(this);

        protected override IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize)
            => new DeferredShaderStorageBufferObject<TData>(this, ssboSize);
    }
}
