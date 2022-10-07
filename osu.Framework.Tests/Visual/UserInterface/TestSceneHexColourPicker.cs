// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneHexColourPicker : ManualInputManagerTestScene
    {
        private TestHexColourPicker hexColourPicker;
        private SpriteText currentText;
        private Box currentPreview;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create content", () =>
            {
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 10),
                    Children = new Drawable[]
                    {
                        hexColourPicker = new TestHexColourPicker(),
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(10, 0),
                            Children = new Drawable[]
                            {
                                currentText = new SpriteText(),
                                new Container
                                {
                                    Width = 50,
                                    RelativeSizeAxes = Axes.Y,
                                    Child = currentPreview = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both
                                    }
                                }
                            }
                        }
                    }
                };

                hexColourPicker.Current.BindValueChanged(colour =>
                {
                    currentText.Text = $"Current.Value = {colour.NewValue.ToHex()}";
                    currentPreview.Colour = colour.NewValue;
                }, true);
            });
        }

        [Test]
        public void TestExternalChange()
        {
            Colour4 colour = Colour4.Yellow;

            AddStep("set current colour", () => hexColourPicker.Current.Value = colour);

            AddAssert("hex code updated", () => hexColourPicker.HexCodeTextBox.Text == colour.ToHex());
            assertPreviewUpdated(colour);
        }

        [Test]
        public void TestTextBoxBehaviour()
        {
            clickTextBox();
            AddStep("insert valid colour", () => hexColourPicker.HexCodeTextBox.Text = "#ff00ff");
            assertPreviewUpdated(Colour4.Magenta);
            AddAssert("current not changed yet", () => hexColourPicker.Current.Value == Colour4.White);

            AddStep("commit text", () => InputManager.Key(Key.Enter));
            AddAssert("current updated", () => hexColourPicker.Current.Value == Colour4.Magenta);

            clickTextBox();
            AddStep("insert invalid colour", () => hexColourPicker.HexCodeTextBox.Text = "c0d0");
            AddStep("commit text", () => InputManager.Key(Key.Enter));
            AddAssert("current not changed", () => hexColourPicker.Current.Value == Colour4.Magenta);
            AddAssert("old hex code restored", () => hexColourPicker.HexCodeTextBox.Text == "#FF00FF");
        }

        private void clickTextBox()
            => AddStep("click text box", () =>
            {
                InputManager.MoveMouseTo(hexColourPicker.HexCodeTextBox);
                InputManager.Click(MouseButton.Left);
            });

        private void assertPreviewUpdated(Colour4 expected)
            => AddAssert("preview colour updated", () => hexColourPicker.Preview.Current.Value == expected);

        private class TestHexColourPicker : BasicHexColourPicker
        {
            public TextBox HexCodeTextBox => this.ChildrenOfType<TextBox>().Single();
            public ColourPreview Preview => this.ChildrenOfType<ColourPreview>().Single();
        }
    }
}
