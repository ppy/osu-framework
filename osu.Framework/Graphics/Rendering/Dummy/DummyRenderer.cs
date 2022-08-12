// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Threading;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    /// <summary>
    /// An <see cref="IRenderer"/> that does nothing. May be used for tests that don't have a visual output.
    /// </summary>
    public sealed class DummyRenderer : IRenderer
    {
        public int MaxTextureSize => int.MaxValue;
        public int MaxTexturesUploadedPerFrame { get; set; } = int.MaxValue;
        public int MaxPixelsUploadedPerFrame { get; set; } = int.MaxValue;

        public ref readonly MaskingInfo CurrentMaskingInfo => ref maskingInfo;
        private readonly MaskingInfo maskingInfo;

        public RectangleI Viewport => RectangleI.Empty;
        public RectangleF Ortho => RectangleF.Empty;
        public RectangleI Scissor => RectangleI.Empty;
        public Vector2I ScissorOffset => Vector2I.Zero;
        public Matrix4 ProjectionMatrix => Matrix4.Identity;
        public DepthInfo CurrentDepthInfo => DepthInfo.Default;
        public StencilInfo CurrentStencilInfo => StencilInfo.Default;
        public WrapMode CurrentWrapModeS => WrapMode.None;
        public WrapMode CurrentWrapModeT => WrapMode.None;
        public bool IsMaskingActive => false;
        public float BackbufferDrawDepth => 0;
        public bool UsingBackbuffer => false;
        public Texture WhitePixel { get; }

        public DummyRenderer()
        {
            maskingInfo = default;
            WhitePixel = new Texture(new DummyNativeTexture(this), WrapMode.None, WrapMode.None);
        }

        void IRenderer.Initialise()
        {
        }

        void IRenderer.BeginFrame(Vector2 windowSize)
        {
        }

        void IRenderer.FinishFrame()
        {
        }

        public bool BindTexture(Texture texture, int unit = 0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null)
            => true;

        public void UseProgram(IShader? shader)
        {
        }

        public void Clear(ClearInfo clearInfo)
        {
        }

        public void PushScissorState(bool enabled)
        {
        }

        public void PopScissorState()
        {
        }

        public void SetBlend(BlendingParameters blendingParameters)
        {
        }

        public void PushViewport(RectangleI viewport)
        {
        }

        public void PopViewport()
        {
        }

        public void PushScissor(RectangleI scissor)
        {
        }

        public void PopScissor()
        {
        }

        public void PushScissorOffset(Vector2I offset)
        {
        }

        public void PopScissorOffset()
        {
        }

        public void PushProjectionMatrix(Matrix4 matrix)
        {
        }

        public void PopProjectionMatrix()
        {
        }

        public void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false)
        {
        }

        public void PopMaskingInfo()
        {
        }

        public void PushDepthInfo(DepthInfo depthInfo)
        {
        }

        public void PopDepthInfo()
        {
        }

        public void PushStencilInfo(StencilInfo stencilInfo)
        {
        }

        public void PopStencilInfo()
        {
        }

        public void ScheduleExpensiveOperation(ScheduledDelegate operation) => operation.RunTask();

        public void ScheduleDisposal<T>(Action<T> disposalAction, T target) => disposalAction(target);

        IShaderPart IRenderer.CreateShaderPart(ShaderManager manager, string name, byte[]? rawData, ShaderPartType partType)
            => new DummyShaderPart();

        IShader IRenderer.CreateShader(string name, params IShaderPart[] parts)
            => new DummyShader(this);

        public IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new DummyFrameBuffer(this);

        public Texture CreateTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, WrapMode wrapModeS = WrapMode.None,
                                     WrapMode wrapModeT = WrapMode.None, Rgba32 initialisationColour = default)
            => new Texture(new DummyNativeTexture(this) { Width = width, Height = height }, wrapModeS, wrapModeT);

        public Texture CreateVideoTexture(int width, int height)
            => CreateTexture(width, height);

        public IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new DummyVertexBatch<TVertex>();

        public IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new DummyVertexBatch<TVertex>();

        void IRenderer.SetUniform<T>(IUniformWithValue<T> uniform)
        {
        }

        void IRenderer.SetDrawDepth(float drawDepth)
        {
        }

        IVertexBatch<TexturedVertex2D> IRenderer.DefaultQuadBatch => new DummyVertexBatch<TexturedVertex2D>();

        void IRenderer.PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch)
        {
        }

        void IRenderer.PopQuadBatch()
        {
        }

        event Action<Texture>? IRenderer.TextureCreated
        {
            add
            {
            }
            remove
            {
            }
        }

        Texture[] IRenderer.GetAllTextures() => Array.Empty<Texture>();
    }
}
