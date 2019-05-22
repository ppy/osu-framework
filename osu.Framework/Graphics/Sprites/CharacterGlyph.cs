// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Contains the texture and associated spacing information for a Character
    /// </summary>
    public struct CharacterGlyph
    {
        /// <summary>
        /// The texture for this character
        /// </summary>
        public Texture Texture;

        /// <summary>
        /// The amount of space that should be given to the left of the character texture
        /// </summary>
        public float XOffset;

        /// <summary>
        /// The amount of space that should be given to the top of the character texture
        /// </summary>
        public float YOffset;

        /// <summary>
        /// The amount of space to advance the cursor by after drawing the texture
        /// </summary>
        public float XAdvance;
    }
}
