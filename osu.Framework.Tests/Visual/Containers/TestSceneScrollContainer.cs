// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneScrollContainer : ManualInputManagerTestScene
    {
        private ScrollContainer<Drawable> scrollContainer;

        [SetUp]
        public void Setup() => Schedule(Clear);

        [TestCase(0)]
        [TestCase(100)]
        public void TestScrollTo(float clampExtension)
        {
            const float container_height = 100;
            const float box_height = 400;

            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = new BasicScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(container_height),
                    ClampExtension = clampExtension,
                    Child = new Box { Size = new Vector2(100, box_height) }
                });
            });

            scrollTo(-100, box_height - container_height, clampExtension);
            checkPosition(0);

            scrollTo(100, box_height - container_height, clampExtension);
            checkPosition(100);

            scrollTo(300, box_height - container_height, clampExtension);
            checkPosition(300);

            scrollTo(400, box_height - container_height, clampExtension);
            checkPosition(300);

            scrollTo(500, box_height - container_height, clampExtension);
            checkPosition(300);
        }

        private FillFlowContainer fill;

        [Test]
        public void TestScrollIntoView()
        {
            const float item_height = 25;

            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = new BasicScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(item_height * 4),
                    Child = fill = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                    },
                });

                for (int i = 0; i < 8; i++)
                {
                    fill.Add(new Box
                    {
                        Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                        RelativeSizeAxes = Axes.X,
                        Height = item_height,
                    });
                }
            });

            // simple last item (hits bottom of view)
            scrollIntoView(7, item_height * 4);

            // position doesn't change when item in view
            scrollIntoView(6, item_height * 4);

            // scroll in reverse without overscrolling
            scrollIntoView(1, item_height);

            // scroll forwards with small (non-zero) view
            // current position will change on restore size
            scrollIntoView(7, item_height * 7, heightAdjust: 15, expectedPostAdjustPosition: 100);

            // scroll backwards with small (non-zero) view
            // current position won't change on restore size
            scrollIntoView(2, item_height * 2, heightAdjust: 15, expectedPostAdjustPosition: item_height * 2);

            // test forwards scroll with zero container height
            scrollIntoView(7, item_height * 7, heightAdjust: 0, expectedPostAdjustPosition: item_height * 4);

            // test backwards scroll with zero container height
            scrollIntoView(2, item_height * 2, heightAdjust: 0, expectedPostAdjustPosition: item_height * 2);
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
                    Child = new Box { Size = new Vector2(200, 300) }
                });
            });

            AddStep("Click and drag scrollcontainer", () =>
            {
                InputManager.MoveMouseTo(scrollContainer);
                InputManager.PressButton(MouseButton.Left);
                // Required for the dragging state to be set correctly.
                InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(10f)));
            });

            AddStep("Move mouse up", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre - new Vector2(0, 1000)));
            checkPosition(withClampExtension ? 200 : 100);
            AddStep("Move mouse down", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre + new Vector2(0, 1000)));
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

        [Test]
        public void TestClampedScrollbar()
        {
            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = new ClampedScrollbarScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(500),
                    Child = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new[]
                        {
                            new Box { Size = new Vector2(500) },
                            new Box { Size = new Vector2(500) },
                            new Box { Size = new Vector2(500) },
                        }
                    }
                });
            });

            AddStep("scroll to end", () => scrollContainer.ScrollToEnd(false));
            checkScrollbarPosition(250);

            AddStep("scroll to start", () => scrollContainer.ScrollToStart(false));
            checkScrollbarPosition(0);
        }

        [Test]
        public void TestClampedScrollbarDrag()
        {
            ClampedScrollbarScrollContainer clampedContainer = null;

            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = clampedContainer = new ClampedScrollbarScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(500),
                    Child = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new[]
                        {
                            new Box { Size = new Vector2(500) },
                            new Box { Size = new Vector2(500) },
                            new Box { Size = new Vector2(500) },
                        }
                    }
                });
            });

            AddStep("Click scroll bar", () =>
            {
                InputManager.MoveMouseTo(clampedContainer.Scrollbar);
                InputManager.PressButton(MouseButton.Left);
            });

            // Position at mouse down
            checkScrollbarPosition(0);

            AddStep("begin drag", () =>
            {
                // Required for the dragging state to be set correctly.
                InputManager.MoveMouseTo(clampedContainer.Scrollbar.ToScreenSpace(clampedContainer.Scrollbar.LayoutRectangle.Centre + new Vector2(0, -10f)));
            });

            AddStep("Move mouse up", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.TopRight - new Vector2(0, 20)));
            checkScrollbarPosition(0);
            AddStep("Move mouse down", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.BottomRight + new Vector2(0, 20)));
            checkScrollbarPosition(250);
            AddStep("Release mouse button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkScrollbarPosition(250);
        }

        [Test]
        public void TestHandleKeyboardRepeatAfterRemoval()
        {
            AddStep("create scroll container", () =>
            {
                Add(scrollContainer = new RepeatCountingScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(500),
                    Child = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new[]
                        {
                            new Box { Size = new Vector2(500) },
                            new Box { Size = new Vector2(500) },
                            new Box { Size = new Vector2(500) },
                        }
                    }
                });
            });

            AddStep("move mouse to scroll container", () => InputManager.MoveMouseTo(scrollContainer));
            AddStep("press page down and remove scroll container", () => InputManager.PressKey(Key.PageDown));
            AddStep("remove scroll container", () =>
            {
                Remove(scrollContainer);
                ((RepeatCountingScrollContainer)scrollContainer).RepeatCount = 0;
            });

            AddWaitStep("wait for repeats", 5);
        }

        [Test]
        public void TestEmptyScrollContainerDoesNotHandleScrollAndDrag()
        {
            AddStep("create scroll container", () =>
            {
                Add(scrollContainer = new InputHandlingScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(500),
                });
            });

            AddStep("Perform scroll", () =>
            {
                InputManager.MoveMouseTo(scrollContainer);
                InputManager.ScrollVerticalBy(50);
            });

            AddAssert("Scroll was not handled", () =>
            {
                var inputHandlingScrollContainer = (InputHandlingScrollContainer)scrollContainer;
                return inputHandlingScrollContainer.ScrollHandled.HasValue && !inputHandlingScrollContainer.ScrollHandled.Value;
            });

            AddStep("Perform drag", () =>
            {
                InputManager.MoveMouseTo(scrollContainer);
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(scrollContainer, new Vector2(50));
            });

            AddAssert("Drag was not handled", () =>
            {
                var inputHandlingScrollContainer = (InputHandlingScrollContainer)scrollContainer;
                return inputHandlingScrollContainer.DragHandled.HasValue && !inputHandlingScrollContainer.DragHandled.Value;
            });
        }

        [Test]
        public void TestScrollbarRespectsPadding()
        {
            const float container_height = 100;
            const float box_height = 400;
            const float padding = 10;

            AddStep("Create scroll container", () =>
            {
                Add(new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(container_height),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.Red,
                        },
                        scrollContainer = new BasicScrollContainer
                        {
                            Padding = new MarginPadding { Vertical = padding },
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Child = new Box { Size = new Vector2(100, box_height) }
                        },
                    }
                });
            });

            AddStep("Scroll to start", () => { scrollContainer.ScrollToStart(false); });

            checkScrollbarPosition(0);

            AddStep("Scroll to end", () => scrollContainer.ScrollToEnd(false));

            checkScrollbarPosition(64);
        }

        [TestCase(Direction.Horizontal, Direction.Vertical)]
        [TestCase(Direction.Vertical, Direction.Horizontal)]
        public void TestNestedScrolling(Direction outer, Direction inner)
        {
            BasicScrollContainer horizontalScroll = null;
            BasicScrollContainer verticalScroll = null;

            AddStep("create scroll containers", () =>
            {
                BasicScrollContainer outerScroll;
                BasicScrollContainer innerScroll;

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

            // if the inner is a horizontal scroll container, it'll absorb input either way,
            // as vertical scrolls translate to horizontal in a horizontal scroll container.
            if (inner != Direction.Horizontal)
            {
                AddStep("scroll vertically", () => InputManager.ScrollVerticalBy(-50));
                AddAssert("vertical container scrolled only", () => checkScrollCurrent(verticalScroll, horizontalScroll));
                AddStep("reset vertical scroll", () => verticalScroll.ScrollToStart(false));
            }

            AddStep("scroll horizontally", () => InputManager.ScrollHorizontalBy(-50));
            AddAssert("horizontal container scrolled only", () => checkScrollCurrent(horizontalScroll, verticalScroll));
            AddStep("reset horizontal scroll", () => horizontalScroll.ScrollToStart(false));

            static bool checkScrollCurrent(BasicScrollContainer scrolled, BasicScrollContainer notScrolled) => notScrolled.Current == 0 && Precision.DefinitelyBigger(scrolled.Current, 0f);
        }

        /// <summary>
        /// Ensures that initiating a drag with horizontal delta on a singular vertical <see cref="ScrollContainer{T}"/> doesn't prevent from continuing with vertical drags.
        /// </summary>
        /// <remarks>
        /// If the vertical scroll container is nested inside of a horizontal one, then it should prevent it, as covered in <see cref="TestNestedScrolling"/>.
        /// </remarks>
        [TestCase(false)]
        [TestCase(true)]
        public void TestSingularVerticalScrollWithHorizontalDelta(bool withClampExtension)
        {
            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = new BasicScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200),
                    ClampExtension = withClampExtension ? 100 : 0,
                    Child = new Box { Size = new Vector2(200, 300) }
                });
            });

            AddStep("Click and drag horizontally", () =>
            {
                InputManager.MoveMouseTo(scrollContainer);
                InputManager.PressButton(MouseButton.Left);

                // Required for the dragging state to be set correctly.
                InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(20f, 0f)));
            });

            AddStep("Move mouse up", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre - new Vector2(0, 1000)));
            checkPosition(withClampExtension ? 200 : 100);
            AddStep("Move mouse down", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre + new Vector2(0, 1000)));
            checkPosition(withClampExtension ? -100 : 0);

            AddStep("Move mouse diagonally up", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre - new Vector2(2000, 1000)));
            checkPosition(withClampExtension ? 200 : 100);
            AddStep("Move mouse diagonally down", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre + new Vector2(2000, 1000)));
            checkPosition(withClampExtension ? -100 : 0);

            AddStep("Release mouse button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkPosition(0);

            AddStep("Hover over scroll container", () => InputManager.MoveMouseTo(scrollContainer));
            AddStep("Scroll diagonally down", () => InputManager.ScrollBy(new Vector2(-20, -10)));
            checkPosition(100);
            AddStep("Scroll diagonally up", () => InputManager.ScrollBy(new Vector2(20, 10)));
            checkPosition(0);
        }

        [Test]
        public void TestDragHandlingUpdatesOnParentChanges()
        {
            BasicScrollContainer horizontalScrollContainer = null;

            AddStep("Create scroll container", () =>
            {
                Add(scrollContainer = new BasicScrollContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200),
                    ClampExtension = 0,
                    Child = new Box { Size = new Vector2(200, 300) }
                });
            });

            AddStep("Click and drag horizontally", () =>
            {
                InputManager.MoveMouseTo(scrollContainer);
                InputManager.PressButton(MouseButton.Left);

                // Required for the dragging state to be set correctly.
                InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(20f, 0f)));
            });

            AddStep("Move mouse diagonally up", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre - new Vector2(1000, 1000)));
            checkPosition(100);
            AddStep("Move mouse diagonally down", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre + new Vector2(1000, 1000)));
            checkPosition(0);

            AddStep("Release mouse button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkPosition(0);

            AddStep("Nest vertical scroll inside of horizontal", () =>
            {
                Remove(scrollContainer);

                Add(horizontalScrollContainer = new BasicScrollContainer(Direction.Horizontal)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(400),
                    ClampExtension = 0,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Size = new Vector2(500, 400),
                            Colour = FrameworkColour.Yellow,
                        },
                        scrollContainer
                    },
                });
            });

            AddStep("Click and drag horizontally", () =>
            {
                InputManager.MoveMouseTo(scrollContainer);
                InputManager.PressButton(MouseButton.Left);

                // Required for the dragging state to be set correctly.
                InputManager.MoveMouseTo(scrollContainer.ToScreenSpace(scrollContainer.LayoutRectangle.Centre + new Vector2(20f, 0f)));
            });

            AddStep("Move mouse diagonally up", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre - new Vector2(1000, 1000)));
            AddUntilStep("horizontal position at 100", () => Precision.AlmostEquals(100, horizontalScrollContainer.Current, 1));
            AddUntilStep("vertical position at 0", () => Precision.AlmostEquals(0, scrollContainer.Current, 1));

            AddStep("Move mouse diagonally down", () => InputManager.MoveMouseTo(scrollContainer.ScreenSpaceDrawQuad.Centre + new Vector2(1000, 1000)));
            AddUntilStep("horizontal position at 0", () => Precision.AlmostEquals(0, horizontalScrollContainer.Current, 1));
            AddUntilStep("vertical position at 0", () => Precision.AlmostEquals(0, scrollContainer.Current, 1));

            AddStep("Release mouse button", () => InputManager.ReleaseButton(MouseButton.Left));
            AddUntilStep("horizontal position at 0", () => Precision.AlmostEquals(0, horizontalScrollContainer.Current, 1));
            AddUntilStep("vertical position at 0", () => Precision.AlmostEquals(0, scrollContainer.Current, 1));
        }

        private void scrollIntoView(int index, float expectedPosition, float? heightAdjust = null, float? expectedPostAdjustPosition = null)
        {
            if (heightAdjust != null)
                AddStep("set container height zero", () => scrollContainer.Height = heightAdjust.Value);

            AddStep($"scroll {index} into view", () => scrollContainer.ScrollIntoView(fill.Skip(index).First()));
            AddUntilStep($"{index} is visible", () => !fill.Skip(index).First().IsMaskedAway);
            checkPosition(expectedPosition);

            if (heightAdjust != null)
            {
                Debug.Assert(expectedPostAdjustPosition != null, nameof(expectedPostAdjustPosition) + " != null");

                AddStep("restore height", () => scrollContainer.Height = 100);
                checkPosition(expectedPostAdjustPosition.Value);
            }
        }

        private void scrollTo(float position, float scrollContentHeight, float extension)
        {
            float clampedTarget = Math.Clamp(position, -extension, scrollContentHeight + extension);

            float immediateScrollPosition = 0;

            AddStep($"scroll to {position}", () =>
            {
                scrollContainer.ScrollTo(position, false);
                immediateScrollPosition = scrollContainer.Current;
            });

            AddAssert($"immediately scrolled to {clampedTarget}", () => Precision.AlmostEquals(clampedTarget, immediateScrollPosition, 1));
        }

        private void checkPosition(float expected, ScrollContainer<Drawable> scroll = null) => AddUntilStep($"position at {expected}", () => Precision.AlmostEquals(expected, (scroll ?? scrollContainer).Current, 1));

        private void checkScrollbarPosition(float expected) =>
            AddUntilStep($"scrollbar position at {expected}", () => Precision.AlmostEquals(expected, scrollContainer.InternalChildren[1].DrawPosition.Y, 1));

        private class RepeatCountingScrollContainer : BasicScrollContainer
        {
            public int RepeatCount { get; set; }

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (e.Repeat)
                    RepeatCount++;

                return base.OnKeyDown(e);
            }
        }

        private class ClampedScrollbarScrollContainer : BasicScrollContainer
        {
            public new ScrollbarContainer Scrollbar => base.Scrollbar;

            protected override ScrollbarContainer CreateScrollbar(Direction direction) => new ClampedScrollbar(direction);

            private class ClampedScrollbar : BasicScrollbar
            {
                protected internal override float MinimumDimSize => 250;

                public ClampedScrollbar(Direction direction)
                    : base(direction)
                {
                }
            }
        }

        private class InputHandlingScrollContainer : BasicScrollContainer
        {
            public bool? ScrollHandled { get; private set; }
            public bool? DragHandled { get; private set; }

            protected override bool OnScroll(ScrollEvent e)
            {
                ScrollHandled = base.OnScroll(e);
                return ScrollHandled.Value;
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                DragHandled = base.OnDragStart(e);
                return DragHandled.Value;
            }
        }
    }
}
