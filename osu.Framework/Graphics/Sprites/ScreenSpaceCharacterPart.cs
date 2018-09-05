// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A character of a <see cref="SpriteText"/> provided with screen space draw coordinates.
    /// </summary>
    internal struct ScreenSpaceCharacterPart
    {
        /// <summary>
        /// The screen-space quad for the character to be drawn in.
        /// </summary>
        public Quad DrawQuad;

        /// <summary>
        /// The texture to draw the character with.
        /// </summary>
        public Texture Texture;
    }
}