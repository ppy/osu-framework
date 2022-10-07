// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
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
        private readonly TestSliderBarWithNub sliderBarWithNub;

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
                    new SpriteText
                    {
                        Text = "w/ Nub:",
                    },
                    sliderBarWithNub = new TestSliderBarWithNub
                    {
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
            checkValue(0);
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 0.0f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag",
                () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 1f))); });
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(0);
        }

        [Test]
        public void TestDragOutReleaseInHasNoEffect()
        {
            checkValue(0);
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 0.0f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag", () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 1.5f))); });
            AddStep("Drag Left", () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.25f, 1.5f))); });
            AddStep("Drag Up", () => { InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.25f, 0.5f))); });
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(0);
        }

        [Test]
        public void TestKeyboardInput()
        {
            AddStep("Press right arrow key", () =>
            {
                InputManager.PressKey(Key.Right);
                InputManager.ReleaseKey(Key.Right);
            });

            checkValue(0);

            AddStep("move mouse inside", () =>
            {
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.25f, 0.5f)));
            });

            AddStep("Press right arrow key", () =>
            {
                InputManager.PressKey(Key.Right);
                InputManager.ReleaseKey(Key.Right);
            });
            checkValue(1);
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
            checkValue(disabled ? 0 : -5);
            AddStep("Press left arrow key", () =>
            {
                bool before = sliderBar.IsHovered;
                sliderBar.IsHovered = true;
                InputManager.PressKey(Key.Left);
                InputManager.ReleaseKey(Key.Left);
                sliderBar.IsHovered = before;
            });
            checkValue(disabled ? 0 : -6);
            AddStep("Click at 75% mark, holding shift", () =>
            {
                InputManager.PressKey(Key.LShift);
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 0.5f)));
                InputManager.Click(MouseButton.Left);
                InputManager.ReleaseKey(Key.LShift);
            });
            checkValue(disabled ? 0 : 5);
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
            checkValue(disabled ? 0 : 6);

            // These steps are broken up so we can see each of the steps being performed independently
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(transferOnCommitSliderBar.ToScreenSpace(transferOnCommitSliderBar.DrawSize * new Vector2(0.75f, 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag",
                () => { InputManager.MoveMouseTo(transferOnCommitSliderBar.ToScreenSpace(transferOnCommitSliderBar.DrawSize * new Vector2(0.25f, 0.5f))); });
            checkValue(disabled ? 0 : 6);
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(disabled ? 0 : -5);
        }

        [Test]
        public void TestRevertValueOnDisabledDuringDrag()
        {
            checkValue(0);

            AddStep("Move Cursor", () => { InputManager.MoveMouseTo(transferOnCommitSliderBar.ToScreenSpace(transferOnCommitSliderBar.DrawSize * new Vector2(0.75f, 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag", () => { InputManager.MoveMouseTo(transferOnCommitSliderBar.ToScreenSpace(transferOnCommitSliderBar.DrawSize * new Vector2(0.25f, 0.5f))); });

            checkValue(0);

            AddStep("set disabled", () => transferOnCommitSliderBar.Current.Disabled = true);
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });

            checkValue(0);
        }

        [Test]
        public void TestAbsoluteDrag()
        {
            checkValue(0);
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(sliderBarWithNub.ToScreenSpace(sliderBarWithNub.DrawSize * new Vector2(0.1f, 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag",
                () => { InputManager.MoveMouseTo(sliderBarWithNub.ToScreenSpace(sliderBarWithNub.DrawSize * new Vector2(0.4f, 1f))); });
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(-2);
        }

        [Test]
        public void TestRelativeDrag()
        {
            checkValue(0);
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(sliderBarWithNub.ToScreenSpace(sliderBarWithNub.DrawSize * new Vector2(0.6f, 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag",
                () => { InputManager.MoveMouseTo(sliderBarWithNub.ToScreenSpace(sliderBarWithNub.DrawSize * new Vector2(0.75f, 1f))); });
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(3);
        }

        [Test]
        public void TestRelativeClick()
        {
            checkValue(0);
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(sliderBarWithNub.ToScreenSpace(sliderBarWithNub.DrawSize * new Vector2(0.6f, 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(0);
        }

        private void checkValue(int expected) =>
            AddAssert($"Value == {expected}", () => sliderBarValue.Value, () => Is.EqualTo(expected).Within(Precision.FLOAT_EPSILON));

        private void sliderBarValueChanged(ValueChangedEvent<double> args)
        {
            sliderBarText.Text = $"Value of Bindable: {args.NewValue:N}";
        }

        public class TestSliderBar : BasicSliderBar<double>
        {
        }

        public class TestSliderBarWithNub : BasicSliderBar<double>
        {
            private Box nub;

            [BackgroundDependencyLoader]
            private void load()
            {
                Add(nub = new Box
                {
                    Colour = Color4.Blue,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Y,
                    RelativePositionAxes = Axes.X,
                    Width = 80,
                });
            }

            protected override bool ShouldHandleAsRelativeDrag(MouseDownEvent e) => nub.ReceivePositionalInputAt(e.ScreenSpaceMouseDownPosition);

            protected override void UpdateValue(float value)
            {
                base.UpdateValue(value);
                nub.X = value;
            }
        }
    }
}
