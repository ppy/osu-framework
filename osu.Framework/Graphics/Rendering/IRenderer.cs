// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Draws to the screen.
    /// </summary>
    public interface IRenderer
    {
        Texture WhitePixel { get; }

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

        #region TextureVisualiser Support

        internal event Action<Texture> TextureCreated;

        internal Texture[] GetAllTextures();

        #endregion
    }
}
