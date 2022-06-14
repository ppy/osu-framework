// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicHSVColourPicker : HSVColourPicker
    {
        public BasicHSVColourPicker()
        {
            Background.Colour = FrameworkColour.GreenDark;

            Content.Padding = new MarginPadding(20);
            Content.Spacing = new Vector2(0, 10);
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
            protected override Marker CreateMarker() => new BasicMarker();

            private class BasicMarker : Marker
            {
                private readonly Box colourPreview;

                public BasicMarker()
                {
                    InternalChild = new Container
                    {
                        Size = new Vector2(15),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        BorderColour = FrameworkColour.YellowGreen,
                        BorderThickness = 4,
                        Child = colourPreview = new Box
                        {
                            RelativeSizeAxes = Axes.Both
                        }
                    };
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();

                    Current.BindValueChanged(_ => updatePreview(), true);
                }

                private void updatePreview() => colourPreview.Colour = Current.Value;
            }
        }
    }
}
