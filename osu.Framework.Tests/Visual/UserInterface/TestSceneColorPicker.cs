// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneColorPicker : ManualInputManagerTestScene
    {
        private const float deviation = 0.01f;

        private int count;

        protected ColorPicker.PickerAreaContainer PickerArea => colorPicker.PickerArea;
        protected ColorPicker.HueSlideContainer HueSlider => colorPicker.HueSlider;
        protected TextBox ColorCodeTextBox => colorPicker.ColorCodeTextBox;

        private readonly TestColorPicker colorPicker;
        private readonly SpriteText counterText;

        private readonly GridContainer colorArea;

        public TestSceneColorPicker()
        {
            Box previewColorBox;
            Add(new GridContainer
            {
                Width = 520,
                Height = 400,
                Content = new[]
                {
                    new Drawable[]
                    {
                        colorPicker = new TestColorPicker(),
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(10),
                            Children = new Drawable[]
                            {
                                counterText = new SpriteText
                                {
                                    RelativeSizeAxes = Axes.X,
                                },
                                previewColorBox = new Box
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 50
                                },
                                new SpriteText
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Text = "Click below colors to change color picker's current color."
                                },
                                colorArea = new GridContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 300,
                                    Content = new[]
                                    {
                                        new Drawable[]
                                        {
                                            new ClickableColor(colorPicker, Color4.Red),
                                            new ClickableColor(colorPicker, Color4.Blue),
                                            new ClickableColor(colorPicker, Color4.White),
                                        },
                                        new Drawable[]
                                        {
                                            new ClickableColor(colorPicker, Color4.Green),
                                            new ClickableColor(colorPicker, Color4.Yellow),
                                            new ClickableColor(colorPicker, Color4.Purple),
                                        },
                                        new Drawable[]
                                        {
                                            new ClickableColor(colorPicker, Color4.Gray),
                                            new ClickableColor(colorPicker, Color4.Orange),
                                            new ClickableColor(colorPicker, Color4.Aqua),
                                        },
                                        new Drawable[]
                                        {
                                            new ClickableColor(colorPicker, Color4.Fuchsia),
                                            new ClickableColor(colorPicker, Color4.PaleGoldenrod),
                                            new ClickableColor(colorPicker, Color4.DarkGray),
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
            counterText.Text = $"{count} changes!";
        }

        [Test]
        public void TestClickColorPicker()
        {
            AddStep("Move Cursor to center",
                () => { InputManager.MoveMouseTo(PickerArea); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(new Color4(127, 63, 63, 255), 1);

            AddStep("Move Cursor to left up",
                () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(deviation))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.White, 2);

            AddStep("Move Cursor to right up",
                () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(PickerArea.DrawWidth - deviation, deviation))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Red, 3);

            AddStep("Move Cursor to left bottom",
                () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(deviation, PickerArea.DrawHeight - deviation))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Black, 4);

            AddStep("Move Cursor to right bottom",
                () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(PickerArea.DrawWidth - deviation, PickerArea.DrawHeight - deviation))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Black, 4);

            // Click again, value should not be change.
            AddStep("Move Cursor to right bottom again",
                () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(PickerArea.DrawWidth - deviation, PickerArea.DrawHeight - deviation))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Black, 4);
        }

        [Test]
        public void TestClickHueSlider()
        {
            AddStep("Move Cursor to picker area",
                () => { InputManager.MoveMouseTo(PickerArea.ToScreenSpace(new Vector2(PickerArea.DrawWidth - deviation, deviation))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });

            AddStep("Move Cursor to slider left",
                () => { InputManager.MoveMouseTo(HueSlider.ToScreenSpace(new Vector2(deviation, 10))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Red, 1);

            AddStep("Move Cursor to slider center",
                () => { InputManager.MoveMouseTo(HueSlider.ToScreenSpace(new Vector2(HueSlider.DrawWidth / 2, 10))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Aqua, 2);

            AddStep("Move Cursor to slider right",
                () => { InputManager.MoveMouseTo(HueSlider.ToScreenSpace(new Vector2(HueSlider.DrawWidth - deviation, 10))); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Red, 3);

            // Click again, value should not be change.
            AddStep("Move Cursor to slider right again",
                () => { InputManager.MoveMouseTo(HueSlider.ToScreenSpace(new Vector2(HueSlider.DrawWidth - deviation, 10))); });
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
            AddStep("Assign first color",
                () => { InputManager.MoveMouseTo(colorArea.Content[0][0]); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Red, 1);

            AddStep("Assign second color",
                () => { InputManager.MoveMouseTo(colorArea.Content[0][1]); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.Blue, 2);

            AddStep("Assign third color",
                () => { InputManager.MoveMouseTo(colorArea.Content[0][2]); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.White, 3);

            // Assign third color again, value should not be changed
            AddStep("Assign third color again",
                () => { InputManager.MoveMouseTo(colorArea.Content[0][2]); });
            AddStep("Click", () => { InputManager.Click(MouseButton.Left); });
            checkValue(Color4.White, 3);
        }

        private void checkValue(Color4 expectColor, int count)
        {
            AddAssert($"Color == {expectColor.ToString()}", () => Precision.AlmostEquals(expectColor, colorPicker.Current.Value, 0.005f));
            AddAssert($"Count == {count}", () => count == this.count);
        }

        public class ClickableColor : ClickableContainer
        {
            public ClickableColor(ColorPicker picker, Color4 color)
            {
                Box background;
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
