// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneColorPicker : ManualInputManagerTestScene
    {
        private float DIVIATION = 0.01f;
        private int count = 0;

        protected ColorPicker.PickerAreaContainer PickerArea => colorPicker.PickerArea;
        protected ColorPicker.HueSlideContainer HueSlider => colorPicker.HueSlider;
        protected TextBox ColorCodeTextBox => colorPicker.ColorCodeTextBox;

        private readonly TestColorPicker colorPicker;
        private readonly Box previewColorBox;
        private readonly SpriteText counterText;

        private readonly GridContainer colorArea;

        public TestSceneColorPicker()
        {
            Add(new GridContainer
            {
                Width = 500,
                Height = 400,
                Content = new Drawable[][]
                {
                    new Drawable[]
                    {
                        colorPicker = new TestColorPicker(),
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            RowDimensions = new []
                            {
                                new Dimension( GridSizeMode.Absolute,100)
                            },
                            Content = new Drawable[][]
                            {
                                new Drawable[]
                                {
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding(10),
                                        Children = new Drawable[]
                                        {
                                            previewColorBox = new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                            },
                                            counterText = new SpriteText
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre
                                            }
                                        }
                                    }
                                },
                                new Drawable[]
                                {
                                    colorArea = new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Content = new Drawable[][]
                                        {
                                            new Drawable[]
                                            {
                                                new ClickableColor(colorPicker,Color4.Red),
                                                new ClickableColor(colorPicker,Color4.Blue),
                                                new ClickableColor(colorPicker,Color4.White),
                                            },
                                            new Drawable[]
                                            {
                                                new ClickableColor(colorPicker,Color4.Green),
                                                new ClickableColor(colorPicker,Color4.Yellow),
                                                new ClickableColor(colorPicker,Color4.Purple),
                                            },
                                            new Drawable[]
                                            {
                                                new ClickableColor(colorPicker,Color4.Gray),
                                                new ClickableColor(colorPicker,Color4.Orange),
                                                new ClickableColor(colorPicker,Color4.Aqua),
                                            },
                                            new Drawable[]
                                            {
                                                new ClickableColor(colorPicker,Color4.Fuchsia),
                                                new ClickableColor(colorPicker,Color4.PaleGoldenrod),
                                                new ClickableColor(colorPicker,Color4.DarkGray),
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            colorPicker.Current.BindValueChanged(value =>
            {
                count++;
                previewColorBox.Colour = value.NewValue;
                counterText.Text = $"{count} changes!";
            });
        }

        [SetUp]
        public override void SetUp()
        {
            colorPicker.Current.SetDefault();
            count = 0;
            counterText.Text = "Haven't change.";
        }

        [Test]
        public void TestClickColorPicker()
        {
            // Click picker center
            AddStep("Move Cursor to center",
               () => { InputManager.MoveMouseTo(PickerArea); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(new Color4(127, 63, 63, 255), 1);

            // Click picker left up
            AddStep("Move Cursor to left up",
               () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(DIVIATION, DIVIATION))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.White, 2);

            // Click picker right up
            AddStep("Move Cursor to right up",
               () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(PickerArea.DrawWidth - DIVIATION, DIVIATION))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Red, 3);

            // Click picker left bottom
            AddStep("Move Cursor to left bottom",
               () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(DIVIATION, PickerArea.DrawHeight - DIVIATION))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Black, 4);

            // Click picker right bottom
            AddStep("Move Cursor to right bottom",
               () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(PickerArea.DrawWidth - DIVIATION, PickerArea.DrawHeight - DIVIATION))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Black, 4);
        }

        [Test]
        public void TestClickHueSlider()
        {
            // Click picker
            AddStep("Move Cursor to picker area",
                () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(PickerArea.DrawWidth - DIVIATION, DIVIATION))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });

            // Click slider left
            AddStep("Move Cursor to slider left",
                () => { InputManager.MoveMouseTo(HueSlider.ToScreenSpace(new Vector2(DIVIATION, 10))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Red, 1);

            // Click slider center
            AddStep("Move Cursor to slider center",
                () => { InputManager.MoveMouseTo(HueSlider.ToScreenSpace(new Vector2(HueSlider.DrawWidth / 2, 10))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Aqua, 2);

            // Click slider right
            AddStep("Move Cursor to slider right",
                () => { InputManager.MoveMouseTo(HueSlider.ToScreenSpace(new Vector2(HueSlider.DrawWidth - DIVIATION, 10))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Red, 3);
        }

        [Test]
        public void TestEnterColorCode()
        {
            AddStep("Enter #FFFFFF",
               () => { ColorCodeTextBox.Text = "#FFFF00"; });
            checkValue(Color4.Yellow, 1);

            AddStep("Enter FF0000",
               () => { ColorCodeTextBox.Text = "FF0000"; });
            checkValue(Color4.Red, 2);

            AddStep("Enter invalid hex code",
               () => { ColorCodeTextBox.Text = "FFGGHH"; });
            checkValue(Color4.Red, 2);
        }

        [Test]
        public void TestAssighColor()
        {
            // Assign first color from right bottom
            AddStep("Assign first color",
               () => { InputManager.MoveMouseTo(colorArea.Content[0][0]); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Red, 1);

            // Assign second color from right bottom
            AddStep("Assign second color",
               () => { InputManager.MoveMouseTo(colorArea.Content[0][1]); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Blue, 2);

            // Assign third color from right bottom
            AddStep("Assign third color",
               () => { InputManager.MoveMouseTo(colorArea.Content[0][2]); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.White, 3);

            // Assign third color again, value should not be changed
            AddStep("Assign third color",
               () => { InputManager.MoveMouseTo(colorArea.Content[0][2]); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.White, 3);
        }

        private void checkValue(Color4 expectColor, int count)
        {
            AddAssert($"Color == {expectColor.ToString()}", () => expectColor == colorPicker.Current.Value);
            AddAssert($"Count == {count}", () => count == this.count);
        }

        public class ClickableColor : ClickableContainer
        {
            private readonly Box background;

            public ClickableColor(ColorPicker picker, Color4 color)
            {
                RelativeSizeAxes = Axes.Both;
                Padding = new MarginPadding(5);
                Child = background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = color
                };

                Action += () =>
                {
                    picker.Current.Value = background.Colour;
                };
            }
        }

        public class TestColorPicker : ColorPicker
        {
            public new PickerAreaContainer PickerArea => base.PickerArea;

            public new HueSlideContainer HueSlider => base.HueSlider;

            public new TextBox ColorCodeTextBox => base.ColorCodeTextBox;
        }
    }
}
