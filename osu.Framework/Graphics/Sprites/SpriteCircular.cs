// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System.Diagnostics;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteCircular : Sprite
    {
        public override float CornerRadius
        {
            get { return Texture.DisplayWidth / 2f; }
            set { Debug.Assert(false, "Cannot set CornerRadius of SpriteCircular."); }
        }
    }
}
