// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneColorPicker : FrameworkTestScene
    {
        public TestSceneColorPicker()
        {
            var colorPicker = new ColorPicker();
            colorPicker.Current.Value = Color4.Red;
            Add(colorPicker);
        }
    }

    public class ColorPicker : Container, IHasCurrentValue<Color4>
    {
        private readonly BindableWithCurrent<Color4> current = new BindableWithCurrent<Color4>();

        public Bindable<Color4> Current
        {
            get => current.Current;
            set => current.Current = value;
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

            colorScroller.Current.BindTo(colorCanvas.Source);
            colorCanvas.Current.BindTo(current);

            // If text changed in valid, change current color
            colorCodeTextBox.Current.BindValueChanged(value =>
            {
                if (value.NewValue.Replace("#", "").Length != 6)
                    return;

                try
                {
                    Current.Value = Color4Extensions.FromHex(value.NewValue);
                }
                catch
                {
                    // Ignore here
                }
            });

            Current.BindValueChanged(value =>
            {
                //TODO : assigh canvas and scroller to change to current color
            });

            colorCanvas.Current.BindValueChanged(value =>
            {
                // TODO : update text hex code
                colorCodeTextBox.Text = value.NewValue.ToHex();
                previewColorBox.Colour = value.NewValue;
            });
        }

        public class ColorCanvas : Container, IHasCurrentValue<Color4>
        {
            private readonly BindableWithCurrent<Color4> source = new BindableWithCurrent<Color4>();

            public Bindable<Color4> Source
            {
                get => source.Current;
                set => source.Current = value;
            }

            private readonly BindableWithCurrent<Color4> current = new BindableWithCurrent<Color4>();

            public Bindable<Color4> Current
            {
                get => current.Current;
                set => current.Current = value;
            }

            private readonly Box whiteBackground;
            private readonly Box horizontalBackground;
            private readonly Box verticalBackground;
            private readonly Circle pickerStylus;

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
                    pickerStylus = new Circle
                    {
                        Size = new Vector2(10),
                        Colour = Color4.Red,
                        Origin = Anchor.Centre
                    }
                };

                source.BindValueChanged(value =>
                {
                    horizontalBackground.Colour = ColourInfo.GradientHorizontal(new Color4(), value.NewValue);
                    verticalBackground.Colour = ColourInfo.GradientVertical(new Color4(), Color4.Black);
                    updateToCurrent(pickerStylus.Position);
                });
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
                pickerStylus.Position = new Vector2(Math.Clamp(position.X, 0, DrawWidth), Math.Clamp(position.Y, 0, DrawHeight));

                //Update value
                updateToCurrent(pickerStylus.Position);
            }

            private void updateToCurrent(Vector2 position)
            {
                var percentage = new Vector2(position.X / DrawWidth, position.Y / DrawHeight);

                var horizontalColor = (Color4)ColourInfo.GradientHorizontal(Color4.White, source.Value).Interpolate(percentage);
                var alpha = 1 - percentage.Y;
                var mixedColor = horizontalColor.Multiply(alpha);

                Current.Value = mixedColor.Opacity(255);
            }
        }

        public class ColorScroller : Container, IHasCurrentValue<Color4>
        {
            private readonly BindableWithCurrent<Color4> current = new BindableWithCurrent<Color4>();

            public Bindable<Color4> Current
            {
                get => current.Current;
                set => current.Current = value;
            }

            private readonly GridContainer background;
            private readonly GradientPart[] colorParts;
            private readonly Triangle picker;

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
                picker.X = Math.Clamp(xPosition, 0, background.DrawWidth);

                //update value
                updateToCurrent(picker.X);
            }

            private void updateToCurrent(float position)
            {
                var index = Math.Clamp((int)(position / background.DrawWidth * 6), 0, colorParts.Length - 1);
                var percentage = position / background.DrawWidth * 6 - index;

                var color = colorParts[index].Colour.Interpolate(new Vector2(percentage));
                Current.Value = color;
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
