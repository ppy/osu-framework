// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class SelectionArea : CompositeDrawable
    {
        private readonly Color4 selectionColour;

        public SelectionArea(Color4 selectionColour)
        {
            this.selectionColour = selectionColour;

            Alpha = 0;
            Colour = Color4.Transparent;

            InternalChild = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.White,
            };
        }

        public void SelectArea(Vector2 leftBound, Vector2 rightBound)
        {
            var size = new Vector2(rightBound.X - leftBound.X, rightBound.Y - leftBound.Y);

            ClearTransforms();
            this.MoveTo(leftBound, 60)
                .ResizeWidthTo(size.X, 60)
                .FadeTo(0.5f, 200, Easing.Out)
                .FadeColour(selectionColour, 200, Easing.Out);
        }
    }
}
