// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using System;

namespace osu.Framework.Graphics.Visualisation
{
    internal class FlashyBox : Box
    {
        private Drawable target;
        private readonly Func<Drawable, Quad> getScreenSpaceQuad;

        public FlashyBox(Func<Drawable, Quad> getScreenSpaceQuad)
        {
            this.getScreenSpaceQuad = getScreenSpaceQuad;
        }

        public Drawable Target
        {
            set => target = value;
        }

        public override Quad ScreenSpaceDrawQuad => target == null ? new Quad() : getScreenSpaceQuad(target);
    }
}
