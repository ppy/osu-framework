// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSuiteFocusedOverlayContainer : ManualInputManagerTestSuite<TestSceneFocusedOverlayContainer>
    {
        [Test]
        public void TestClickDismiss()
        {
            AddStep("create container", () => { TestScene.CreateOverlayContainer(); });

            AddStep("show", () => TestScene.OverlayContainer.Show());
            AddAssert("has focus", () => TestScene.OverlayContainer.HasFocus);

            AddStep("click inside", () =>
            {
                InputManager.MoveMouseTo(TestScene.OverlayContainer.ScreenSpaceDrawQuad.Centre);
                InputManager.PressButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Left);
            });

            AddAssert("still has focus", () => TestScene.OverlayContainer.HasFocus);

            AddStep("click outside", () =>
            {
                InputManager.MoveMouseTo(TestScene.OverlayContainer.ScreenSpaceDrawQuad.TopLeft - new Vector2(20));
                InputManager.PressButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Left);
            });

            AddAssert("lost focus", () => !TestScene.OverlayContainer.HasFocus);
            AddAssert("not visible", () => TestScene.OverlayContainer.State.Value == Visibility.Hidden);
        }

        [Test]
        public void TestScrollBlocking()
        {
            AddStep("create container", () => { TestScene.CreateParentContainer(); });

            AddStep("show", () => TestScene.OverlayContainer.Show());

            AddAssert("has focus", () => TestScene.OverlayContainer.HasFocus);

            int initialScrollCount = 0;

            AddStep("scroll inside", () =>
            {
                initialScrollCount = TestScene.ParentContainer.ScrollReceived;
                InputManager.MoveMouseTo(TestScene.OverlayContainer.ScreenSpaceDrawQuad.Centre);
                InputManager.ScrollVerticalBy(1);
            });

            AddAssert("scroll not received by parent", () => TestScene.ParentContainer.ScrollReceived == initialScrollCount);

            AddStep("scroll outside", () =>
            {
                InputManager.MoveMouseTo(TestScene.OverlayContainer.ScreenSpaceDrawQuad.TopLeft - new Vector2(20));
                InputManager.ScrollVerticalBy(1);
            });

            AddAssert("scroll received by parent", () => TestScene.ParentContainer.ScrollReceived == ++initialScrollCount);
        }
    }
}
