// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
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
    public partial class TestSceneSwatchColourPicker : ManualInputManagerTestScene
    {
        private TestSwatchColourPicker colourPicker;
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
                        colourPicker = new TestSwatchColourPicker(),
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

                colourPicker.Current.BindValueChanged(colour =>
                {
                    currentText.Text = $"Current.Value = {colour.NewValue.ToHex()}";
                    currentPreview.Colour = colour.NewValue;
                }, true);
            });
        }

        [Test]
        public void TestHiddenWhenEmpty()
        {
            AddAssert("not present when empty", () => !colourPicker.IsPresent);

            AddStep("add colours", () => colourPicker.Colours.AddRange(new[]
            {
                Colour4.Red,
                Colour4.Green,
                Colour4.Blue
            }));
            AddAssert("present with colours", () => colourPicker.IsPresent);

            AddStep("clear colours", () => colourPicker.Colours.Clear());
            AddAssert("not present after clear", () => !colourPicker.IsPresent);
        }

        [Test]
        public void TestColoursCollectionChanges()
        {
            AddAssert("no swatches", () => !colourPicker.Swatches.Any());

            AddStep("add colours", () => colourPicker.Colours.AddRange(new[]
            {
                Colour4.Red,
                Colour4.Green
            }));
            AddAssert("two swatches", () => colourPicker.Swatches.Count() == 2);

            AddStep("add another colour", () => colourPicker.Colours.Add(Colour4.Blue));
            AddAssert("three swatches", () => colourPicker.Swatches.Count() == 3);

            AddStep("remove first colour", () => colourPicker.Colours.RemoveAt(0));
            AddAssert("two swatches again", () => colourPicker.Swatches.Count() == 2);

            AddStep("clear colours", () => colourPicker.Colours.Clear());
            AddAssert("no swatches again", () => !colourPicker.Swatches.Any());
        }

        [Test]
        public void TestSwatchClick()
        {
            AddStep("add colours", () => colourPicker.Colours.AddRange(new[]
            {
                Colour4.Red,
                Colour4.Green,
                Colour4.Blue
            }));
            AddAssert("current is default", () => colourPicker.Current.Value == default);

            AddStep("click first swatch", () =>
            {
                InputManager.MoveMouseTo(colourPicker.GetSwatch(0));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("current is red", () => colourPicker.Current.Value == Colour4.Red);

            AddStep("click third swatch", () =>
            {
                InputManager.MoveMouseTo(colourPicker.GetSwatch(2));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("current is blue", () => colourPicker.Current.Value == Colour4.Blue);
        }

        private partial class TestSwatchColourPicker : BasicSwatchColourPicker
        {
            public IEnumerable<ClickableContainer> Swatches => Content.Children.OfType<ClickableContainer>();

            public ClickableContainer GetSwatch(int index) => Swatches.ElementAt(index);
        }
    }
}
