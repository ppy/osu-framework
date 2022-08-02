// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Draws to the screen.
    /// </summary>
    public interface IRenderer
    {
        public const int MAX_MIPMAP_LEVELS = 3;

        public const int VERTICES_PER_QUAD = 4;

        public const int VERTICES_PER_TRIANGLE = 4;

        /// <summary>
        /// The current horizontal texture wrap mode.
        /// </summary>
        WrapMode CurrentWrapModeS { get; }

        /// <summary>
        /// The current vertical texture wrap mode.
        /// </summary>
        WrapMode CurrentWrapModeT { get; }

        /// <summary>
        /// The texture for a white pixel.
        /// </summary>
        Texture WhitePixel { get; }

        /// <summary>
        /// Resets any states to prepare for drawing a new frame.
        /// </summary>
        /// <param name="windowSize">The full window size.</param>
        internal void BeginFrame(Vector2 windowSize);

        /// <summary>
        /// Binds a texture.
        /// </summary>
        /// <param name="texture">The texture to bind.</param>
        /// <param name="unit">The sampling unit in which the texture is to be bound.</param>
        /// <param name="wrapModeS">The texture's horizontal wrap mode.</param>
        /// <param name="wrapModeT">The texture's vertex wrap mode.</param>
        /// <returns>Whether <paramref name="texture"/> was newly-bound.</returns>
        bool BindTexture(Texture texture, int unit = 0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null);

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
