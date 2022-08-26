// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Textures;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Threading;

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
        public const int MAX_DRAW_NODES = 3;

        public const int MAX_MIPMAP_LEVELS = 3;

        public const int VERTICES_PER_QUAD = 4;

        public const int VERTICES_PER_TRIANGLE = 4;

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
        /// The current backbuffer depth.
        /// </summary>
        float BackbufferDrawDepth { get; }

        /// <summary>
        /// Whether the currently bound framebuffer is the backbuffer.
        /// </summary>
        bool UsingBackbuffer { get; }

        /// <summary>
        /// The texture for a white pixel.
        /// </summary>
        Texture WhitePixel { get; }

        /// <summary>
        /// Performs a once-off initialisation of this <see cref="IRenderer"/>.
        /// </summary>
        internal void Initialise();

        /// <summary>
        /// Resets any states to prepare for drawing a new frame.
        /// </summary>
        /// <param name="windowSize">The full window size.</param>
        internal void BeginFrame(Vector2 windowSize);

        /// <summary>
        /// Performs any last actions before a frame ends.
        /// </summary>
        internal void FinishFrame();

        /// <summary>
        /// Binds a texture.
        /// </summary>
        /// <param name="texture">The texture to bind.</param>
        /// <param name="unit">The sampling unit in which the texture is to be bound.</param>
        /// <param name="wrapModeS">The texture's horizontal wrap mode.</param>
        /// <param name="wrapModeT">The texture's vertex wrap mode.</param>
        /// <returns>Whether <paramref name="texture"/> was newly-bound.</returns>
        bool BindTexture(Texture texture, int unit = 0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null);

        /// <summary>
        /// Binds a shader.
        /// </summary>
        /// <param name="shader">The shader to bind.</param>
        void UseProgram(IShader? shader);

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

        internal IShaderPart CreateShaderPart(ShaderManager manager, string name, byte[]? rawData, ShaderPartType partType);

        internal IShader CreateShader(string name, params IShaderPart[] parts);

        /// <summary>
        /// Creates a new <see cref="IFrameBuffer"/>.
        /// </summary>
        /// <param name="renderBufferFormats">Any render buffer formats.</param>
        /// <param name="filteringMode">The texture filtering mode.</param>
        /// <returns>The <see cref="IFrameBuffer"/>.</returns>
        IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear);

        /// <summary>
        /// Creates a new texture.
        /// </summary>
        Texture CreateTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, WrapMode wrapModeS = WrapMode.None,
                              WrapMode wrapModeT = WrapMode.None, Rgba32 initialisationColour = default);

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
        /// Sets the value of a uniform.
        /// </summary>
        /// <param name="uniform">The uniform to set.</param>
        internal void SetUniform<T>(IUniformWithValue<T> uniform) where T : struct, IEquatable<T>;

        /// <summary>
        /// Sets the current draw depth.
        /// The draw depth is written to every vertex added to <see cref="IGLVertexBuffer"/>s.
        /// </summary>
        /// <param name="drawDepth">The draw depth.</param>
        internal void SetDrawDepth(float drawDepth);

        internal IVertexBatch<TexturedVertex2D> DefaultQuadBatch { get; }

        internal void PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch);

        internal void PopQuadBatch();

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
