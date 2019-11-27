// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.OML.Attributes;

namespace osu.Framework.OML.Objects
{
    [OmlObject(Aliases = new []{ "circle" })]
    public class OmlCircle : OmlObject
    {
        public OmlCircle()
        {
            var circle = new Circle
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FillMode = FillMode.Stretch,
            };

            Child = circle;
        }
    }
}
