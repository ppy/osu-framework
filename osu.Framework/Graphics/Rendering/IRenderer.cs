// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK.Graphics;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Draws to the screen.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Maximum number of <see cref="DrawNode"/>s a <see cref="Drawable"/> can draw with.
        /// This is a carefully-chosen number to enable the update and draw threads to work concurrently without causing unnecessary load.
        /// </summary>
        const int MAX_DRAW_NODES = 3;

        const int MAX_MIPMAP_LEVELS = 3;

        const int VERTICES_PER_TRIANGLE = 4;

        const int VERTICES_PER_QUAD = 4;

        const int INDICES_PER_QUAD = VERTICES_PER_QUAD + 2;

        /// <summary>
        /// Maximum number of vertices in a linear vertex buffer.
        /// </summary>
        const int MAX_VERTICES = ushort.MaxValue;

        /// <summary>
        /// Maximum number of quads in a quad vertex buffer.
        /// </summary>
        const int MAX_QUADS = ushort.MaxValue / INDICES_PER_QUAD;

        /// <summary>
        /// Enables or disables vertical sync.
        /// </summary>
        protected internal bool VerticalSync { get; set; }

        protected internal bool AllowTearing { get; set; }

        /// <summary>
        /// A <see cref="Storage"/> that can be used to cache objects.
        /// </summary>
        protected internal Storage? CacheStorage { set; }

        /// <summary>
        /// The current frame index.
        /// </summary>
        ulong FrameIndex { get; }

        /// <summary>
        /// The maximum allowed texture size.
        /// </summary>
        int MaxTextureSize { get; }

        /// <summary>
        /// The maximum number of texture uploads to dequeue and upload per frame.
        /// Defaults to 32.
        /// </summary>
        int MaxTexturesUploadedPerFrame { get; set; }

        /// <summary>
        /// The maximum number of pixels to upload per frame.
        /// Defaults to 2 megapixels (8mb alloc).
        /// </summary>
        int MaxPixelsUploadedPerFrame { get; set; }

        /// <summary>
        /// Whether the depth is in the range [0, 1] (i.e. Reversed-Z). If <c>false</c>, depth is in the range [-1, 1].
        /// </summary>
        bool IsDepthRangeZeroToOne { get; }

        /// <summary>
        /// Whether the texture coordinates begin in the top-left of the texture. If <c>false</c>, (0, 0) corresponds to the bottom-left texel of the texture.
        /// </summary>
        bool IsUvOriginTopLeft { get; }

        /// <summary>
        /// Whether the y-coordinate ranges from -1 (top) to 1 (bottom). If <c>false</c>, the y-coordinate ranges from -1 (bottom) to 1 (top).
        /// </summary>
        bool IsClipSpaceYInverted { get; }

        /// <summary>
        /// The current masking parameters.
        /// </summary>
        ref readonly MaskingInfo CurrentMaskingInfo { get; }

        /// <summary>
        /// The current viewport.
        /// </summary>
        RectangleI Viewport { get; }

        /// <summary>
        /// The current scissor rectangle.
        /// </summary>
        RectangleI Scissor { get; }

        /// <summary>
        /// The current scissor offset.
        /// </summary>
        Vector2I ScissorOffset { get; }

        /// <summary>
        /// The current projection matrix.
        /// </summary>
        Matrix4 ProjectionMatrix { get; }

        /// <summary>
        /// The current depth parameters.
        /// </summary>
        DepthInfo CurrentDepthInfo { get; }

        /// <summary>
        /// The current stencil parameters.
        /// </summary>
        StencilInfo CurrentStencilInfo { get; }

        /// <summary>
        /// The current horizontal texture wrap mode.
        /// </summary>
        WrapMode CurrentWrapModeS { get; }

        /// <summary>
        /// The current vertical texture wrap mode.
        /// </summary>
        WrapMode CurrentWrapModeT { get; }

        /// <summary>
        /// Whether any masking parameters are currently applied.
        /// </summary>
        bool IsMaskingActive { get; }

        /// <summary>
        /// Whether the currently bound framebuffer is the backbuffer.
        /// </summary>
        bool UsingBackbuffer { get; }

        /// <summary>
        /// The texture for a white pixel.
        /// </summary>
        Texture WhitePixel { get; }

        /// <summary>
        /// The current depth of <see cref="TexturedVertex2D"/> vertices when drawn to the backbuffer.
        /// </summary>
        internal DepthValue BackbufferDepth { get; }

        /// <summary>
        /// Whether this <see cref="IRenderer"/> has been initialised using <see cref="Initialise"/>.
        /// </summary>
        bool IsInitialised { get; }

        /// <summary>
        /// Performs a once-off initialisation of this <see cref="IRenderer"/>.
        /// </summary>
        protected internal void Initialise(IGraphicsSurface graphicsSurface);

        /// <summary>
        /// Resets any states to prepare for drawing a new frame.
        /// </summary>
        /// <param name="windowSize">The full window size.</param>
        protected internal void BeginFrame(Vector2 windowSize);

        /// <summary>
        /// Performs any last actions before a frame ends.
        /// </summary>
        protected internal void FinishFrame();

        /// <summary>
        /// Swaps the back buffer with the front buffer to display the new frame.
        /// </summary>
        protected internal void SwapBuffers();

        /// <summary>
        /// Waits until all renderer commands have been fully executed GPU-side, as signaled by the graphics backend.
        /// </summary>
        /// <remarks>
        /// This is equivalent to a <c>glFinish</c> call.
        /// </remarks>
        protected internal void WaitUntilIdle();

        /// <summary>
        /// Waits until the GPU signals that the next frame is ready to be rendered.
        /// </summary>
        protected internal void WaitUntilNextFrameReady();

        /// <summary>
        /// Invoked when the rendering thread is active and commands will be enqueued.
        /// This is mainly required for OpenGL renderers to mark context as current before performing GL calls.
        /// </summary>
        protected internal void MakeCurrent();

        /// <summary>
        /// Invoked when the rendering thread is suspended and no more commands will be enqueued.
        /// This is mainly required for OpenGL renderers to mark context as current before performing GL calls.
        /// </summary>
        protected internal void ClearCurrent();

        /// <summary>
        /// Flushes the currently active vertex batch.
        /// </summary>
        /// <param name="source">The source performing the flush, for profiling purposes.</param>
        internal void FlushCurrentBatch(FlushBatchSource? source);

        /// <summary>
        /// Binds a texture.
        /// </summary>
        /// <param name="texture">The texture to bind.</param>
        /// <param name="unit">The sampling unit in which the texture is to be bound.</param>
        /// <param name="wrapModeS">The texture's horizontal wrap mode.</param>
        /// <param name="wrapModeT">The texture's vertex wrap mode.</param>
        /// <returns>Whether the texture was successfully bound.</returns>
        bool BindTexture(Texture texture, int unit = 0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null);

        /// <summary>
        /// Clears the currently bound frame buffer.
        /// </summary>
        /// <param name="clearInfo">The clearing parameters.</param>
        void Clear(ClearInfo clearInfo);

        /// <summary>
        /// Applies a new scissor test enablement state.
        /// </summary>
        /// <param name="enabled">Whether the scissor test is enabled.</param>
        void PushScissorState(bool enabled);

        /// <summary>
        /// Restores the last scissor test enablement state.
        /// </summary>
        void PopScissorState();

        /// <summary>
        /// Sets the current blending state.
        /// </summary>
        /// <param name="blendingParameters">The blending parameters.</param>
        void SetBlend(BlendingParameters blendingParameters);

        /// <summary>
        /// Sets a mask deciding which colour components are affected during blending.
        /// </summary>
        /// <param name="blendingMask">The blending mask.</param>
        void SetBlendMask(BlendingMask blendingMask);

        /// <summary>
        /// Applies a new viewport rectangle.
        /// </summary>
        /// <param name="viewport">The viewport rectangle.</param>
        void PushViewport(RectangleI viewport);

        /// <summary>
        /// Restores the last viewport rectangle.
        /// </summary>
        void PopViewport();

        /// <summary>
        /// Applies a new scissor rectangle.
        /// </summary>
        /// <param name="scissor">The scissor rectangle.</param>
        void PushScissor(RectangleI scissor);

        /// <summary>
        /// Restores the last scissor rectangle.
        /// </summary>
        void PopScissor();

        /// <summary>
        /// Applies a new scissor offset to the scissor rectangle.
        /// </summary>
        /// <param name="offset">The scissor offset.</param>
        void PushScissorOffset(Vector2I offset);

        /// <summary>
        /// Restores the last scissor offset.
        /// </summary>
        void PopScissorOffset();

        /// <summary>
        /// Applies a new projection matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        void PushProjectionMatrix(Matrix4 matrix);

        /// <summary>
        /// Restores the last projection matrix.
        /// </summary>
        void PopProjectionMatrix();

        /// <summary>
        /// Applies new masking parameters.
        /// </summary>
        /// <param name="maskingInfo">The masking parameters.</param>
        /// <param name="overwritePreviousScissor">Whether to use the last scissor rectangle.</param>
        void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false);

        /// <summary>
        /// Restores the last masking parameters.
        /// </summary>
        void PopMaskingInfo();

        /// <summary>
        /// Applies new depth parameters.
        /// </summary>
        /// <param name="depthInfo">The depth parameters.</param>
        void PushDepthInfo(DepthInfo depthInfo);

        /// <summary>
        /// Restores the last depth parameters.
        /// </summary>
        void PopDepthInfo();

        /// <summary>
        /// Applies new stencil parameters.
        /// </summary>
        /// <param name="stencilInfo">The stencil parameters.</param>
        void PushStencilInfo(StencilInfo stencilInfo);

        /// <summary>
        /// Restores the last stencil parameters.
        /// </summary>
        void PopStencilInfo();

        /// <summary>
        /// Schedules an expensive operation to a queue from which a maximum of one operation is performed per frame.
        /// </summary>
        /// <param name="operation">The operation to schedule.</param>
        void ScheduleExpensiveOperation(ScheduledDelegate operation);

        /// <summary>
        /// Schedules a disposal action to be run on the next frame.
        /// </summary>
        /// <param name="disposalAction">The disposal action.</param>
        /// <param name="target">The target to be disposed.</param>
        void ScheduleDisposal<T>(Action<T> disposalAction, T target);

        /// <summary>
        /// Returns an image containing the current content of the backbuffer, i.e. takes a screenshot.
        /// </summary>
        protected internal Image<Rgba32> TakeScreenshot();

        /// <summary>
        /// Returns an image containing the content of a framebuffer.
        /// </summary>
        Image<Rgba32>? ExtractFrameBufferData(IFrameBuffer frameBuffer);

        /// <summary>
        /// Creates a new <see cref="IShaderPart"/>.
        /// </summary>
        /// <param name="store">The shader store to load headers with.</param>
        /// <param name="name">The name of the shader part.</param>
        /// <param name="rawData">The content of the shader part.</param>
        /// <param name="partType">The type of the shader part.</param>
        /// <returns>The <see cref="IShaderPart"/>.</returns>
        protected internal IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType);

        /// <summary>
        /// Creates a new <see cref="IShader"/>.
        /// </summary>
        /// <param name="name">The name of the shader.</param>
        /// <param name="parts">The <see cref="IShaderPart"/>s associated with this shader.</param>
        /// <returns>The <see cref="IShader"/>.</returns>
        protected internal IShader CreateShader(string name, IShaderPart[] parts);

        /// <summary>
        /// Creates a new <see cref="IFrameBuffer"/>.
        /// </summary>
        /// <param name="renderBufferFormats">Any render buffer formats.</param>
        /// <param name="filteringMode">The texture filtering mode.</param>
        /// <returns>The <see cref="IFrameBuffer"/>.</returns>
        IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear);

        /// <summary>
        /// Creates a new <see cref="Texture"/>.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="manualMipmaps">Whether manual mipmaps will be uploaded to the texture. If false, the texture will compute mipmaps automatically.</param>
        /// <param name="filteringMode">The filtering mode.</param>
        /// <param name="initialisationColour">The colour to initialise texture levels with (in the case of sub region initial uploads). If null, no initialisation is provided out-of-the-box.</param>
        /// <param name="wrapModeS">The texture's horizontal wrap mode.</param>
        /// <param name="wrapModeT">The texture's vertex wrap mode.</param>
        /// <returns>The <see cref="Texture"/>.</returns>
        Texture CreateTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, WrapMode wrapModeS = WrapMode.None,
                              WrapMode wrapModeT = WrapMode.None, Color4? initialisationColour = null);

        /// <summary>
        /// Creates a new video texture.
        /// </summary>
        Texture CreateVideoTexture(int width, int height);

        /// <summary>
        /// Creates a new linear vertex batch, accepting vertices and drawing as a given primitive type.
        /// </summary>
        /// <param name="size">Number of quads.</param>
        /// <param name="maxBuffers">Maximum number of vertex buffers.</param>
        /// <param name="topology">The type of primitive the vertices are drawn as.</param>
        IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology) where TVertex : unmanaged, IEquatable<TVertex>, IVertex;

        /// <summary>
        /// Creates a new quad vertex batch, accepting vertices and drawing as quads.
        /// </summary>
        /// <param name="size">Number of quads.</param>
        /// <param name="maxBuffers">Maximum number of vertex buffers.</param>
        IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : unmanaged, IEquatable<TVertex>, IVertex;

        /// <summary>
        /// Creates a buffer that stores data for a uniform block of a <see cref="IShader"/>.
        /// </summary>
        /// <typeparam name="TData">The type of data in the buffer.</typeparam>
        IUniformBuffer<TData> CreateUniformBuffer<TData>() where TData : unmanaged, IEquatable<TData>;

        /// <summary>
        /// Creates a buffer that can be used to store an array of data for use in a <see cref="IShader"/>.
        /// </summary>
        /// <param name="uboSize">The number of elements this buffer should contain if Shader Storage Buffer Objects <b>are not</b> supported by the platform.
        /// A safe value is <c>16384/{data_size}</c>. The value must match the definition of the UBO implementation in the shader.</param>
        /// <param name="ssboSize">The number of elements this buffer should contain if Shader Storage Buffer Objects <b>are</b> supported by the platform.
        /// May be any value up to <c>{vram_size}/{data_size}</c>.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Internally, this buffer may be implemented as either a "Uniform Buffer Object" (UBO) or
        /// a "Shader Storage Buffer Object" (SSBO) depending on the capabilities of the platform.</item>
        /// <item>UBOs are more broadly supported but cannot hold as much data as SSBOs.</item>
        /// <item>Shaders must provide implementations for both types of buffers to properly support this storage.</item>
        /// </list>
        /// </remarks>
        /// <typeparam name="TData">The type of data to be stored in the buffer.</typeparam>
        /// <returns>An <see cref="IShaderStorageBufferObject{TData}"/>.</returns>
        IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize) where TData : unmanaged, IEquatable<TData>;

        /// <summary>
        /// Sets the value of a uniform.
        /// </summary>
        /// <param name="uniform">The uniform to set.</param>
        internal void SetUniform<T>(IUniformWithValue<T> uniform) where T : unmanaged, IEquatable<T>;

        internal IVertexBatch<TexturedVertex2D> DefaultQuadBatch { get; }

        internal void PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch);

        internal void PopQuadBatch();

        internal void EnterDrawNode(DrawNode node)
        {
        }

        internal void ExitDrawNode()
        {
        }

        #region TextureVisualiser Support

        /// <summary>
        /// An event which is invoked every time a <see cref="Texture"/> is created.
        /// </summary>
        internal event Action<Texture> TextureCreated;

        /// <summary>
        /// Retrieves all <see cref="Texture"/>s that have been created.
        /// </summary>
        internal Texture[] GetAllTextures();

        #endregion
    }
}
