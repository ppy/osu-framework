// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Draws to the screen.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Creates a new <see cref="IFrameBuffer"/>.
        /// </summary>
        /// <param name="renderBufferFormats">Any render buffer formats.</param>
        /// <param name="filteringMode">The texture filtering mode.</param>
        /// <returns>The <see cref="IFrameBuffer"/>.</returns>
        IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear);
    }
}
