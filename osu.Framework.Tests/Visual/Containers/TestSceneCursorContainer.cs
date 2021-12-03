// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneCursorContainer : ManualInputManagerTestScene
    {
        public TestSceneCursorContainer()
        {
            Container container;
            TestCursorContainer cursorContainer;

            void createContent() =>
                Child = container = new Container
                {
                    Masking = true,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(0.5f),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Color4.Yellow,
                            RelativeSizeAxes = Axes.Both,
                        },
                        cursorContainer = new TestCursorContainer
                        {
                            Name = "test",
                            RelativeSizeAxes = Axes.Both
                        }
                    }
                };

            bool cursorCenteredInContainer() =>
                Precision.AlmostEquals(
                    cursorContainer.ActiveCursor.ScreenSpaceDrawQuad.Centre,
                    container.ScreenSpaceDrawQuad.Centre);

            bool cursorAtMouseScreenSpace() =>
                Precision.AlmostEquals(
                    cursorContainer.ActiveCursor.ScreenSpaceDrawQuad.Centre,
                    InputManager.CurrentState.Mouse.Position);

            createContent();
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(container.ScreenSpaceDrawQuad.Centre));
            AddAssert("cursor is centered", () => cursorCenteredInContainer());
            AddStep("Move container", () => container.Y += 50);
            AddAssert("cursor no longer centered", () => !cursorCenteredInContainer());
            AddAssert("cursor at mouse position", () => cursorAtMouseScreenSpace());
            AddStep("Resize container", () => container.Size *= new Vector2(1.4f, 1));
            AddAssert("cursor at mouse position", () => cursorAtMouseScreenSpace());

            // ensure positional updates work
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(container.ScreenSpaceDrawQuad.Centre));
            AddAssert("cursor is not centered", () => cursorCenteredInContainer());
            AddAssert("cursor at mouse position", () => cursorAtMouseScreenSpace());

            // ensure we received the mouse position update from IRequireHighFrequencyMousePosition
            AddStep("Move cursor to test centre", () => InputManager.MoveMouseTo(Content.ScreenSpaceDrawQuad.Centre));
            AddStep("Recreate container with mouse already in place", createContent);
            AddAssert("cursor is centered", () => cursorCenteredInContainer());
            AddAssert("cursor at mouse position", () => cursorAtMouseScreenSpace());
        }

        private class TestCursorContainer : CursorContainer
        {
            protected override Drawable CreateCursor() => new Circle
            {
                Size = new Vector2(50),
                Colour = Color4.Red,
                Origin = Anchor.Centre,
            };
        }
    }
}
