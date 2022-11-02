// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Textures
{
    public enum WrapMode
    {
        /// <summary>
        /// No wrapping. If the texture is part of an atlas, this may read outside the texture's bounds.
        /// </summary>
        None = 0,

        /// <summary>
        /// Clamps to the edge of the texture, repeating the edge to fill the remainder of the draw area.
        /// </summary>
        ClampToEdge = 1,

        /// <summary>
        /// Clamps to a transparent-black border around the texture, repeating the border to fill the remainder of the draw area.
        /// </summary>
        ClampToBorder = 2,

        /// <summary>
        /// Repeats the texture.
        /// </summary>
        Repeat = 3,
    }
}
