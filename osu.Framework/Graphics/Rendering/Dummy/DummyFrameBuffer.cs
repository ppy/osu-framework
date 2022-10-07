// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    /// <summary>
    /// An <see cref="IFrameBuffer"/> that does nothing. May be used for tests that don't have a visual output.
    /// </summary>
    internal class DummyFrameBuffer : IFrameBuffer
    {
        public Texture Texture { get; }

        public Vector2 Size
        {
            get => Texture.Size;
            set
            {
                Texture.Width = (int)Math.Ceiling(value.X);
                Texture.Height = (int)Math.Ceiling(value.Y);
            }
        }

        public DummyFrameBuffer(IRenderer renderer)
        {
            Texture = new Texture(new DummyNativeTexture(renderer), WrapMode.None, WrapMode.None);
        }

        public void Bind()
        {
        }

        public void Unbind()
        {
        }

        public void Dispose()
        {
        }
    }
}
