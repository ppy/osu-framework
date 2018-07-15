﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class SelectionArea : CompositeDrawable
    {
        public SelectionArea(Color4 selectionColour)
        {
            Alpha = 0;
            Colour = selectionColour;

            InternalChild = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.White,
            };
        }

        /// <summary>
        /// Highlights a selected area.
        /// </summary>
        /// <param name="leftBound">Upper left corner of the selection</param>
        /// <param name="rightBound">Lower right corner of the selection</param>
        public void SelectArea(Vector2 leftBound, Vector2 rightBound)
        {
            var size = new Vector2(rightBound.X - leftBound.X, rightBound.Y - leftBound.Y);

            ClearTransforms();

            this.MoveTo(leftBound, 60)
                .ResizeWidthTo(size.X, 60)
                .FadeTo(0.5f, 200, Easing.Out);
        }
    }
}
