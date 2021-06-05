// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicHSVColourPicker : HSVColourPicker
    {
        public BasicHSVColourPicker()
        {
            BackgroundColour = FrameworkColour.GreenDark;

            Content.Padding = new MarginPadding(20);
        }

        protected override HueSelector CreateHueSelector() => new BasicHueSelector();
        protected override SaturationValueSelector CreateSaturationValueSelector() => new BasicSaturationValueSelector();

        public class BasicHueSelector : HueSelector
        {
            protected override Drawable CreateSliderNub() => new BasicHueSelectorNub();
        }

        public class BasicHueSelectorNub : CompositeDrawable
        {
            public BasicHueSelectorNub()
            {
                InternalChild = new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = 8,
                    Height = 1.2f,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true
                    },
                    Masking = true,
                    BorderColour = FrameworkColour.YellowGreen,
                    BorderThickness = 4
                };
            }
        }

        public class BasicSaturationValueSelector : SaturationValueSelector
        {
        }
    }
}
