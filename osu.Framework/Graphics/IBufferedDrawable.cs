// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Rendering;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Interface for <see cref="Drawable"/>s which can be drawn by a <see cref="BufferedDrawNode"/>.
    /// </summary>
    public interface IBufferedDrawable : ITexturedShaderDrawable
    {
        /// <summary>
        /// The background colour of the <see cref="IFrameBuffer"/>s.
        /// Visually changes the colour which rendered alpha is blended against.
        /// </summary>
        /// <remarks>
        /// This should generally be transparent-black or transparent-white, but can also be used to
        /// colourise the background colour of the <see cref="IFrameBuffer"/> with non-transparent colours.
        /// </remarks>
        Color4 BackgroundColour { get; }

        /// <summary>
        /// The colour with which the <see cref="IFrameBuffer"/>s are rendered to the screen.
        /// A null value implies the <see cref="IFrameBuffer"/>s should be drawn as they are.
        /// </summary>
        DrawColourInfo? FrameBufferDrawColour { get; }

        /// <summary>
        /// The scale of the <see cref="IFrameBuffer"/>s drawn relative to the size of this <see cref="IBufferedDrawable"/>.
        /// </summary>
        /// <remarks>
        /// The contents of the <see cref="IFrameBuffer"/>s are populated at this scale, however the scale of <see cref="Drawable"/>s remains unaffected.
        /// </remarks>
        Vector2 FrameBufferScale { get; }
    }
}
