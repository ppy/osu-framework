// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Input
{
    [HeadlessTest]
    public class TouchInputTest : ManualInputManagerTestScene
    {
        private FillFlowContainer<InputReceptor> receptors;

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
            Child = receptors = new FillFlowContainer<InputReceptor>
            {
                RelativeSizeAxes = Axes.Both,
                Position = new Vector2(15f),
                Spacing = new Vector2(15f),
                ChildrenEnumerable = Enum.GetValues(typeof(TouchSource)).Cast<TouchSource>()
                                         .Select(s => new InputReceptor(s)),
            };
        });

        private static Vector2 getDownPos(InputReceptor r) => r.ScreenSpaceDrawQuad.TopLeft + Vector2.One; // +1 to avoid precision errors causing receptor not receiving input.
        private static Vector2 getMovePos(InputReceptor r) => r.ScreenSpaceDrawQuad.Centre;
        private static Vector2 getUpPos(InputReceptor r) => r.ScreenSpaceDrawQuad.BottomLeft + new Vector2(10f);

        [Test]
        public void TestTouchInputHandling()
        {
            AddStep("activate touches", () =>
            {
                foreach (var r in receptors)
                    InputManager.BeginTouch(new Touch(r.AssociatedSource, getDownPos(r)));
            });

            AddAssert("received correct touch-down event", () =>
            {
                foreach (var r in receptors)
                {
                    // attempt dequeuing from touch events queue.
                    if (!(r.TouchEvents.TryDequeue(out TouchEvent te) && te is TouchDownEvent touchDown))
                        return false;

                    // check correct provided information.
                    if (touchDown.ScreenSpaceTouch.Source != r.AssociatedSource ||
                        touchDown.ScreenSpaceTouch.Position != getDownPos(r) ||
                        touchDown.ScreenSpaceTouchDownPosition != getDownPos(r))
                        return false;

                    // check no other events popped up.
                    if (r.TouchEvents.Count > 0)
                        return false;
                }

                return true;
            });

            AddStep("move touches", () =>
            {
                foreach (var r in receptors)
                    InputManager.MoveTouchTo(new Touch(r.AssociatedSource, getMovePos(r)));
            });

            AddAssert("received correct touch-move event", () =>
            {
                foreach (var r in receptors)
                {
                    if (!(r.TouchEvents.TryDequeue(out TouchEvent te) && te is TouchMoveEvent touchMove))
                        return false;

                    if (touchMove.ScreenSpaceTouch.Source != r.AssociatedSource ||
                        touchMove.ScreenSpaceTouch.Position != getMovePos(r) ||
                        touchMove.ScreenSpaceLastTouchPosition != getDownPos(r) ||
                        touchMove.ScreenSpaceTouchDownPosition != getDownPos(r))
                        return false;

                    if (r.TouchEvents.Count > 0)
                        return false;
                }

                return true;
            });

            AddStep("deactivate touches out of receptors", () =>
            {
                foreach (var r in receptors)
                    InputManager.EndTouch(new Touch(r.AssociatedSource, getUpPos(r)));
            });

            AddAssert("received correct touch events", () =>
            {
                foreach (var r in receptors)
                {
                    // event #1: move touch to deactivation position.
                    // even if it's outside the receptor area, move events must still be fired to handlers of down event.
                    if (!(r.TouchEvents.TryDequeue(out TouchEvent te1) && te1 is TouchMoveEvent touchMove))
                        return false;

                    if (touchMove.ScreenSpaceTouch.Source != r.AssociatedSource ||
                        touchMove.ScreenSpaceTouch.Position != getUpPos(r) ||
                        touchMove.ScreenSpaceLastTouchPosition != getMovePos(r) ||
                        touchMove.ScreenSpaceTouchDownPosition != getDownPos(r))
                        return false;

                    // event #2: deactivate touch.
                    if (!(r.TouchEvents.TryDequeue(out TouchEvent te2) && te2 is TouchUpEvent touchUp))
                        return false;

                    if (touchUp.ScreenSpaceTouch.Source != r.AssociatedSource ||
                        touchUp.ScreenSpaceTouch.Position != getUpPos(r) ||
                        touchUp.ScreenSpaceTouchDownPosition != getDownPos(r))
                        return false;

                    if (r.TouchEvents.Count > 0)
                        return false;
                }

                return true;
            });
        }

        [Test]
        public void TestMouseInputAppliedFromPrimaryTouch()
        {
            AddStep("activate touches", () =>
            {
                foreach (var r in receptors)
                    InputManager.BeginTouch(new Touch(r.AssociatedSource, getDownPos(r)));
            });

            AddAssert("received correct mouse-down event", () =>
            {
                foreach (var r in receptors)
                {
                    if (r.AssociatedSource != TouchSource.Touch1)
                    {
                        // secondary touch receptors should not receive any mouse events.
                        if (r.MouseEvents.Count > 0)
                            return false;

                        continue;
                    }

                    // event #1: move mouse to primary-touch activation position.
                    if (!(r.MouseEvents.TryDequeue(out MouseEvent me1) && me1 is MouseMoveEvent mouseMove))
                        return false;

                    if (mouseMove.ScreenSpaceMousePosition != getDownPos(r))
                        return false;

                    // event #2: press mouse left-button (from primary-touch activation).
                    if (!(r.MouseEvents.TryDequeue(out MouseEvent me2) && me2 is MouseDownEvent mouseDown))
                        return false;

                    if (mouseDown.Button != MouseButton.Left ||
                        mouseDown.ScreenSpaceMousePosition != getDownPos(r) ||
                        mouseDown.ScreenSpaceMouseDownPosition != getDownPos(r))
                        return false;

                    if (r.MouseEvents.Count > 0)
                        return false;
                }

                return true;
            });

            AddStep("move touches", () =>
            {
                foreach (var r in receptors)
                    InputManager.MoveTouchTo(new Touch(r.AssociatedSource, getMovePos(r)));
            });

            AddAssert("received correct mouse-drag event", () =>
            {
                foreach (var r in receptors)
                {
                    if (r.AssociatedSource != TouchSource.Touch1)
                    {
                        if (r.MouseEvents.Count > 0)
                            return false;

                        continue;
                    }

                    // mouse-move event fired regardless of whether its dragging, dequeue it first.
                    if (!(r.MouseEvents.TryDequeue(out MouseEvent me1) && me1 is MouseMoveEvent mouseMove))
                        return false;

                    if (mouseMove.ScreenSpaceMousePosition != getMovePos(r) ||
                        mouseMove.ScreenSpaceLastMousePosition != getDownPos(r))
                        return false;

                    if (!(r.MouseEvents.TryDequeue(out MouseEvent me2) && me2 is DragEvent mouseDrag))
                        return false;

                    if (mouseDrag.Button != MouseButton.Left ||
                        mouseDrag.ScreenSpaceMousePosition != getMovePos(r) ||
                        mouseDrag.ScreenSpaceLastMousePosition != getDownPos(r) ||
                        mouseDrag.ScreenSpaceMouseDownPosition != getDownPos(r))
                        return false;

                    if (r.MouseEvents.Count > 0)
                        return false;
                }

                return true;
            });

            AddStep("deactivate touches", () =>
            {
                foreach (var r in receptors)
                    InputManager.EndTouch(new Touch(r.AssociatedSource, getUpPos(r)));
            });

            AddAssert("received correct mouse events", () =>
            {
                foreach (var r in receptors)
                {
                    if (r.AssociatedSource != TouchSource.Touch1)
                    {
                        if (r.MouseEvents.Count > 0)
                            return false;

                        continue;
                    }

                    if (!(r.MouseEvents.TryDequeue(out MouseEvent me1) && me1 is DragEvent mouseDrag))
                        return false;

                    if (mouseDrag.Button != MouseButton.Left ||
                        mouseDrag.ScreenSpaceMousePosition != getUpPos(r) ||
                        mouseDrag.ScreenSpaceLastMousePosition != getMovePos(r) ||
                        mouseDrag.ScreenSpaceMouseDownPosition != getDownPos(r))
                        return false;

                    if (!(r.MouseEvents.TryDequeue(out MouseEvent me2) && me2 is MouseUpEvent mouseUp))
                        return false;

                    if (mouseUp.Button != MouseButton.Left ||
                        mouseUp.ScreenSpaceMousePosition != getUpPos(r) ||
                        mouseUp.ScreenSpaceMouseDownPosition != getDownPos(r))
                        return false;

                    if (r.MouseEvents.Count > 0)
                        return false;
                }

                return true;
            });
        }

        private class InputReceptor : Box
        {
            public readonly TouchSource AssociatedSource;

            public readonly Queue<TouchEvent> TouchEvents = new Queue<TouchEvent>();
            public readonly Queue<MouseEvent> MouseEvents = new Queue<MouseEvent>();

            public InputReceptor(TouchSource source)
            {
                AssociatedSource = source;
                Size = new Vector2(100f);
            }

            protected override bool Handle(UIEvent e)
            {
                switch (e)
                {
                    case TouchDownEvent _:
                    case TouchMoveEvent _:
                    case TouchUpEvent _:
                        TouchEvents.Enqueue((TouchEvent)e);
                        break;

                    case MouseDownEvent _:
                    case MouseMoveEvent _:
                    case DragEvent _:
                    case MouseUpEvent _:
                        MouseEvents.Enqueue((MouseEvent)e);
                        break;
                }

                return true;
            }
        }
    }
}
