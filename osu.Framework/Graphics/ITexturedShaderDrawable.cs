// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics
{
    public interface ITexturedShaderDrawable : IDrawable
    {
        IShader TextureShader { get; }
        IShader RoundedTextureShader { get; }
    }
}
