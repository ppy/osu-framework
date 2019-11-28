// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.OML.Attributes;

namespace osu.Framework.OML.Objects
{
    [OmlObject("box")]
    public class OmlBox : OmlObject
    {
        public OmlBox()
        {
            var box = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                FillMode = FillMode.Stretch,
            };

            Child = box;
        }
    }
}
