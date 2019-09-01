// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.MathUtils;
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

            void createContent()
            {
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
            }

            bool cursorCenteredInContainer() =>
                Precision.AlmostEquals(
                    cursorContainer.ActiveCursor.ScreenSpaceDrawQuad.Centre,
                    container.ScreenSpaceDrawQuad.Centre);

            createContent();
            Steps.AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(container.ScreenSpaceDrawQuad.Centre));
            Steps.AddAssert("cursor is centered", cursorCenteredInContainer);
            Steps.AddStep("Move container", () => container.Y += 50);
            Steps.AddAssert("cursor is still centered", cursorCenteredInContainer);
            Steps.AddStep("Resize container", () => container.Size *= new Vector2(1.4f, 1));
            Steps.AddAssert("cursor is still centered", cursorCenteredInContainer);

            // ensure positional updates work
            Steps.AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(container.ScreenSpaceDrawQuad.Centre));
            Steps.AddAssert("cursor is still centered", cursorCenteredInContainer);

            // ensure we received the mouse position update from IRequireHighFrequencyMousePosition
            Steps.AddStep("Move cursor to test centre", () => InputManager.MoveMouseTo(Content.ScreenSpaceDrawQuad.Centre));
            Steps.AddStep("Recreate container with mouse already in place", createContent);
            Steps.AddAssert("cursor is centered", cursorCenteredInContainer);
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
