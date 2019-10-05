// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSuiteCursorContainer : ManualInputManagerTestSuite<TestSceneCursorContainer>
    {
        public TestSuiteCursorContainer()
        {
            bool cursorCenteredInContainer() =>
                Precision.AlmostEquals(
                    TestScene.CursorContainer.ActiveCursor.ScreenSpaceDrawQuad.Centre,
                    TestScene.Container.ScreenSpaceDrawQuad.Centre);

            TestScene.CreateContent();
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(TestScene.Container.ScreenSpaceDrawQuad.Centre));
            AddAssert("cursor is centered", cursorCenteredInContainer);
            AddStep("Move container", () => TestScene.Container.Y += 50);
            AddAssert("cursor is still centered", cursorCenteredInContainer);
            AddStep("Resize container", () => TestScene.Container.Size *= new Vector2(1.4f, 1));
            AddAssert("cursor is still centered", cursorCenteredInContainer);

            // ensure positional updates work
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(TestScene.Container.ScreenSpaceDrawQuad.Centre));
            AddAssert("cursor is still centered", cursorCenteredInContainer);

            // ensure we received the mouse position update from IRequireHighFrequencyMousePosition
            AddStep("Move cursor to test centre", () => InputManager.MoveMouseTo(Content.ScreenSpaceDrawQuad.Centre));
            AddStep("Recreate container with mouse already in place", TestScene.CreateContent);
            AddAssert("cursor is centered", cursorCenteredInContainer);
        }
    }
}
