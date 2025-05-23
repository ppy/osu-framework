// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public partial class TestSceneTouchInput : ManualInputManagerTestScene
    {
        private static readonly TouchSource[] touch_sources = (TouchSource[])Enum.GetValues(typeof(TouchSource));

        private Container<InputReceptor> receptors;

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
            InputManager.RightClickFromLongTouch = true;

            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Gray.Darken(2f),
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Text = "Parent"
                        },
                    }
                },
                receptors = new Container<InputReceptor>
                {
                    Padding = new MarginPadding { Bottom = 20f },
                    RelativeSizeAxes = Axes.Both,
                    ChildrenEnumerable = touch_sources.Select(s => new InputReceptor(s)
                    {
                        RelativePositionAxes = Axes.Both,
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Gray.Lighten((float)s / TouchState.MAX_TOUCH_COUNT),
                        X = (float)s / TouchState.MAX_TOUCH_COUNT,
                    })
                },
                new TestSceneTouchVisualiser.TouchVisualiser(),
            };
        });

        private float getTouchXPos(TouchSource source) => receptors[(int)source].DrawPosition.X + Content.DrawWidth / TouchState.MAX_TOUCH_COUNT / 2;
        private Vector2 getTouchDownPos(TouchSource source) => receptors.ToScreenSpace(new Vector2(getTouchXPos(source), 50f));
        private Vector2 getTouchMovePos(TouchSource source) => receptors.ToScreenSpace(new Vector2(getTouchXPos(source), receptors.DrawHeight / 2f));
        private Vector2 getTouchUpPos(TouchSource source) => receptors.ToScreenSpace(new Vector2(getTouchXPos(source), receptors.DrawHeight - 1f));

        [Test]
        public void TestTouchInputHandling()
        {
            AddStep("activate touches", () =>
            {
                foreach (var s in touch_sources)
                    InputManager.BeginTouch(new Touch(s, getTouchDownPos(s)));
            });

            AddAssert("received correct event for each receptor", () =>
            {
                foreach (var r in receptors)
                {
                    // attempt dequeuing from touch events queue.
                    if (!(r.TouchEvents.TryDequeue(out TouchEvent te) && te is TouchDownEvent touchDown))
                        return false;

                    // check correct provided information.
                    if (touchDown.ScreenSpaceTouch.Source != r.AssociatedSource ||
                        touchDown.ScreenSpaceTouch.Position != getTouchDownPos(r.AssociatedSource) ||
                        touchDown.ScreenSpaceTouchDownPosition != getTouchDownPos(r.AssociatedSource))
                        return false;

                    // check no other events popped up.
                    if (r.TouchEvents.Count > 0)
                        return false;
                }

                return true;
            });

            AddStep("move touches", () =>
            {
                foreach (var s in touch_sources)
                    InputManager.MoveTouchTo(new Touch(s, getTouchMovePos(s)));
            });

            AddAssert("received correct event for each receptor", () =>
            {
                foreach (var r in receptors)
                {
                    if (!(r.TouchEvents.TryDequeue(out TouchEvent te) && te is TouchMoveEvent touchMove))
                        return false;

                    if (touchMove.ScreenSpaceTouch.Source != r.AssociatedSource ||
                        touchMove.ScreenSpaceTouch.Position != getTouchMovePos(r.AssociatedSource) ||
                        touchMove.ScreenSpaceLastTouchPosition != getTouchDownPos(r.AssociatedSource) ||
                        touchMove.ScreenSpaceTouchDownPosition != getTouchDownPos(r.AssociatedSource))
                        return false;

                    if (r.TouchEvents.Count > 0)
                        return false;
                }

                return true;
            });

            AddStep("move touches outside of area", () =>
            {
                foreach (var s in touch_sources)
                    InputManager.MoveTouchTo(new Touch(s, getTouchUpPos(s)));
            });

            AddAssert("received correct event for each receptor", () =>
            {
                foreach (var r in receptors)
                {
                    if (!(r.TouchEvents.TryDequeue(out TouchEvent te) && te is TouchMoveEvent touchMove))
                        return false;

                    if (touchMove.ScreenSpaceTouch.Source != r.AssociatedSource ||
                        touchMove.ScreenSpaceTouch.Position != getTouchUpPos(r.AssociatedSource) ||
                        touchMove.ScreenSpaceLastTouchPosition != getTouchMovePos(r.AssociatedSource) ||
                        touchMove.ScreenSpaceTouchDownPosition != getTouchDownPos(r.AssociatedSource))
                        return false;

                    if (r.TouchEvents.Count > 0)
                        return false;
                }

                return true;
            });

            AddStep("deactivate touches out of receptors", () =>
            {
                foreach (var s in touch_sources)
                    InputManager.EndTouch(new Touch(s, getTouchUpPos(s)));
            });

            AddAssert("received correct event for each receptor", () =>
            {
                foreach (var r in receptors)
                {
                    if (!(r.TouchEvents.TryDequeue(out TouchEvent te) && te is TouchUpEvent touchUp))
                        return false;

                    if (touchUp.ScreenSpaceTouch.Source != r.AssociatedSource ||
                        touchUp.ScreenSpaceTouch.Position != getTouchUpPos(r.AssociatedSource) ||
                        touchUp.ScreenSpaceTouchDownPosition != getTouchDownPos(r.AssociatedSource))
                        return false;

                    if (r.TouchEvents.Count > 0)
                        return false;
                }

                return true;
            });

            // All touch events have been handled, mouse input should not be performed.
            // For simplicity, let's check whether we received mouse events or not.
            AddAssert("no mouse input performed", () => receptors.All(r => r.MouseEvents.Count == 0));
        }

        [Test]
        public void TestMouseInputAppliedFromLatestTouch()
        {
            InputReceptor firstReceptor = null, lastReceptor = null;

            AddStep("setup receptors to receive left mouse-from-touch", () =>
            {
                InputManager.RightClickFromLongTouch = false;

                foreach (var r in receptors)
                    r.HandleTouch = _ => false;
            });

            AddStep("retrieve receptors", () =>
            {
                firstReceptor = receptors.First();
                lastReceptor = receptors.Last();
            });

            AddStep("activate first", () =>
            {
                InputManager.BeginTouch(new Touch(firstReceptor.AssociatedSource, getTouchDownPos(firstReceptor.AssociatedSource)));
            });

            AddAssert("received mouse-down event on first", () =>
            {
                // event #1: move mouse to first touch position.
                if (!(firstReceptor.MouseEvents.TryDequeue(out MouseEvent me1) && me1 is MouseMoveEvent mouseMove))
                    return false;

                if (mouseMove.ScreenSpaceMousePosition != getTouchDownPos(firstReceptor.AssociatedSource))
                    return false;

                // event #2: press mouse left-button (from first touch activation).
                if (!(firstReceptor.MouseEvents.TryDequeue(out MouseEvent me2) && me2 is MouseDownEvent mouseDown))
                    return false;

                if (mouseDown.Button != MouseButton.Left ||
                    mouseDown.ScreenSpaceMousePosition != getTouchDownPos(firstReceptor.AssociatedSource) ||
                    mouseDown.ScreenSpaceMouseDownPosition != getTouchDownPos(firstReceptor.AssociatedSource))
                    return false;

                return firstReceptor.MouseEvents.Count == 0;
            });

            // Activate each touch after first source and assert mouse has jumped to it.
            foreach (var s in touch_sources.Skip(1))
            {
                Touch touch = default;

                AddStep($"activate {s}", () => InputManager.BeginTouch(touch = new Touch(s, getTouchDownPos(s))));
                AddAssert("mouse jumped to new touch", () => assertMouseOnTouchChange(touch, null, true));
            }

            Vector2? lastMovePosition = null;

            // Move each touch inside area and assert regular mouse-move events received.
            foreach (var s in touch_sources)
            {
                Touch touch = default;

                AddStep($"move {s} inside area", () => InputManager.MoveTouchTo(touch = new Touch(s, getTouchMovePos(s))));
                AddAssert("received regular mouse-move event", () =>
                {
                    // ReSharper disable once AccessToModifiedClosure
                    bool result = assertMouseOnTouchChange(touch, lastMovePosition, true);
                    lastMovePosition = touch.Position;
                    return result;
                });
            }

            // Move each touch outside of area and assert no MouseMoveEvent expected to be received.
            foreach (var s in touch_sources)
            {
                Touch touch = default;

                AddStep($"move {s} outside of area", () => InputManager.MoveTouchTo(touch = new Touch(s, getTouchUpPos(s))));
                AddAssert("no mouse-move event received", () =>
                {
                    // ReSharper disable once AccessToModifiedClosure
                    bool result = assertMouseOnTouchChange(touch, lastMovePosition, false);
                    lastMovePosition = touch.Position;
                    return result;
                });
            }

            // Deactivate each touch but last touch and assert mouse did not jump to it.
            foreach (var s in touch_sources.SkipLast(1))
            {
                AddStep($"deactivate {s}", () => InputManager.EndTouch(new Touch(s, getTouchUpPos(s))));
                AddAssert("no mouse event received", () => receptors[(int)s].MouseEvents.Count == 0);
            }

            AddStep("deactivate last", () =>
            {
                InputManager.EndTouch(new Touch(lastReceptor.AssociatedSource, getTouchUpPos(lastReceptor.AssociatedSource)));
            });

            AddAssert("received mouse-up event", () =>
            {
                // First receptor is the one handling the mouse down event, mouse up would be raised to it.
                if (!(firstReceptor.MouseEvents.TryDequeue(out MouseEvent me) && me is MouseUpEvent mouseUp))
                    return false;

                if (mouseUp.Button != MouseButton.Left ||
                    mouseUp.ScreenSpaceMousePosition != getTouchUpPos(lastReceptor.AssociatedSource) ||
                    mouseUp.ScreenSpaceMouseDownPosition != getTouchDownPos(firstReceptor.AssociatedSource))
                    return false;

                return firstReceptor.MouseEvents.Count == 0;
            });

            AddAssert("all events dequeued", () => receptors.All(r => r.MouseEvents.Count == 0));

            bool assertMouseOnTouchChange(Touch touch, Vector2? lastPosition, bool expectsMouseMove)
            {
                var receptor = receptors[(int)touch.Source];

                if (expectsMouseMove)
                {
                    if (!(receptor.MouseEvents.TryDequeue(out MouseEvent me1) && me1 is MouseMoveEvent mouseMove))
                        return false;

                    if (mouseMove.ScreenSpaceMousePosition != touch.Position ||
                        (lastPosition != null && mouseMove.ScreenSpaceLastMousePosition != lastPosition.Value))
                        return false;
                }

                // Dequeue the "false drag" from first receptor to ensure there isn't any unexpected hidden event in this receptor.
                if (!(firstReceptor.MouseEvents.TryDequeue(out MouseEvent me2) && me2 is DragEvent mouseDrag))
                    return false;

                if (mouseDrag.Button != MouseButton.Left ||
                    mouseDrag.ScreenSpaceMousePosition != touch.Position ||
                    (lastPosition != null && mouseDrag.ScreenSpaceLastMousePosition != lastPosition.Value) ||
                    mouseDrag.ScreenSpaceMouseDownPosition != getTouchDownPos(firstReceptor.AssociatedSource))
                    return false;

                return receptor.MouseEvents.Count == 0;
            }
        }

        [Test]
        public void TestMouseEventFromTouchIndication()
        {
            InputReceptor primaryReceptor = null;

            AddStep("retrieve primary receptor", () => primaryReceptor = receptors[(int)TouchSource.Touch1]);
            AddStep("setup receptors to discard mouse-from-touch events", () =>
            {
                primaryReceptor.HandleTouch = _ => false;
                primaryReceptor.HandleMouse = e => !(e.CurrentState.Mouse.LastSource is ISourcedFromTouch);
            });

            AddStep("perform input on primary touch", () =>
            {
                InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1)));
                InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, getTouchMovePos(TouchSource.Touch1)));
                InputManager.EndTouch(new Touch(TouchSource.Touch1, getTouchUpPos(TouchSource.Touch1)));
            });
            AddAssert("no mouse event received", () => primaryReceptor.MouseEvents.Count == 0);

            AddStep("perform input on mouse", () =>
            {
                InputManager.MoveMouseTo(getTouchDownPos(TouchSource.Touch1));
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(getTouchMovePos(TouchSource.Touch1));
                InputManager.ReleaseButton(MouseButton.Left);
            });
            AddAssert("all mouse events received", () =>
            {
                // mouse moved.
                if (!(primaryReceptor.MouseEvents.TryDequeue(out var me1) && me1 is MouseMoveEvent))
                    return false;

                // left down.
                if (!(primaryReceptor.MouseEvents.TryDequeue(out var me2) && me2 is MouseDownEvent))
                    return false;

                // mouse dragged with left.
                if (!(primaryReceptor.MouseEvents.TryDequeue(out var me3) && me3 is MouseMoveEvent))
                    return false;
                if (!(primaryReceptor.MouseEvents.TryDequeue(out var me4) && me4 is DragEvent))
                    return false;

                // left up.
                if (!(primaryReceptor.MouseEvents.TryDequeue(out var me5) && me5 is MouseUpEvent))
                    return false;

                return primaryReceptor.MouseEvents.Count == 0;
            });
        }

        [Test]
        public void TestMouseStillReleasedOnHierarchyInterference()
        {
            InputReceptor primaryReceptor = null;
            InputReceptor childReceptor = null;

            AddStep("retrieve primary receptor", () => primaryReceptor = receptors[(int)TouchSource.Touch1]);
            AddStep("setup handlers to receive left mouse-from-touch", () =>
            {
                InputManager.RightClickFromLongTouch = false;
                primaryReceptor.HandleTouch = _ => false;
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddAssert("primary receptor received mouse", () =>
            {
                bool event1 = primaryReceptor.MouseEvents.Dequeue() is MouseMoveEvent;
                bool event2 = primaryReceptor.MouseEvents.Dequeue() is MouseDownEvent;
                return event1 && event2 && primaryReceptor.MouseEvents.Count == 0;
            });

            AddStep("add drawable", () => primaryReceptor.Add(childReceptor = new InputReceptor(TouchSource.Touch1)
            {
                RelativeSizeAxes = Axes.Both,
                HandleTouch = _ => true,
            }));

            AddStep("end touch", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddAssert("primary receptor received mouse", () =>
            {
                bool event1 = primaryReceptor.MouseEvents.Dequeue() is MouseUpEvent;
                return event1 && primaryReceptor.MouseEvents.Count == 0;
            });

            AddAssert("child receptor received nothing", () =>
                childReceptor.TouchEvents.Count == 0 &&
                childReceptor.MouseEvents.Count == 0);
        }

        [Test]
        public void TestHoldTouchToRightClick()
        {
            InputReceptor primaryReceptor = null;

            AddStep("retrieve primary receptor", () => primaryReceptor = receptors[(int)TouchSource.Touch1]);
            AddStep("setup handlers to receive mouse-from-touch", () =>
            {
                primaryReceptor.HandleTouch = _ => false;
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddAssert("left down received", () =>
            {
                bool event1 = primaryReceptor.MouseEvents.Dequeue() is MouseMoveEvent;
                bool event2 = primaryReceptor.MouseEvents.Dequeue() is MouseDownEvent down && down.Button == MouseButton.Left;
                return event1 && event2 && primaryReceptor.MouseEvents.Count == 0;
            });

            AddWaitStep("hold touch", 4);
            AddAssert("right click received", () =>
            {
                bool event1 = primaryReceptor.MouseEvents.Dequeue() is MouseDownEvent down && down.Button == MouseButton.Right;
                bool event2 = primaryReceptor.MouseEvents.Dequeue() is MouseUpEvent up && up.Button == MouseButton.Right;
                return event1 && event2 && primaryReceptor.MouseEvents.Count == 0;
            });

            AddStep("move touch", () => InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, getTouchMovePos(TouchSource.Touch1))));
            AddAssert("received move and left drag", () =>
            {
                bool event1 = primaryReceptor.MouseEvents.Dequeue() is MouseMoveEvent;
                bool event2 = primaryReceptor.MouseEvents.Dequeue() is DragEvent drag && drag.Button == MouseButton.Left;
                return event1 && event2 && primaryReceptor.MouseEvents.Count == 0;
            });

            AddStep("end touch in place", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, getTouchMovePos(TouchSource.Touch1))));
            AddAssert("left up received", () =>
            {
                bool event1 = primaryReceptor.MouseEvents.Dequeue() is MouseUpEvent up && up.Button == MouseButton.Left;
                return event1 && primaryReceptor.MouseEvents.Count == 0;
            });
        }

        [Test]
        public void TestHoldTouchThenKeepHoldingAfterRightClick()
        {
            InputReceptor primaryReceptor = null;

            AddStep("retrieve primary receptor", () => primaryReceptor = receptors[(int)TouchSource.Touch1]);
            AddStep("setup handlers to receive mouse-from-touch", () =>
            {
                primaryReceptor.HandleTouch = _ => false;
                primaryReceptor.HandleMouse = e => e is MouseButtonEvent button && button.Button == MouseButton.Right;
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("hold touch", 4);
            AddAssert("right click received", () =>
            {
                bool event1 = primaryReceptor.MouseEvents.Dequeue() is MouseDownEvent down && down.Button == MouseButton.Right;
                bool event2 = primaryReceptor.MouseEvents.Dequeue() is MouseUpEvent up && up.Button == MouseButton.Right;
                return event1 && event2 && primaryReceptor.MouseEvents.Count == 0;
            });

            AddStep("nudge touch", () => InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1) + new Vector2(70))));
            AddWaitStep("keep holding", 4);
            AddAssert("no right click received", () => primaryReceptor.MouseEvents.Count == 0);
        }

        [Test]
        public void TestHoldTouchAndReleaseBeforeRightClick()
        {
            InputReceptor primaryReceptor = null;

            AddStep("retrieve primary receptor", () => primaryReceptor = receptors[(int)TouchSource.Touch1]);
            AddStep("setup handlers to receive mouse-from-touch", () =>
            {
                primaryReceptor.HandleTouch = _ => false;
                primaryReceptor.HandleMouse = e => e is MouseButtonEvent button && button.Button == MouseButton.Right;
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("hold shortly", 2);
            AddStep("end touch", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("wait a bit", 4);
            AddAssert("no right click received", () => primaryReceptor.MouseEvents.Count == 0);
        }

        [Test]
        public void TestHoldTouchAndNudgeBeforeRightClick()
        {
            InputReceptor primaryReceptor = null;

            AddStep("retrieve primary receptor", () => primaryReceptor = receptors[(int)TouchSource.Touch1]);
            AddStep("setup handlers to receive mouse-from-touch", () =>
            {
                primaryReceptor.HandleTouch = _ => false;
                primaryReceptor.HandleMouse = e => e is MouseButtonEvent button && button.Button == MouseButton.Right;
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("hold shortly", 2);
            AddStep("nudge touch", () => InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1) + new Vector2(70))));
            AddWaitStep("wait a bit", 4);
            AddAssert("right click received at original position", () =>
            {
                bool event1 = primaryReceptor.MouseEvents.Dequeue() is MouseDownEvent down && down.Button == MouseButton.Right && down.ScreenSpaceMousePosition == getTouchDownPos(TouchSource.Touch1);
                bool event2 = primaryReceptor.MouseEvents.Dequeue() is MouseUpEvent up && up.Button == MouseButton.Right;
                return event1 && event2 && primaryReceptor.MouseEvents.Count == 0;
            });
        }

        [Test]
        public void TestHoldTouchAndDragBeforeRightClick()
        {
            InputReceptor primaryReceptor = null;

            AddStep("retrieve primary receptor", () => primaryReceptor = receptors[(int)TouchSource.Touch1]);
            AddStep("setup handlers to receive mouse-from-touch", () =>
            {
                primaryReceptor.HandleTouch = _ => false;
                primaryReceptor.HandleMouse = e => e is MouseButtonEvent button && button.Button == MouseButton.Right;
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("hold shortly", 2);
            AddStep("move touch", () => InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, getTouchMovePos(TouchSource.Touch1))));
            AddWaitStep("wait a bit", 4);
            AddAssert("no right click received", () => primaryReceptor.MouseEvents.Count == 0);

            AddStep("move back", () => InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("wait a bit", 4);
            AddAssert("no right click received", () => primaryReceptor.MouseEvents.Count == 0);
        }

        [Test]
        public void TestHoldTwoTouchesAndReleaseFirstBeforeRightClick()
        {
            InputReceptor primaryReceptor = null;
            InputReceptor secondaryReceptor = null;

            AddStep("retrieve touch1 and touch2 receptor", () =>
            {
                primaryReceptor = receptors[(int)TouchSource.Touch1];
                secondaryReceptor = receptors[(int)TouchSource.Touch2];
            });

            AddStep("setup handlers to receive mouse-from-touch", () =>
            {
                primaryReceptor.HandleTouch = secondaryReceptor.HandleTouch = _ => false;
                primaryReceptor.HandleMouse = secondaryReceptor.HandleMouse = e => e is MouseButtonEvent button && button.Button == MouseButton.Right;
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("hold shortly", 2);
            AddStep("begin another", () => InputManager.BeginTouch(new Touch(TouchSource.Touch2, getTouchDownPos(TouchSource.Touch2))));
            AddStep("end first", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("wait a bit", 4);
            AddAssert("no right click received", () => primaryReceptor.MouseEvents.Count == 0 && secondaryReceptor.MouseEvents.Count == 0);
        }

        [Test]
        public void TestHoldTwoTouchesAndReleaseSecondBeforeRightClick()
        {
            InputReceptor primaryReceptor = null;
            InputReceptor secondaryReceptor = null;

            AddStep("retrieve touch1 and touch2 receptor", () =>
            {
                primaryReceptor = receptors[(int)TouchSource.Touch1];
                secondaryReceptor = receptors[(int)TouchSource.Touch2];
            });

            AddStep("setup handlers to receive mouse-from-touch", () =>
            {
                primaryReceptor.HandleTouch = secondaryReceptor.HandleTouch = _ => false;
                primaryReceptor.HandleMouse = secondaryReceptor.HandleMouse = e => e is MouseButtonEvent button && button.Button == MouseButton.Right;
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("hold shortly", 2);
            AddStep("begin another", () => InputManager.BeginTouch(new Touch(TouchSource.Touch2, getTouchDownPos(TouchSource.Touch2))));
            AddStep("end second", () => InputManager.EndTouch(new Touch(TouchSource.Touch2, getTouchDownPos(TouchSource.Touch2))));
            AddStep("move first", () => InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, getTouchMovePos(TouchSource.Touch1))));
            AddWaitStep("wait a bit", 4);
            AddAssert("no right click received", () => primaryReceptor.MouseEvents.Count == 0 && secondaryReceptor.MouseEvents.Count == 0);
        }

        [Test]
        public void TestHoldTouchAndDisableHoldingBeforeRightClick()
        {
            InputReceptor primaryReceptor = null;

            AddStep("retrieve primary receptor", () => primaryReceptor = receptors[(int)TouchSource.Touch1]);
            AddStep("setup handlers to receive mouse-from-touch", () =>
            {
                primaryReceptor.HandleTouch = _ => false;
                primaryReceptor.HandleMouse = e => e is MouseButtonEvent button && button.Button == MouseButton.Right;
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, getTouchDownPos(TouchSource.Touch1))));
            AddWaitStep("hold shortly", 2);
            AddStep("turn off hold-to-right-click", () => InputManager.RightClickFromLongTouch = false);
            AddWaitStep("wait a bit", 4);
            AddAssert("no right click received", () => primaryReceptor.MouseEvents.Count == 0);
        }

        private partial class InputReceptor : Container
        {
            public readonly TouchSource AssociatedSource;

            public readonly Queue<TouchEvent> TouchEvents = new Queue<TouchEvent>();
            public readonly Queue<MouseEvent> MouseEvents = new Queue<MouseEvent>();

            public Func<TouchEvent, bool> HandleTouch;
            public Func<MouseEvent, bool> HandleMouse;

            protected override Container<Drawable> Content => content;

            private readonly Container content;

            public InputReceptor(TouchSource source)
            {
                AssociatedSource = source;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new SpriteText
                    {
                        X = 15f,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = source.ToString(),
                        Colour = Color4.Black,
                    },
                    content = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                    }
                };
            }

            protected override bool Handle(UIEvent e)
            {
                switch (e)
                {
                    case TouchEvent te:
                        if (HandleTouch?.Invoke(te) != false)
                        {
                            TouchEvents.Enqueue(te);
                            return true;
                        }

                        break;

                    case MouseDownEvent:
                    case MouseMoveEvent:
                    case DragEvent:
                    case MouseUpEvent:
                        if (HandleMouse?.Invoke((MouseEvent)e) != false)
                        {
                            MouseEvents.Enqueue((MouseEvent)e);
                            return true;
                        }

                        break;

                    // not worth enqueuing, just handle for receiving drag.
                    case DragStartEvent dse:
                        return HandleMouse?.Invoke(dse) ?? true;
                }

                return false;
            }
        }
    }
}
