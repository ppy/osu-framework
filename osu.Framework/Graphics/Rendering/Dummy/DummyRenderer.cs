// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    /// <summary>
    /// An <see cref="IRenderer"/> that does nothing. May be used for tests that don't have a visual output.
    /// </summary>
    public sealed class DummyRenderer : IRenderer
    {
        public WrapMode CurrentWrapModeS => WrapMode.None;
        public WrapMode CurrentWrapModeT => WrapMode.None;
        public Texture WhitePixel { get; } = new Texture(new DummyNativeTexture(), WrapMode.None, WrapMode.None);

        void IRenderer.BeginFrame(Vector2 windowSize)
        {
        }

        public bool BindTexture(Texture texture, int unit = 0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null)
            => true;

        IShaderPart IRenderer.CreateShaderPart(ShaderManager manager, string name, byte[]? rawData, ShaderPartType partType)
            => new DummyShaderPart();

        IShader IRenderer.CreateShader(string name, params IShaderPart[] parts)
            => new DummyShader();

        public IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new DummyFrameBuffer();

        public Texture CreateTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, WrapMode wrapModeS = WrapMode.None,
                                     WrapMode wrapModeT = WrapMode.None, Rgba32 initialisationColour = default)
            => new Texture(new DummyNativeTexture { Width = width, Height = height }, wrapModeS, wrapModeT);

        public Texture CreateVideoTexture(int width, int height)
            => CreateTexture(width, height);

        public IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new DummyVertexBatch<TVertex>();

        public IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new DummyVertexBatch<TVertex>();

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
