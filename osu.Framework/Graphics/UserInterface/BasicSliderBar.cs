// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
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

        private Color4 selectionColour = FrameworkColour.Yellow;

        public Color4 SelectionColour
        {
            get => selectionColour;
            set
            {
                selectionColour = value;
                updateColour();
            }
        }

        private Color4 focusColour = FrameworkColour.YellowGreen;

        public Color4 FocusColour
        {
            get => focusColour;
            set
            {
                focusColour = value;
                updateColour();
            }
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
                }
            };

            updateColour();
        }

        private void updateColour()
        {
            SelectionBox.Colour = HasFocus ? FocusColour : SelectionColour;
        }

        protected override void OnFocus(FocusEvent e)
        {
            updateColour();
            base.OnFocus(e);
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            updateColour();
            base.OnFocusLost(e);
        }

        protected override void UpdateValue(float value)
        {
            SelectionBox.ScaleTo(new Vector2(value, 1), 300, Easing.OutQuint);
        }
    }
}
