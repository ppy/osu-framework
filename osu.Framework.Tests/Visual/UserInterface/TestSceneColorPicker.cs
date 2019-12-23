// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
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
                            Height = 50,
                            Padding = new MarginPadding(10),
                            Child = new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Content = new Drawable[][]
                                {
                                    new Drawable[]
                                    {
                                        colorCodeTextBox = new TextBox
                                        {
                                            RelativeSizeAxes = Axes.Both
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

            current.BindValueChanged(value =>
            {
                // TODO : update text hex code
                previewColorBox.Colour = value.NewValue;
            });

            //Testing bindable
            colorScroller.Current.Value = Color4.Red;
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
            private readonly Box background;
            private readonly Box background2;
            private readonly Circle pickerStylus;

            public ColorCanvas()
            {
                Children = new Drawable[]
                {
                    whiteBackground = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    background2 = new Box
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
                    background.Colour = ColourInfo.GradientHorizontal(new Color4(0, 0, 0, 0), value.NewValue);
                    background2.Colour = ColourInfo.GradientVertical(new Color4(0, 0, 0, 0), Color4.Black);

                    updateToCurrent(pickerStylus.Position);
                });
            }

            protected override bool OnClick(ClickEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            protected override bool OnDrag(DragEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            private void handleMouseInput(UIEvent e)
            {
                var position = ToLocalSpace(e.ScreenSpaceMousePosition);
                pickerStylus.Position = position;

                //Update value
                updateToCurrent(position);
            }

            private void updateToCurrent(Vector2 position)
            {
                var percentage = new Vector2(position.X / Width, position.Y / Height);
                var targetColor = whiteBackground.Colour.Interpolate(percentage) +
                    background.Colour.Interpolate(percentage) + background2.Colour.Interpolate(percentage);

                Current.Value = targetColor;
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

            private readonly Box background;
            private readonly Triangle picker;

            public ColorScroller()
            {
                Padding = new MarginPadding { Bottom = 20 };
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
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

            protected override bool OnDrag(DragEvent e)
            {
                handleMouseInput(e);
                return true;
            }

            private void handleMouseInput(UIEvent e)
            {
                var xPosition = ToLocalSpace(e.ScreenSpaceMousePosition).X;
                picker.X = xPosition;
            }
        }
    }
}
