// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneScrollContainer : ManualInputManagerTestScene
    {
        private float clampExtension = 50;
        private ScrollContainer scrollContainer;

        public TestSceneScrollContainer()
        {
            AddSliderStep("Clamp extension", 0, 100, 50, c =>
            {
                if (scrollContainer != null)
                    scrollContainer.ClampExtension = c;

                clampExtension = c;
            });
        }

        /// <summary>
        /// Create a scroll container, attempt to scroll past its <see cref="ScrollContainer.ClampExtension"/>, and check that it does not.
        /// </summary>
        [Test]
        public void TestScrollTo()
        {
            AddStep("Create scroll container with specified clamp extension", () => createScrollContainer(clampExtension));
            AddStep("Scroll past extent", () => scrollContainer.ScrollTo(200));
            checkScrollWithinBounds();
            AddStep("Scroll past negative", () => scrollContainer.ScrollTo(-200));
            checkScrollWithinBounds();
        }

        /// <summary>
        /// Attempt to drag a scrollcontainer past its <see cref="ScrollContainer.ClampExtension"/> and check that it does not.
        /// </summary>
        [Test]
        public void TestDraggingScroll()
        {
            AddStep("Create scroll container with specified clamp extension", () => createScrollContainer(clampExtension));
            AddStep("Click and drag scrollcontainer", () =>
            {
                InputManager.MoveMouseTo(scrollContainer);
                InputManager.PressButton(MouseButton.Left);
                // Required for the dragging state to be set correctly.
                InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(10f)));
            });
            AddStep("Move mouse up", () => InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(-300f))));
            checkScrollWithinBounds();
            AddStep("Move mouse down", () => InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(300f))));
            checkScrollWithinBounds();
            AddStep("Release mouse button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkScrollWithinBounds();
        }

        [Test]
        public void TestContentAnchor()
        {
            AddStep("Create scroll container with centre-left content", () =>
            {
                createScrollContainer(clampExtension).With(d =>
                {
                    d.RelativeSizeAxes = Axes.None;
                    d.Size = new Vector2(300);
                    d.ScrollContent.Anchor = Anchor.CentreLeft;
                    d.ScrollContent.Origin = Anchor.CentreLeft;
                    d.ScrollContent.Child.Height = 400;
                });
            });

            AddStep("Scroll to 0", () => scrollContainer.ScrollTo(0, false));
            AddAssert("Content position at top", () => Precision.AlmostEquals(scrollContainer.ScreenSpaceDrawQuad.TopLeft, scrollContainer.ScrollContent.ScreenSpaceDrawQuad.TopLeft));
        }

        private void checkScrollWithinBounds()
        {
            AddAssert("Scroll amount is within ClampExtension bounds", () => Math.Abs(scrollContainer.Current) <= scrollContainer.ClampExtension);
        }

        private ScrollContainer createScrollContainer(float clampExtension = 0)
        {
            if (scrollContainer != null)
                InputManager.Remove(scrollContainer);

            InputManager.Add(scrollContainer = new ScrollContainer
            {
                ClampExtension = clampExtension,
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Height = 50,
                        RelativeSizeAxes = Axes.X
                    }
                }
            });

            return scrollContainer;
        }
    }
}
