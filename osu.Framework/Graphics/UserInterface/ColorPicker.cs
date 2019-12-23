// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
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
        private readonly BindableWithCurrent<Color4> current = new BindableWithCurrent<Color4>();

        public Bindable<Color4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        public Color4 BackgroundColour
        {
            get => background.Colour;
            set => background.FadeColour(value);
        }

        private readonly Box background;
        private readonly FillFlowContainer fillFlowContainer;
        private readonly ColorCanvas colorCanvas;
        private readonly ColorScroller colorScroller;
        private readonly TextBox colorCodeTextBox;
        private readonly Box previewColorBox;

        public ColorPicker()
        {
            AutoSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Gray
                },
                fillFlowContainer = new FillFlowContainer
                {
                    Margin = new MarginPadding(10),
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 10),
                    Children = new Drawable[]
                    {
                        colorCanvas = new ColorCanvas
                        {
                            Size = new Vector2(200),
                        },
                        colorScroller = new ColorScroller
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
                                        colorCodeTextBox = new HexTextBox
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            LengthLimit = 7
                                        },
                                        previewColorBox = new Box
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

            colorScroller.BindableH.BindTo(colorCanvas.BindableH);

            Current.BindValueChanged(value =>
            {
                // Update text and preview area
                colorCodeTextBox.Text = value.NewValue.ToHex();
                previewColorBox.Colour = value.NewValue;

                // Assigh canvas and scroller to change to current color
                //Color4Extensions.RGB2HSL(value.NewValue, out double h, out double s, out double l);
                //colorScroller.BindableH.Value = h;
                //colorCanvas.BindableS.Value = s;
                //colorCanvas.BindableL.Value = l;
            }, true);

            // If text changed is valid, change current color.
            colorCodeTextBox.Current.BindValueChanged(value =>
            {
                if (value.NewValue.Replace("#", "").Length != 6)
                    return;

                Current.Value = Color4Extensions.FromHex(value.NewValue);
            });

            // Update scroll result
            colorCanvas.BindableH.BindValueChanged(_ => updateHsl());
            colorCanvas.BindableS.BindValueChanged(_ => updateHsl());
            colorCanvas.BindableL.BindValueChanged(_ => updateHsl());
        }

        private void updateHsl()
        {
            var h = colorCanvas.BindableH.Value;
            var s = colorCanvas.BindableS.Value;
            var l = colorCanvas.BindableL.Value;

            // Update current color
            var color = Color4Extensions.HSL2RGB(h, s, l);
            Current.Value = color;
        }

        public class ColorCanvas : Container
        {
            public BindableDouble BindableH { get; private set; } = new BindableDouble();

            public BindableDouble BindableS { get; private set; } = new BindableDouble();

            public BindableDouble BindableL { get; private set; } = new BindableDouble();

            private readonly Box whiteBackground;
            private readonly Box horizontalBackground;
            private readonly Box verticalBackground;
            private readonly Drawable picker;

            public ColorCanvas()
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

                BindableH.BindValueChanged(value =>
                {
                    // Calculate display color
                    var color = Color4Extensions.HSL2RGB(value.NewValue, 1, 0.5);
                    horizontalBackground.Colour = ColourInfo.GradientHorizontal(new Color4(), color);
                    verticalBackground.Colour = ColourInfo.GradientVertical(new Color4(), Color4.Black);
                });

                // Update picker position
                BindableS.BindValueChanged(value => picker.X = (float)value.NewValue * DrawWidth);
                BindableL.BindValueChanged(value => picker.Y = (float)(1 - value.NewValue) * DrawHeight);
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
                BindableS.Value = Math.Clamp(position.X / DrawWidth, 0, 1);
                BindableL.Value = Math.Clamp(1 - (position.Y / DrawHeight), 0, 1);
            }
        }

        public class ColorScroller : Container
        {
            public BindableDouble BindableH { get; private set; } = new BindableDouble();

            private readonly GridContainer background;
            private readonly GradientPart[] colorParts;
            private readonly Drawable picker;

            public ColorScroller()
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
                BindableH.BindValueChanged(value => picker.X = (float)value.NewValue * DrawWidth);
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
                BindableH.Value = percentage;
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
            protected override bool CanAddCharacter(char character) => (string.IsNullOrEmpty(Text) && character == '#') || Uri.IsHexDigit(character);
        }
    }
}
