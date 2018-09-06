// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Caching;
using OpenTK;

namespace osu.Framework.Tests.Layout.SpriteText
{
    public class TestSpriteText : Graphics.Sprites.SpriteText
    {
        public TestSpriteText()
        {
            Shadow = true;
        }

        public Cached CharactersCache => this.Get<Cached>("charactersCache");
        public Cached ScreenSpaceCharactersCache => this.Get<Cached>("screenSpaceCharactersCache");
        public Cached<Vector2> ShadowOffsetCache => this.Get<Cached<Vector2>>("shadowOffsetCache");
    }
}
