// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Testing;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSuiteKeyBindingInputQueueChange : ManualInputManagerTestSuite<TestSceneKeyBindingInputQueueChange>
    {
        [Test]
        public void TestReleaseDoesNotTriggerWithoutPress()
        {
            AddStep("set children", () => { Scene.SetChildren(); });

            AddStep("click-hold shown receptor", () =>
            {
                InputManager.MoveMouseTo(Scene.ShownReceptor);
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("hide shown receptor", () => Scene.ShownReceptor.Hide());
            AddStep("show hidden receptor", () => Scene.HiddenReceptor.Show());
            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("shown pressed", () => Scene.ShownReceptor.Pressed);
            AddAssert("shown released", () => Scene.ShownReceptor.Released);
            AddAssert("hidden not pressed", () => !Scene.HiddenReceptor.Pressed);
            AddAssert("hidden not released", () => !Scene.HiddenReceptor.Released);
        }
    }
}
