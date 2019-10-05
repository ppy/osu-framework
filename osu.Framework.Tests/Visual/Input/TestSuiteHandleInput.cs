// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSuiteHandleInput : ManualInputManagerTestSuite<TestSceneHandleInput>
    {
        public TestSuiteHandleInput()
        {
            AddStep($"enable {Scene.TestNotHandleInput}", () => { InputManager.MoveMouseTo(Scene.TestNotHandleInput); });
            AddAssert($"check {nameof(Scene.TestNotHandleInput)}", () => !Scene.TestNotHandleInput.IsHovered && !Scene.TestNotHandleInput.HasFocus);

            AddStep($"enable {nameof(Scene.TestHandlePositionalInput)}", () =>
            {
                Scene.TestHandlePositionalInput.Enabled = true;
                InputManager.MoveMouseTo(Scene.TestHandlePositionalInput);
            });
            AddAssert($"check {nameof(Scene.TestHandlePositionalInput)}", () => Scene.TestHandlePositionalInput.IsHovered && Scene.TestHandlePositionalInput.HasFocus);

            AddStep($"enable {nameof(Scene.TestHandleNonPositionalInput)}", () =>
            {
                Scene.TestHandleNonPositionalInput.Enabled = true;
                InputManager.MoveMouseTo(Scene.TestHandleNonPositionalInput);
                InputManager.TriggerFocusContention(null);
            });
            AddAssert($"check {nameof(Scene.TestHandleNonPositionalInput)}", () => !Scene.TestHandleNonPositionalInput.IsHovered && Scene.TestHandleNonPositionalInput.HasFocus);

            AddStep("move mouse", () => InputManager.MoveMouseTo(Scene.TestHandlePositionalInput));
            AddStep("disable all", () =>
            {
                Scene.TestHandlePositionalInput.Enabled = false;
                Scene.TestHandleNonPositionalInput.Enabled = false;
            });
            AddAssert($"check {nameof(Scene.TestHandlePositionalInput)}", () => !Scene.TestHandlePositionalInput.IsHovered);
            // focus is not released when AcceptsFocus become false while focused
            //AddAssert($"check {nameof(handleNonPositionalInput)}", () => !handleNonPositionalInput.HasFocus);
        }
    }
}
