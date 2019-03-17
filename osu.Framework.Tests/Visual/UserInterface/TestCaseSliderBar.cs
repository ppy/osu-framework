// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestCaseSliderBar : ManualInputManagerTestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(BasicSliderBar<>), typeof(SliderBar<>) };

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly BindableDouble sliderBarValue; //keep a reference to avoid GC of the bindable
        private readonly SpriteText sliderBarText;
        private readonly SliderBar<double> sliderBar;
        private readonly SliderBar<double> transferOnCommitSliderBar;

        public TestCaseSliderBar()
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
                    sliderBar = new BasicSliderBar<double>
                    {
                        Size = new Vector2(200, 10),
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

        [Test]
        public void SliderBar()
        {
            AddStep("Click at 25% mark", () =>
            {
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.25f, 0.5f)));
                InputManager.Click(MouseButton.Left);
            });
            // We're translating to/from screen-space coordinates for click coordinates so we want to be more lenient with the value comparisons in these tests
            AddAssert("Value == -5", () => Precision.AlmostEquals(sliderBarValue.Value, -5, Precision.FLOAT_EPSILON));
            AddStep("Press left arrow key", () =>
            {
                var before = sliderBar.IsHovered;
                sliderBar.IsHovered = true;
                InputManager.PressKey(Key.Left);
                InputManager.ReleaseKey(Key.Left);
                sliderBar.IsHovered = before;
            });
            AddAssert("Value == -6", () => Precision.AlmostEquals(sliderBarValue.Value, -6, Precision.FLOAT_EPSILON));
            AddStep("Click at 75% mark, holding shift", () =>
            {
                InputManager.PressKey(Key.LShift);
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.75f, 0.5f)));
                InputManager.Click(MouseButton.Left);
                InputManager.ReleaseKey(Key.LShift);
            });
            AddAssert("Value == 5", () => Precision.AlmostEquals(sliderBarValue.Value, 5, Precision.FLOAT_EPSILON));
        }

        [Test]
        public void TransferValueOnCommit()
        {
            AddStep("Click at 80% mark", () =>
            {
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize * new Vector2(0.8f, 0.5f)));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("Value == 6", () => Precision.AlmostEquals(sliderBarValue.Value, 6, Precision.FLOAT_EPSILON));

            // These steps are broken up so we can see each of the steps being performed independently
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(transferOnCommitSliderBar.ToScreenSpace(transferOnCommitSliderBar.DrawSize * new Vector2(0.75f, 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag",
                () => { InputManager.MoveMouseTo(transferOnCommitSliderBar.ToScreenSpace(transferOnCommitSliderBar.DrawSize * new Vector2(0.25f, 0.5f))); });
            AddAssert("Value == 6 (still)", () => Precision.AlmostEquals(sliderBarValue.Value, 6, Precision.FLOAT_EPSILON));
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            AddAssert("Value == -5", () => Precision.AlmostEquals(sliderBarValue.Value, -5, Precision.FLOAT_EPSILON));
        }

        private void sliderBarValueChanged(ValueChangedEvent<double> args)
        {
            sliderBarText.Text = $"Value of Bindable: {args.NewValue:N}";
        }
    }
}
