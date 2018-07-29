// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Extensions.Color4Extensions;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicSliderBar<T> : SliderBar<T>
        where T : struct, IComparable, IConvertible
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
            CornerRadius = 4;
            Masking = true;

            Children = new Drawable[]
            {
                Box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.MediumPurple.Darken(0.5f),
                },
                SelectionBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.MediumPurple.Lighten(0.1f),
                }
            };
        }

        protected override void UpdateValue(float value)
        {
            SelectionBox.ScaleTo(new Vector2(value, 1), 300, Easing.OutQuint);
        }
    }
}
