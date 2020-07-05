// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneTouchInput : ManualInputManagerTestScene
    {
        private static readonly TouchSource[] touch_sources = (TouchSource[])Enum.GetValues(typeof(TouchSource));

        private Container<InputReceptor> receptors;

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
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

        private float getTouchXPos(TouchSource source) => receptors[(int)source].DrawPosition.X + 10f;
        private Vector2 getTouchDownPos(TouchSource source) => receptors.ToScreenSpace(new Vector2(getTouchXPos(source), 1f));
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
        }

        [Test]
        public void TestMouseInputAppliedFromLatestTouch()
        {
            InputReceptor firstReceptor = null, lastReceptor = null;

            AddStep("retrieve receptors", () =>
            {
                firstReceptor = receptors[(int)TouchSource.Touch1];
                lastReceptor = receptors[(int)TouchSource.Touch10];
            });

            AddStep("activate first", () =>
            {
                InputManager.BeginTouch(new Touch(firstReceptor.AssociatedSource, getTouchDownPos(firstReceptor.AssociatedSource)));
            });

            AddAssert("received correct down event on first", () =>
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
                AddStep($"activate {s}", () => InputManager.BeginTouch(new Touch(s, getTouchDownPos(s))));

                AddAssert("mouse jumped to latest activated touch", () =>
                {
                    var r = receptors[(int)s];

                    if (!(r.MouseEvents.TryDequeue(out MouseEvent me1) && me1 is MouseMoveEvent mouseMove))
                        return false;

                    if (mouseMove.ScreenSpaceMousePosition != getTouchDownPos(r.AssociatedSource))
                        return false;

                    // Dequeue the false drag from first receptor to ensure there isn't any unexpected hidden event in this receptor.
                    if (!(firstReceptor.MouseEvents.TryDequeue(out MouseEvent me2) && me2 is DragEvent tmpMouseDrag))
                        return false;

                    if (tmpMouseDrag.Button != MouseButton.Left ||
                        tmpMouseDrag.ScreenSpaceMousePosition != getTouchDownPos(r.AssociatedSource) ||
                        tmpMouseDrag.ScreenSpaceLastMousePosition != getTouchDownPos(r.AssociatedSource - 1) ||
                        tmpMouseDrag.ScreenSpaceMouseDownPosition != getTouchDownPos(firstReceptor.AssociatedSource))
                        return false;

                    if (firstReceptor.MouseEvents.Count > 0)
                        return false;

                    return r.MouseEvents.Count == 0;
                });
            }

            AddStep("move touches", () =>
            {
                foreach (var s in touch_sources)
                    InputManager.MoveTouchTo(new Touch(s, getTouchMovePos(s)));
            });

            AddAssert("received correct movement event on latest", () =>
            {
                // mouse-move event fires regardless of whether it is dragging, dequeue it first.
                if (!(lastReceptor.MouseEvents.TryDequeue(out MouseEvent me1) && me1 is MouseMoveEvent mouseMove))
                    return false;

                if (mouseMove.ScreenSpaceMousePosition != getTouchMovePos(lastReceptor.AssociatedSource) ||
                    mouseMove.ScreenSpaceLastMousePosition != getTouchDownPos(lastReceptor.AssociatedSource))
                    return false;

                // Can be uncommented back when the wrong-down-queue issue is fixed, no drag to latest because mouse is still dragging from first.
                //if (!(lastReceptor.MouseEvents.TryDequeue(out MouseEvent me2) && me2 is DragEvent mouseDrag))
                //    return false;

                //if (mouseDrag.Button != MouseButton.Left ||
                //    mouseDrag.ScreenSpaceMousePosition != getTouchMovePos(lastReceptor.AssociatedSource) ||
                //    mouseDrag.ScreenSpaceLastMousePosition != getTouchDownPos(lastReceptor.AssociatedSource) ||
                //    mouseDrag.ScreenSpaceMouseDownPosition != getTouchDownPos(lastReceptor.AssociatedSource))
                //    return false;

                // Still dequeue the "false drag" from first receptor to ensure there isn't any unexpected hidden event in this receptor.
                if (!(firstReceptor.MouseEvents.TryDequeue(out MouseEvent me2) && me2 is DragEvent tmpMouseDrag))
                    return false;

                if (tmpMouseDrag.Button != MouseButton.Left ||
                    tmpMouseDrag.ScreenSpaceMousePosition != getTouchMovePos(lastReceptor.AssociatedSource) ||
                    tmpMouseDrag.ScreenSpaceLastMousePosition != getTouchDownPos(lastReceptor.AssociatedSource) ||
                    tmpMouseDrag.ScreenSpaceMouseDownPosition != getTouchDownPos(firstReceptor.AssociatedSource))
                    return false;

                return lastReceptor.MouseEvents.Count == 0;
            });

            AddStep("move touches outside of area", () =>
            {
                foreach (var s in touch_sources)
                    InputManager.MoveTouchTo(new Touch(s, getTouchUpPos(s)));
            });

            AddAssert("received correct movement event on latest", () =>
            {
                // No mouse move event here since the touch has moved outside of its receptor area. (only drag will received)

                // Can be uncommented back when the wrong-down-queue issue is fixed, no drag to latest because mouse is still dragging from first.
                //if (!(lastReceptor.MouseEvents.TryDequeue(out MouseEvent me) && me is DragEvent mouseDrag))
                //    return false;

                //if (mouseDrag.Button != MouseButton.Left ||
                //    mouseDrag.ScreenSpaceMousePosition != getTouchUpPos(lastReceptor.AssociatedSource) ||
                //    mouseDrag.ScreenSpaceLastMousePosition != getTouchMovePos(lastReceptor.AssociatedSource) ||
                //    mouseDrag.ScreenSpaceMouseDownPosition != getTouchDownPos(lastReceptor.AssociatedSource))
                //    return false;

                // Still dequeue the "wrong drag" from first receptor to ensure there isn't any unexpected hidden event in this receptor.
                if (!(firstReceptor.MouseEvents.TryDequeue(out MouseEvent me) && me is DragEvent tmpMouseDrag))
                    return false;

                if (tmpMouseDrag.Button != MouseButton.Left ||
                    tmpMouseDrag.ScreenSpaceMousePosition != getTouchUpPos(lastReceptor.AssociatedSource) ||
                    tmpMouseDrag.ScreenSpaceLastMousePosition != getTouchMovePos(lastReceptor.AssociatedSource) ||
                    tmpMouseDrag.ScreenSpaceMouseDownPosition != getTouchDownPos(firstReceptor.AssociatedSource))
                    return false;

                return lastReceptor.MouseEvents.Count == 0;
            });

            AddStep("deactivate touches", () =>
            {
                foreach (var s in touch_sources)
                    InputManager.EndTouch(new Touch(s, getTouchUpPos(s)));
            });

            AddAssert("received correct up event on latest", () =>
            {
                // Can be uncommented back when the wrong-down-queue issue is fixed, latest doesn't receive up because mouse thinks we're just dragging from first receptor to last.
                //if (!(lastReceptor.MouseEvents.TryDequeue(out MouseEvent me) && me is MouseUpEvent mouseUp))
                //    return false;

                //if (mouseUp.Button != MouseButton.Left ||
                //    mouseUp.ScreenSpaceMousePosition != getTouchUpPos(lastReceptor.AssociatedSource) ||
                //    mouseUp.ScreenSpaceMouseDownPosition != getTouchDownPos(lastReceptor.AssociatedSource))
                //    return false;

                // Still dequeue the "wrong up" from first receptor to ensure there isn't any unexpected hidden event in this receptor.
                if (!(firstReceptor.MouseEvents.TryDequeue(out MouseEvent me) && me is MouseUpEvent tmpMouseUp))
                    return false;

                if (tmpMouseUp.Button != MouseButton.Left ||
                    tmpMouseUp.ScreenSpaceMousePosition != getTouchUpPos(lastReceptor.AssociatedSource) ||
                    tmpMouseUp.ScreenSpaceMouseDownPosition != getTouchDownPos(firstReceptor.AssociatedSource))
                    return false;

                return lastReceptor.MouseEvents.Count == 0;
            });

            AddAssert("all events dequeued", () => receptors.All(r => r.MouseEvents.Count == 0));
        }

        private class InputReceptor : CompositeDrawable
        {
            public readonly TouchSource AssociatedSource;

            public readonly Queue<TouchEvent> TouchEvents = new Queue<TouchEvent>();
            public readonly Queue<MouseEvent> MouseEvents = new Queue<MouseEvent>();

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
                };
            }

            protected override bool Handle(UIEvent e)
            {
                switch (e)
                {
                    case TouchEvent te:
                        TouchEvents.Enqueue(te);
                        return !(e is TouchUpEvent);

                    case MouseDownEvent _:
                    case MouseMoveEvent _:
                    case DragEvent _:
                    case MouseUpEvent _:
                        MouseEvents.Enqueue((MouseEvent)e);
                        return !(e is MouseUpEvent);

                    // not worth enqueuing, just handle for receiving drag.
                    case DragStartEvent _:
                        return true;
                }

                return false;
            }
        }
    }
}
