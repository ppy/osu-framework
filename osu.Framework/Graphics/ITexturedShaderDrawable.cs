// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Interface for <see cref="Drawable"/>s which can be drawn by <see cref="TexturedShaderDrawNode"/>s.
    /// </summary>
    public interface ITexturedShaderDrawable : IDrawable
    {
        /// <summary>
        /// The <see cref="IShader"/> to be used for rendering.
        /// </summary>
        IShader? TextureShader { get; }
    }
}
