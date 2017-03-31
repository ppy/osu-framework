// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicSliderBar<T> : SliderBar<T> where T : struct
    {
        public Color4 Color
        {
            get { return Box.Colour; }
            set { Box.Colour = value; }
        }

        public Color4 SelectionColor
        {
            get { return SelectionBox.Colour; }
            set { SelectionBox.Colour = value; }
        }

        protected readonly Box SelectionBox;
        protected readonly Box Box;

        public BasicSliderBar()
        {
            Children = new Drawable[]
            {
                Box = new Box { RelativeSizeAxes = Axes.Both },
                SelectionBox = new Box { RelativeSizeAxes = Axes.Both }
            };
        }

        protected override void UpdateValue(float value)
        {
            SelectionBox.ScaleTo(new Vector2(value, 1), 300, EasingTypes.OutQuint);
        }
    }
}
