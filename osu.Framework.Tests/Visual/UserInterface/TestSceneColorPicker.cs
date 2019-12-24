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
        private int count = 0;

        private readonly ColorPicker colorPicker;
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
                        colorPicker = new ColorPicker(),
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
            count = 0;
            counterText.Text = "Haven't change.";
        }

        [Test]
        public void TestClickHueSlider()
        {
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(colorPicker.ToScreenSpace(colorPicker.DrawSize * new Vector2(0.75f, 0.0f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            checkValue(0, false);
        }

        [Test]
        public void TestClickColorPicker()
        {

        }

        [Test]
        public void TestSlideColorPicker()
        {

        }

        [Test]
        public void TestSlideHueSlider()
        {

        }

        [Test]
        public void TestAssighColor()
        {

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
                    RelativeSizeAxes= Axes.Both,
                    Colour = color
                };

                Action += () =>
                {
                    picker.Current.Value = background.Colour;
                };
            }
        }
    }
}
