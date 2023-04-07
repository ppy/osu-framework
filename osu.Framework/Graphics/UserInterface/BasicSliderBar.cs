// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using Vector2 = osuTK.Vector2;

namespace osu.Framework.Graphics.UserInterface
{
    public partial class BasicSliderBar<T> : SliderBar<T>
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        public Color4 BackgroundColour
        {
            get => Box.Colour;
            set => Box.Colour = value;
        }

        public Color4 SelectionColour
        {
            get => SelectionBox.Colour;
            set => SelectionBox.Colour = value;
        }

        protected readonly Box SelectionBox;
        protected readonly Box Box;

        public BasicSliderBar()
        {
            Children = new Drawable[]
            {
                Box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = FrameworkColour.Green,
                },
                SelectionBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = FrameworkColour.Yellow,
                }
            };
        }

        protected override void UpdateValue(float value)
        {
            SelectionBox.ScaleTo(new Vector2(value, 1), 300, Easing.OutQuint);
        }
    }
}
