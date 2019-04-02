// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL.Buffers;
using osuTK.Graphics;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Interface for <see cref="Drawable"/>s which can be drawn by a <see cref="BufferedDrawNode"/>.
    /// </summary>
    public interface IBufferedDrawable : ITexturedShaderDrawable
    {
        /// <summary>
        /// The background colour of the <see cref="FrameBuffer"/>s.
        /// Visually changes the colour which rendered alpha is blended against.
        /// </summary>
        /// <remarks>
        /// This should generally be transparent-black or transparent-white, but can also be used to
        /// colourise the background colour of the <see cref="FrameBuffer"/> with non-transparent colours.
        /// </remarks>
        Color4 BackgroundColour { get; }

        /// <summary>
        /// The colour with which the <see cref="FrameBuffer"/>s are rendered to the screen.
        /// A null value implies the <see cref="FrameBuffer"/>s should be drawn as they are.
        /// </summary>
        DrawColourInfo? FrameBufferDrawColour { get; }
    }
}
