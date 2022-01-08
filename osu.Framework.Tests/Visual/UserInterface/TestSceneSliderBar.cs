// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneSliderBar : ManualInputManagerTestScene
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly BindableDouble sliderBarValue; //keep a reference to avoid GC of the bindable
        private readonly SpriteText sliderBarText;
        private readonly TestSliderBar sliderBar;
        private readonly SliderBar<double> transferOnCommitSliderBar;

        public TestSceneSliderBar()
        {
            sliderBarValue = new BindableDouble
            {
                MinValue = -10,
                MaxValue = 10
            };
            sliderBarValue.ValueChanged += sliderBarValueChanged;

            Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(5),
                Spacing = new Vector2(5, 5),
                Children = new Drawable[]
                {
                    sliderBarText = new SpriteText
                    {
                        Text = $"Value of Bindable: {sliderBarValue.Value}",
                    },
                    new SpriteText
                    {
                        Text = "BasicSliderBar:",
                    },
                    sliderBar = new TestSliderBar
                    {
                        Size = new Vector2(200, 50),
                        BackgroundColour = Color4.White,
                        SelectionColour = Color4.Pink,
                        KeyboardStep = 1,
                        Current = sliderBarValue
                    },
                    new SpriteText
                    {
                        Text = "w/ RangePadding:",
                    },
                    new BasicSliderBar<double>
                    {
                        Size = new Vector2(200, 10),
                        RangePadding = 20,
                        BackgroundColour = Color4.White,
                        SelectionColour = Color4.Pink,
                        KeyboardStep = 1,
                        Current = sliderBarValue
                    },
                    new SpriteText
                    {
                        Text = "w/ TransferValueOnCommit:",
                    },
                    transferOnCommitSliderBar = new BasicSliderBar<double>
                    {
                        TransferValueOnCommit = true,
                        Size = new Vector2(200, 10),
                        BackgroundColour = Color4.White,
                        SelectionColour = Color4.Pink,
                        KeyboardStep = 1,
                        Current = sliderBarValue
                    },
                }
            });
        }

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
            sliderBar.Current.Disabled = false;
            sliderBar.Current.Value = 0;
        });

        [Test]
        public void TestVerticalDragHasNoEffect()
        {
            checkValue(0, false);
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 0.0f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag",
                () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 1f))); });
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(0, false);
        }

        [Test]
        public void TestDragOutReleaseInHasNoEffect()
        {
            checkValue(0, false);
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 0.0f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag", () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 1.5f))); });
            AddStep("Drag Left", () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.25f, 1.5f))); });
            AddStep("Drag Up", () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.25f, 0.5f))); });
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(0, false);
        }

        [Test]
        public void TestKeyboardInput()
        {
            AddStep("Press right arrow key", () =>
            {
                InputManager.PressKey(Key.Right);
                InputManager.ReleaseKey(Key.Right);
            });

            checkValue(0, true);

            AddStep("move mouse inside", () =>
            {
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.25f, 0.5f)));
            });

            AddStep("Press right arrow key", () =>
            {
                InputManager.PressKey(Key.Right);
                InputManager.ReleaseKey(Key.Right);
            });
            checkValue(1, false);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestAdjustmentPrecision(bool disabled)
        {
            AddStep($"set disabled to {disabled}", () => sliderBar.Current.Disabled = disabled);

            AddStep("Click at 25% mark", () =>
            {
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.25f, 0.5f)));
                InputManager.Click(MouseButton.Left);
            });
            // We're translating to/from screen-space coordinates for click coordinates so we want to be more lenient with the value comparisons in these tests
            checkValue(-5, disabled);
            AddStep("Press left arrow key", () =>
            {
                bool before = sliderBar.IsHovered;
                sliderBar.IsHovered = true;
                InputManager.PressKey(Key.Left);
                InputManager.ReleaseKey(Key.Left);
                sliderBar.IsHovered = before;
            });
            checkValue(-6, disabled);
            AddStep("Click at 75% mark, holding shift", () =>
            {
                InputManager.PressKey(Key.LShift);
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 0.5f)));
                InputManager.Click(MouseButton.Left);
                InputManager.ReleaseKey(Key.LShift);
            });
            checkValue(5, disabled);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestTransferValueOnCommit(bool disabled)
        {
            AddStep($"set disabled to {disabled}", () => sliderBar.Current.Disabled = disabled);

            AddStep("Click at 80% mark", () =>
            {
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.8f, 0.5f)));
                InputManager.Click(MouseButton.Left);
            });
            checkValue(6, disabled);

            // These steps are broken up so we can see each of the steps being performed independently
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(transferOnCommitSliderBar.ToScreenSpace(transferOnCommitSliderBar.DrawSize * new Vector2(0.75f, 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag",
                () => { InputManager.MoveMouseTo(transferOnCommitSliderBar.ToScreenSpace(transferOnCommitSliderBar.DrawSize * new Vector2(0.25f, 0.5f))); });
            checkValue(6, disabled);
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(-5, disabled);
        }

        private void checkValue(int expected, bool disabled)
        {
            if (disabled)
                AddAssert("value unchanged (disabled)", () => Precision.AlmostEquals(sliderBarValue.Value, 0, Precision.FLOAT_EPSILON));
            else
                AddAssert($"Value == {expected}", () => Precision.AlmostEquals(sliderBarValue.Value, expected, Precision.FLOAT_EPSILON));
        }

        private void sliderBarValueChanged(ValueChangedEvent<double> args)
        {
            sliderBarText.Text = $"Value of Bindable: {args.NewValue:N}";
        }

        public class TestSliderBar : BasicSliderBar<double>
        {
        }
    }
}
