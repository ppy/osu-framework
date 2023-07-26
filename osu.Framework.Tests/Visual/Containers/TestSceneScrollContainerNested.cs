// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Containers
{
    public partial class TestSceneScrollContainerNested : ManualInputManagerTestScene
    {
        private BasicScrollContainer horizontalScroll = null!;
        private BasicScrollContainer verticalScroll = null!;
        private BasicScrollContainer outerScroll = null!;
        private BasicScrollContainer innerScroll = null!;

        private static bool checkScrollCurrent(BasicScrollContainer scrolled, BasicScrollContainer notScrolled) => notScrolled.Current == 0 && Precision.DefinitelyBigger(scrolled.Current, 0f);

        private void setup(Direction outer, Direction inner)
        {
            AddStep("create scroll containers", () =>
            {
                Child = outerScroll = new BasicScrollContainer(outer)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(500),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Size = new Vector2(500f),
                            Colour = FrameworkColour.Yellow,
                        },
                        innerScroll = new BasicScrollContainer(inner)
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(250, 250),
                            Child = new Box
                            {
                                Size = new Vector2(250, 250),
                                Colour = FrameworkColour.Green,
                            },
                        }
                    }
                };

                horizontalScroll = outer == Direction.Horizontal ? outerScroll : innerScroll;
                verticalScroll = outer == Direction.Vertical ? outerScroll : innerScroll;
            });

            AddStep("move mouse to inner", () => InputManager.MoveMouseTo(innerScroll));
        }

        [TestCase(Direction.Horizontal, Direction.Vertical)]
        [TestCase(Direction.Vertical, Direction.Horizontal)]
        public void TestDragging(Direction outer, Direction inner)
        {
            setup(outer, inner);

            AddStep("drag vertically", () =>
            {
                InputManager.MoveMouseTo(verticalScroll);
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(verticalScroll, new Vector2(0, -50));
            });
            AddAssert("vertical container scrolled only", () => checkScrollCurrent(verticalScroll, horizontalScroll));
            AddStep("reset vertical scroll", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                verticalScroll.ScrollToStart(false, true);
            });

            AddStep("drag horizontally", () =>
            {
                InputManager.MoveMouseTo(horizontalScroll);
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(horizontalScroll, new Vector2(-50, 0));
            });
            AddAssert("horizontal container scrolled only", () => checkScrollCurrent(horizontalScroll, verticalScroll));
            AddStep("reset horizontal scroll", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                horizontalScroll.ScrollToStart(false, true);
            });
        }

        [TestCase(Direction.Horizontal, Direction.Vertical, false)]
        [TestCase(Direction.Horizontal, Direction.Vertical, true)]
        [TestCase(Direction.Vertical, Direction.Horizontal, false)]
        [TestCase(Direction.Vertical, Direction.Horizontal, true)]
        public void TestNonPreciseScrolling(Direction outer, Direction inner, bool holdShift)
        {
            setup(outer, inner);

            if (holdShift)
                AddStep("press shift", () => InputManager.PressKey(Key.ShiftLeft));

            AddStep("scroll vertically", () => InputManager.ScrollVerticalBy(-50));

            if (holdShift)
            {
                // holding shift should behave the same as a regular horizontal scroll
                AddAssert("horizontal container scrolled only", () => checkScrollCurrent(horizontalScroll, verticalScroll));
                AddStep("reset horizontal scroll", () => horizontalScroll.ScrollToStart(false));
            }
            else
            {
                // since most non-precise scroll wheels only have a vertical axis, assume the user always wants to scroll
                // the inner-most container.
                AddAssert("inner container scrolled only", () => checkScrollCurrent(innerScroll, outerScroll));
                AddStep("reset inner scroll", () => innerScroll.ScrollToStart(false));
            }

            AddStep("scroll horizontally", () => InputManager.ScrollHorizontalBy(-50));
            AddAssert("horizontal container scrolled only", () => checkScrollCurrent(horizontalScroll, verticalScroll));
            AddStep("reset horizontal scroll", () => horizontalScroll.ScrollToStart(false));

            if (holdShift)
                AddStep("release shift", () => InputManager.ReleaseKey(Key.ShiftLeft));
        }

        [TestCase(Direction.Horizontal, Direction.Vertical, false)]
        [TestCase(Direction.Horizontal, Direction.Vertical, true)]
        [TestCase(Direction.Vertical, Direction.Horizontal, false)]
        [TestCase(Direction.Vertical, Direction.Horizontal, true)]
        public void TestPreciseScrolling(Direction outer, Direction inner, bool holdShift)
        {
            setup(outer, inner);

            if (holdShift)
                // holding shift shouldn't affect precise scrolling
                AddStep("press shift", () => InputManager.PressKey(Key.ShiftLeft));

            AddStep("scroll vertically", () => InputManager.ScrollVerticalBy(-50, true));
            // this could be improved so that it always scrolls the vertical container
            AddAssert("inner container scrolled only", () => checkScrollCurrent(innerScroll, outerScroll));
            AddStep("reset inner scroll", () => innerScroll.ScrollToStart(false));

            AddStep("scroll horizontally", () => InputManager.ScrollHorizontalBy(-50, true));
            AddAssert("horizontal container scrolled only", () => checkScrollCurrent(horizontalScroll, verticalScroll));
            AddStep("reset horizontal scroll", () => horizontalScroll.ScrollToStart(false));

            if (holdShift)
                AddStep("release shift", () => InputManager.ReleaseKey(Key.ShiftLeft));
        }
    }
}
