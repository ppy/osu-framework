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
                    Scene.CursorContainer.ActiveCursor.ScreenSpaceDrawQuad.Centre,
                    Scene.Container.ScreenSpaceDrawQuad.Centre);

            Scene.CreateContent();
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(Scene.Container.ScreenSpaceDrawQuad.Centre));
            AddAssert("cursor is centered", cursorCenteredInContainer);
            AddStep("Move container", () => Scene.Container.Y += 50);
            AddAssert("cursor is still centered", cursorCenteredInContainer);
            AddStep("Resize container", () => Scene.Container.Size *= new Vector2(1.4f, 1));
            AddAssert("cursor is still centered", cursorCenteredInContainer);

            // ensure positional updates work
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(Scene.Container.ScreenSpaceDrawQuad.Centre));
            AddAssert("cursor is still centered", cursorCenteredInContainer);

            // ensure we received the mouse position update from IRequireHighFrequencyMousePosition
            AddStep("Move cursor to test centre", () => InputManager.MoveMouseTo(Content.ScreenSpaceDrawQuad.Centre));
            AddStep("Recreate container with mouse already in place", Scene.CreateContent);
            AddAssert("cursor is centered", cursorCenteredInContainer);
        }
    }
}
