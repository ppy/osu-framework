// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneCursorContainer : ManualInputManagerTestScene
    {
        private Container container;
        private TestCursorContainer cursorContainer;

        [SetUp]
        public new void SetUp() => Schedule(createContent);

        [Test]
        public void TestPositionalUpdates()
        {
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(container.ScreenSpaceDrawQuad.Centre));
            AddAssert("Cursor is centered", () => cursorCenteredInContainer());
            AddAssert("Cursor at mouse position", () => cursorAtMouseScreenSpace());
        }

        [Test]
        public void TestPositionalUpdatesWhileHidden()
        {
            AddStep("Hide cursor container", () => cursorContainer.Alpha = 0f);
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(Content.ScreenSpaceDrawQuad.Centre));
            AddAssert("Cursor is centered", () => cursorCenteredInContainer());
            AddAssert("Cursor at mouse position", () => cursorAtMouseScreenSpace());
            AddStep("Show cursor container", () => cursorContainer.Alpha = 1f);
            AddAssert("Cursor is centered", () => cursorCenteredInContainer());
            AddAssert("Cursor at mouse position", () => cursorAtMouseScreenSpace());
        }

        [Test]
        public void TestChangeContainerDimensions()
        {
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(container.ScreenSpaceDrawQuad.Centre));
            AddStep("Move container", () => container.Y += 50);
            AddAssert("Cursor no longer centered", () => !cursorCenteredInContainer());
            AddAssert("Cursor at mouse position", () => cursorAtMouseScreenSpace());
            AddStep("Resize container", () => container.Size *= new Vector2(1.4f, 1));
            AddAssert("Cursor at mouse position", () => cursorAtMouseScreenSpace());
        }

        /// <summary>
        /// Ensures the mouse position still gets updated on content recreation via <see cref="IRequireHighFrequencyMousePosition"/>.
        /// </summary>
        [Test]
        public void TestRecreateContainer()
        {
            AddStep("Move cursor to centre", () => InputManager.MoveMouseTo(Content.ScreenSpaceDrawQuad.Centre));
            AddStep("Recreate container with mouse already in place", createContent);
            AddAssert("Cursor is centered", () => cursorCenteredInContainer());
            AddAssert("Cursor at mouse position", () => cursorAtMouseScreenSpace());
        }

        private bool cursorCenteredInContainer() =>
            Precision.AlmostEquals(
                cursorContainer.ActiveCursor.ScreenSpaceDrawQuad.Centre,
                container.ScreenSpaceDrawQuad.Centre);

        private bool cursorAtMouseScreenSpace() =>
            Precision.AlmostEquals(
                cursorContainer.ActiveCursor.ScreenSpaceDrawQuad.Centre,
                InputManager.CurrentState.Mouse.Position);

        private void createContent()
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
