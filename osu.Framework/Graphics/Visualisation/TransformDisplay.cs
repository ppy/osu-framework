// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Visualisation
{
    public class TransformDisplay : Container
    {
        public TransformDisplay()
        {
            RelativeSizeAxes = Axes.Both;
            Child = new SpriteText
            {
                Text = "This will be the transform display."
            };
        }
    }
}
