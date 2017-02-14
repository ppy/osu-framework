// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using System;

namespace osu.Framework.Graphics.Visualisation
{
    class FlashyBox : Box
    {
        Drawable target;
        Func<Drawable, Quad> getScreenSpaceQuad;

        public FlashyBox(Func<Drawable, Quad> getScreenSpaceQuad)
        {
            this.getScreenSpaceQuad = getScreenSpaceQuad;
        }

        public Drawable Target { set { target = value; } }

        public override Quad ScreenSpaceDrawQuad => target == null ? new Quad() : getScreenSpaceQuad(target);
    }
}