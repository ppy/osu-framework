// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.OML
{
    public sealed class OmlDisplayContainer : Container
    {
        public OmlDisplayContainer(IOmlParser parser)
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            FillMode = FillMode.Stretch;

            Add(parser.ConstructContainer());
        }
    }
}
