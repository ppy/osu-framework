// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSuiteSliderBar : ManualInputManagerTestSuite<TestSceneSliderBar>
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(BasicSliderBar<>), typeof(SliderBar<>) };

        public TestSuiteSliderBar()
        {
            Scene.SliderBarValue.ValueChanged += sliderBarValueChanged;
        }

        [SetUp]
        public override void SetUp()
        {
            Scene.SliderBar.Current.Disabled = false;
            Scene.SliderBar.Current.Value = 0;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SliderBar(bool disabled)
        {
            AddStep($"set disabled to {disabled}", () => Scene.SliderBar.Current.Disabled = disabled);

            AddStep("Click at 25% mark", () =>
            {
                InputManager.MoveMouseTo(Scene.SliderBar.ToScreenSpace(Scene.SliderBar.DrawSize * new Vector2(0.25f, 0.5f)));
                InputManager.Click(MouseButton.Left);
            });
            // We're translating to/from screen-space coordinates for click coordinates so we want to be more lenient with the value comparisons in these tests
            checkValue(-5, disabled);
            AddStep("Press left arrow key", () =>
            {
                var before = Scene.SliderBar.IsHovered;
                Scene.SliderBar.IsHovered = true;
                InputManager.PressKey(Key.Left);
                InputManager.ReleaseKey(Key.Left);
                Scene.SliderBar.IsHovered = before;
            });
            checkValue(-6, disabled);
            AddStep("Click at 75% mark, holding shift", () =>
            {
                InputManager.PressKey(Key.LShift);
                InputManager.MoveMouseTo(Scene.SliderBar.ToScreenSpace(Scene.SliderBar.DrawSize * new Vector2(0.75f, 0.5f)));
                InputManager.Click(MouseButton.Left);
                InputManager.ReleaseKey(Key.LShift);
            });
            checkValue(5, disabled);
        }

        private void checkValue(int expected, bool disabled)
        {
            if (disabled)
                AddAssert("value unchanged (disabled)", () => Precision.AlmostEquals(Scene.SliderBarValue.Value, 0, Precision.FLOAT_EPSILON));
            else
                AddAssert($"Value == {expected}", () => Precision.AlmostEquals(Scene.SliderBarValue.Value, expected, Precision.FLOAT_EPSILON));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TransferValueOnCommit(bool disabled)
        {
            AddStep($"set disabled to {disabled}", () => Scene.SliderBar.Current.Disabled = disabled);

            AddStep("Click at 80% mark", () =>
            {
                InputManager.MoveMouseTo(Scene.SliderBar.ToScreenSpace(Scene.SliderBar.DrawSize * new Vector2(0.8f, 0.5f)));
                InputManager.Click(MouseButton.Left);
            });
            checkValue(6, disabled);

            // These steps are broken up so we can see each of the steps being performed independently
            AddStep("Move Cursor",
                () => { InputManager.MoveMouseTo(Scene.TransferOnCommitSliderBar.ToScreenSpace(Scene.TransferOnCommitSliderBar.DrawSize * new Vector2(0.75f, 0.5f))); });
            AddStep("Click", () => { InputManager.PressButton(MouseButton.Left); });
            AddStep("Drag",
                () => { InputManager.MoveMouseTo(Scene.TransferOnCommitSliderBar.ToScreenSpace(Scene.TransferOnCommitSliderBar.DrawSize * new Vector2(0.25f, 0.5f))); });
            checkValue(6, disabled);
            AddStep("Release Click", () => { InputManager.ReleaseButton(MouseButton.Left); });
            checkValue(-5, disabled);
        }

        private void sliderBarValueChanged(ValueChangedEvent<double> args)
        {
            Scene.SliderBarText.Text = $"Value of Bindable: {args.NewValue:N}";
        }
    }
}
