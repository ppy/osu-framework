// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(ScrollContainer<Drawable>),
            typeof(BasicScrollContainer),
            typeof(BasicScrollContainer<Drawable>)
        };

        private ScrollContainer<Drawable> scrollContainer;

        [SetUp]
        public void Setup() => Schedule(Clear);

        [TestCase(false)]
        [TestCase(true)]
        public void TestScrollTo(bool withClampExtension)
        {
            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = new BasicScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(100),
                    ClampExtension = withClampExtension ? 100 : 0,
                    Child = new Box { Size = new Vector2(100, 400) }
                });
            });

            scrollTo(-100);
            checkPosition(0);

            scrollTo(100);
            checkPosition(100);

            scrollTo(300);
            checkPosition(300);

            scrollTo(400);
            checkPosition(300);

            scrollTo(500);
            checkPosition(300);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestDraggingScroll(bool withClampExtension)
        {
            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = new BasicScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200),
                    ClampExtension = withClampExtension ? 100 : 0,
                    Child = new Box { Size = new Vector2(200, 400) }
                });
            });

            AddStep("Click and drag scrollcontainer", () =>
            {
                InputManager.MoveMouseTo(scrollContainer);
                InputManager.PressButton(MouseButton.Left);
                // Required for the dragging state to be set correctly.
                InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(10f)));
            });

            AddStep("Move mouse up", () => InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(0, -600f))));
            checkPosition(withClampExtension ? 300 : 200);
            AddStep("Move mouse down", () => InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(0, 600f))));
            checkPosition(withClampExtension ? -100 : 0);
            AddStep("Release mouse button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkPosition(0);
        }

        [Test]
        public void TestContentAnchors()
        {
            AddStep("Create scroll container with centre-left content", () =>
            {
                Add(scrollContainer = new BasicScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(300),
                    ScrollContent =
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    Child = new Box { Size = new Vector2(300, 400) }
                });
            });

            AddStep("Scroll to 0", () => scrollContainer.ScrollTo(0, false));
            AddAssert("Content position at top", () => Precision.AlmostEquals(scrollContainer.ScreenSpaceDrawQuad.TopLeft, scrollContainer.ScrollContent.ScreenSpaceDrawQuad.TopLeft));
        }

        private void scrollTo(float position)
        {
            float immediateScrollPosition = 0;

            AddStep($"scroll to {position}", () =>
            {
                scrollContainer.ScrollTo(position, false);
                immediateScrollPosition = position;
            });

            AddAssert($"immediately scrolled to {position}", () => Precision.AlmostEquals(position, immediateScrollPosition, 1));
        }

        private void checkPosition(float expected) => AddUntilStep($"position at {expected}", () => Precision.AlmostEquals(expected, scrollContainer.Current, 1));
    }
}
