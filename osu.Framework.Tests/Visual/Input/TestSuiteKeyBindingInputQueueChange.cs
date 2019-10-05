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
            AddStep("set children", () => { TestScene.SetChildren(); });

            AddStep("click-hold shown receptor", () =>
            {
                InputManager.MoveMouseTo(TestScene.ShownReceptor);
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("hide shown receptor", () => TestScene.ShownReceptor.Hide());
            AddStep("show hidden receptor", () => TestScene.HiddenReceptor.Show());
            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("shown pressed", () => TestScene.ShownReceptor.Pressed);
            AddAssert("shown released", () => TestScene.ShownReceptor.Released);
            AddAssert("hidden not pressed", () => !TestScene.HiddenReceptor.Pressed);
            AddAssert("hidden not released", () => !TestScene.HiddenReceptor.Released);
        }
    }
}
