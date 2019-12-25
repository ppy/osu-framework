// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class ColorPicker : Container, IHasCurrentValue<Color4>
    {
        private readonly BindableWithCurrent<Color4> current = new BindableWithCurrent<Color4> { Default = Color4.White };

        public Bindable<Color4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        public Color4 BackgroundColour
        {
            get => Background.Colour;
            set => Background.FadeColour(value);
        }

        // Change current value will cause recursive change, so need a record to disable this change.
        private readonly Cached internalUpdate = new Cached();

        protected Box Background;
        protected FillFlowContainer FillFlowContainer;
        protected PickerAreaContainer PickerArea;
        protected HueSlideContainer HueSlider;
        protected TextBox ColorCodeTextBox;
        protected Box PreviewColorBox;

        public ColorPicker()
        {
            AutoSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Gray
                },
                FillFlowContainer = new FillFlowContainer
                {
                    Margin = new MarginPadding(10),
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 10),
                    Children = new Drawable[]
                    {
                        PickerArea = new PickerAreaContainer
                        {
                            Size = new Vector2(200),
                        },
                        HueSlider = new HueSlideContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 50,
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 40,
                            Padding = new MarginPadding(10),
                            Child = new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Content = new Drawable[][]
                                {
                                    new Drawable[]
                                    {
                                        ColorCodeTextBox = new HexTextBox
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            LengthLimit = 7
                                        },
                                        PreviewColorBox = new Box
                                        {
                                            RelativeSizeAxes = Axes.Both
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            HueSlider.Hue.BindTo(PickerArea.Hue);

            Current.BindValueChanged(value =>
            {
                var newColor = value.NewValue;

                // Update text and preview area
                ColorCodeTextBox.Text = newColor.ToHex();
                PreviewColorBox.Colour = newColor;

                // Prevent internal update cause recursive
                if (!internalUpdate.IsValid)
                    return;

                // Assigh canvas and scroller to change to current color
                Color4Extensions.ToHSV(newColor, out float h, out float s, out float v);
                HueSlider.Hue.Value = h;
                PickerArea.Saturation.Value = s;
                PickerArea.Value.Value = v;
            }, true);

            // If text changed is valid, change current color.
            ColorCodeTextBox.Current.BindValueChanged(value =>
            {
                if (value.NewValue.Replace("#", "").Length != 6)
                    return;

                Current.Value = Color4Extensions.FromHex(value.NewValue);
            });

            // Update scroll result
            PickerArea.Hue.BindValueChanged(_ => internalUpdate.Invalidate());
            PickerArea.Saturation.BindValueChanged(_ => internalUpdate.Invalidate());
            PickerArea.Value.BindValueChanged(_ => internalUpdate.Invalidate());
        }

        protected override void Update()
        {
            base.Update();

            if (!internalUpdate.IsValid)
                updateHsl();
        }

        private void updateHsl()
        {
            var h = PickerArea.Hue.Value;
            var s = PickerArea.Saturation.Value;
            var v = PickerArea.Value.Value;

            // Update current color
            var color = Color4Extensions.ToRGB(h, s, v);
            Current.Value = color;

            // Set to valid
            internalUpdate.Validate();
        }

        public class PickerAreaContainer : Container
        {
            public BindableFloat Hue { get; private set; } = new BindableFloat { Precision = 0.1f };

            public BindableFloat Saturation { get; private set; } = new BindableFloat { Precision = 0.001f };

            public BindableFloat Value { get; private set; } = new BindableFloat { Precision = 0.001f };

            private readonly Box whiteBackground;
            private readonly Box horizontalBackground;
            private readonly Box verticalBackground;
            private readonly Drawable picker;

            public PickerAreaContainer()
            {
                Children = new Drawable[]
                {
                    whiteBackground = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    horizontalBackground = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    verticalBackground = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    picker = new Circle
                    {
                        Size = new Vector2(10),
                        Colour = Color4.Red,
                        Origin = Anchor.Centre
                    }
                };

                // Re-calculate display color if HSV's hue changed.
                Hue.BindValueChanged(value =>
                {
                    var color = Color4Extensions.ToRGB(value.NewValue, 1, 1);
                    horizontalBackground.Colour = ColourInfo.GradientHorizontal(new Color4(), color);
                    verticalBackground.Colour = ColourInfo.GradientVertical(new Color4(), Color4.Black);
                }, true);

                // Update picker position
                Saturation.BindValueChanged(value => picker.X = value.NewValue * DrawWidth);
                Value.BindValueChanged(value => picker.Y = (1 - value.NewValue) * DrawHeight);
            }

            protected override bool OnClick(ClickEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            protected override bool OnDrag(DragEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            protected override bool OnDragEnd(DragEndEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            private void handleMouseInput(UIEvent e)
            {
                var position = ToLocalSpace(e.ScreenSpaceMousePosition);
                Saturation.Value = Math.Clamp(position.X / DrawWidth, 0, 1);
                Value.Value = Math.Clamp(1 - (position.Y / DrawHeight), 0, 1);
            }
        }

        public class HueSlideContainer : Container
        {
            public BindableFloat Hue { get; private set; } = new BindableFloat { Precision = 0.1f };

            private readonly GridContainer background;
            private readonly GradientPart[] colorParts;
            private readonly Drawable picker;

            public HueSlideContainer()
            {
                Padding = new MarginPadding { Bottom = 20 };
                Children = new Drawable[]
                {
                    background = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Content = new Drawable[][]
                        {
                            colorParts = new GradientPart[]
                            {
                                new GradientPart(Color4.Red, Color4.Magenta),
                                new GradientPart(Color4.Magenta, Color4.Blue),
                                new GradientPart(Color4.Blue, Color4.Aqua),
                                new GradientPart(Color4.Aqua, Color4.Lime),
                                new GradientPart(Color4.Lime, Color4.Yellow),
                                new GradientPart(Color4.Yellow, Color4.Red),
                            }
                        }
                    },
                    picker = new Triangle
                    {
                        Size = new Vector2(15),
                        Colour = Color4.Red,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.TopCentre
                    }
                };

                // Update picker position
                Hue.BindValueChanged(value => picker.X = value.NewValue / 360 * DrawWidth);
            }

            protected override bool OnClick(ClickEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            protected override bool OnDrag(DragEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            protected override bool OnDragEnd(DragEndEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            private void handleMouseInput(UIEvent e)
            {
                var xPosition = ToLocalSpace(e.ScreenSpaceMousePosition).X;
                var percentage = Math.Clamp(xPosition / DrawWidth, 0, 1);
                Hue.Value = percentage * 360;
            }

            private class GradientPart : Box
            {
                public GradientPart(Color4 start, Color4 end)
                {
                    RelativeSizeAxes = Axes.Both;
                    Colour = ColourInfo.GradientHorizontal(start, end);
                }
            }
        }

        public class HexTextBox : BasicTextBox
        {
            /// <summary>
            /// Only support Hex and start with `#`
            /// </summary>
            /// <param name="character">Characters should be filter</param>
            /// <returns></returns>
            protected override bool CanAddCharacter(char character) => (string.IsNullOrEmpty(Text) && character == '#') || Uri.IsHexDigit(character);
        }
    }
}
