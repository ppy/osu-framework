// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Sprites
{
    internal struct CharacterPart
    {
        public RectangleF DrawRectangle;
        public Texture Texture;
    }

    internal struct ScreenSpaceCharacterPart
    {
        public Quad DrawQuad;
        public Texture Texture;
    }
}
