// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    /// <summary>
    /// An <see cref="IRenderer"/> that does nothing. May be used for tests that don't have a visual output.
    /// </summary>
    public sealed class DummyRenderer : IRenderer
    {
        public Texture WhitePixel => new Texture(1, 1);

        public IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new DummyFrameBuffer();
    }
}
