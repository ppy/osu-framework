// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing.Drawables.Steps;
using osuTK;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneStepButton : FrameworkTestScene
    {
        public TestSceneStepButton()
        {
            Child = new FillFlowContainer
            {
                Width = 150,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    new LabelStep { Text = nameof(LabelStep) },
                    new AssertButton { Text = nameof(AssertButton), Assertion = () => true },
                    new SingleStepButton { Text = nameof(SingleStepButton) },
                    new RepeatStepButton(null) { Text = nameof(RepeatStepButton) },
                    new ToggleStepButton(null) { Text = nameof(ToggleStepButton) },
                    new UntilStepButton(() => true) { Text = nameof(UntilStepButton) },
                    new StepSlider<int>(nameof(StepSlider<int>), 0, 10, 5),
                }
            };
        }
    }
}
