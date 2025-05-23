// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing.Drawables.Steps;
using osuTK;

namespace osu.Framework.Tests.Visual.Testing
{
    public partial class TestSceneStepButton : FrameworkTestScene
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
                    new LabelStep
                    {
                        Text = nameof(LabelStep),
                        IsSetupStep = false,
                        Action = _ => { },
                    },
                    new AssertButton
                    {
                        Text = nameof(AssertButton),
                        IsSetupStep = false,
                        Assertion = () => true,
                        CallStack = new StackTrace()
                    },
                    new SingleStepButton
                    {
                        Text = nameof(SingleStepButton),
                        IsSetupStep = false,
                        Action = () => { }
                    },
                    new RepeatStepButton
                    {
                        Text = nameof(RepeatStepButton),
                        IsSetupStep = false
                    },
                    new ToggleStepButton
                    {
                        Text = nameof(ToggleStepButton),
                        IsSetupStep = false,
                        Action = _ => { }
                    },
                    new UntilStepButton
                    {
                        Text = nameof(UntilStepButton),
                        IsSetupStep = false,
                        Assertion = () => true,
                        CallStack = new StackTrace()
                    },
                    new StepSlider<int>(nameof(StepSlider<int>), 0, 10, 5),
                }
            };
        }
    }
}
