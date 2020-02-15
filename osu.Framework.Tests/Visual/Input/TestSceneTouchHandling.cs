// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneTouchHandling : ManualInputManagerTestScene
    {
        private EventReceptor receptor1, receptor2;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Spacing = new Vector2(200),
                    Children = new[]
                    {
                        receptor1 = new EventReceptor(),
                        receptor2 = new EventReceptor(),
                    }
                },
                new TouchVisualiser(),
            };
        });

        [Test]
        public void TestTouchInputHandling()
        {
            Vector2 t1Pos1 = default, t1Pos2 = default, t1Pos3 = default;
            Vector2 t2Pos1 = default, t2Pos2 = default, t2Pos3 = default;

            AddStep("activate touches", () =>
            {
                InputManager.ActivateTouchAt(t1Pos1 = receptor1.ToScreenSpace(receptor1.LayoutRectangle.TopLeft) + new Vector2(10), MouseButton.Touch1);
                InputManager.ActivateTouchAt(t2Pos1 = receptor2.ToScreenSpace(receptor2.LayoutRectangle.TopLeft) + new Vector2(10), MouseButton.Touch2);
            });

            AddAssert("received correct touch down", () =>
            {
                var touchDown1 = (TouchDownEvent)receptor1.TouchEvents.Dequeue();
                var touchDown2 = (TouchDownEvent)receptor2.TouchEvents.Dequeue();

                return touchDown1.ScreenSpaceTouch.Source == MouseButton.Touch1 &&
                       touchDown1.ScreenSpaceTouchPosition == t1Pos1 &&
                       touchDown1.ScreenSpaceTouchDownPosition == t1Pos1 &&
                       touchDown2.ScreenSpaceTouch.Source == MouseButton.Touch2 &&
                       touchDown2.ScreenSpaceTouchPosition == t2Pos1 &&
                       touchDown2.ScreenSpaceTouchDownPosition == t2Pos1;
            });

            AddStep("move touches inside drawables", () =>
            {
                InputManager.MoveTouchTo(t1Pos2 = receptor1.ToScreenSpace(receptor1.LayoutRectangle.Centre), MouseButton.Touch1);
                InputManager.MoveTouchTo(t2Pos2 = receptor2.ToScreenSpace(receptor2.LayoutRectangle.Centre), MouseButton.Touch2);
            });

            AddAssert("received correct touch move", () =>
            {
                var touchMove1 = (TouchMoveEvent)receptor1.TouchEvents.Dequeue();
                var touchMove2 = (TouchMoveEvent)receptor2.TouchEvents.Dequeue();

                return touchMove1.ScreenSpaceTouch.Source == MouseButton.Touch1 &&
                       touchMove1.ScreenSpaceTouchPosition == t1Pos2 &&
                       touchMove1.ScreenSpaceLastTouchPosition == t1Pos1 &&
                       touchMove1.ScreenSpaceTouchDownPosition == t1Pos1 &&
                       touchMove2.ScreenSpaceTouch.Source == MouseButton.Touch2 &&
                       touchMove2.ScreenSpaceTouchPosition == t2Pos2 &&
                       touchMove2.ScreenSpaceLastTouchPosition == t2Pos1 &&
                       touchMove2.ScreenSpaceTouchDownPosition == t2Pos1;
            });

            AddStep("move touches out of drawables", () =>
            {
                InputManager.MoveTouchTo(t1Pos3 = Content.ScreenSpaceDrawQuad.TopLeft, MouseButton.Touch1);
                InputManager.MoveTouchTo(t2Pos3 = Content.ScreenSpaceDrawQuad.TopRight, MouseButton.Touch2);
            });

            AddStep("deactivate touches", () =>
            {
                InputManager.DeactivateTouch(MouseButton.Touch1);
                InputManager.DeactivateTouch(MouseButton.Touch2);
            });

            AddAssert("received correct touch up", () =>
            {
                var touchUp1 = (TouchUpEvent)receptor1.TouchEvents.Dequeue();
                var touchUp2 = (TouchUpEvent)receptor2.TouchEvents.Dequeue();

                return touchUp1.ScreenSpaceTouch.Source == MouseButton.Touch1 &&
                       touchUp1.ScreenSpaceTouchPosition == t1Pos3 &&
                       touchUp1.ScreenSpaceTouchDownPosition == t1Pos1 &&
                       touchUp2.ScreenSpaceTouch.Source == MouseButton.Touch2 &&
                       touchUp2.ScreenSpaceTouchPosition == t2Pos3 &&
                       touchUp2.ScreenSpaceTouchDownPosition == t2Pos1;
            });
        }

        [Test]
        public void TestMouseInputFromTouch()
        {
            Vector2 pos1 = default, pos2 = default;

            AddStep("activate touches", () =>
            {
                InputManager.ActivateTouchAt(pos1 = receptor1.ToScreenSpace(receptor1.LayoutRectangle.TopLeft) + new Vector2(10), MouseButton.Touch1);
                InputManager.ActivateTouchAt(receptor2.ToScreenSpace(receptor2.LayoutRectangle.TopLeft) + new Vector2(10), MouseButton.Touch2);
            });

            AddAssert("received correct mouse event sequence", () =>
            {
                var mouseMove = (MouseMoveEvent)receptor1.MouseEvents.Dequeue();
                var mouseDown = (MouseDownEvent)receptor1.MouseEvents.Dequeue();

                return mouseMove.ScreenSpaceMousePosition == pos1 &&
                       mouseDown.ScreenSpaceMousePosition == pos1 &&
                       mouseDown.ScreenSpaceMouseDownPosition == pos1;
            });

            AddStep("move touches out of drawables", () =>
            {
                InputManager.MoveTouchTo(pos2 = receptor1.ToScreenSpace(receptor1.LayoutRectangle.Centre), MouseButton.Touch1);
                InputManager.MoveTouchTo(receptor2.ToScreenSpace(receptor2.LayoutRectangle.Centre), MouseButton.Touch2);
            });

            AddAssert("received correct mouse move", () =>
            {
                var mouseMove = (MouseMoveEvent)receptor1.MouseEvents.Dequeue();

                return mouseMove.ScreenSpaceMousePosition == pos2 &&
                       mouseMove.ScreenSpaceLastMousePosition == pos1;
            });

            AddStep("deactivate touches", () =>
            {
                InputManager.DeactivateTouch(MouseButton.Touch1);
                InputManager.DeactivateTouch(MouseButton.Touch2);
            });

            AddAssert("received correct mouse up", () =>
            {
                var mouseUp = (MouseUpEvent)receptor1.MouseEvents.Dequeue();

                return mouseUp.ScreenSpaceMousePosition == pos2 &&
                       mouseUp.ScreenSpaceMouseDownPosition == pos1;
            });
        }

        private class EventReceptor : Box
        {
            public readonly Queue<TouchEvent> TouchEvents = new Queue<TouchEvent>();
            public readonly Queue<MouseEvent> MouseEvents = new Queue<MouseEvent>();

            public EventReceptor()
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                Size = new Vector2(100);
            }

            protected override bool Handle(UIEvent e)
            {
                switch (e)
                {
                    case TouchMoveEvent _:
                    case TouchDownEvent _:
                        TouchEvents.Enqueue((TouchEvent)e);
                        return true;

                    case TouchUpEvent tue:
                        TouchEvents.Enqueue(tue);
                        return false;

                    case MouseMoveEvent _:
                    case MouseDownEvent _:
                        MouseEvents.Enqueue((MouseEvent)e);
                        return true;

                    case MouseUpEvent mue:
                        MouseEvents.Enqueue(mue);
                        return false;
                }

                return base.Handle(e);
            }
        }

        private class TouchVisualiser : CompositeDrawable
        {
            private static readonly Color4[] colours =
            {
                Color4.Red,
                Color4.Orange,
                Color4.Yellow,
                Color4.Lime,
                Color4.Green,
                Color4.Cyan,
                Color4.Blue,
                Color4.Purple,
                Color4.Magenta,
            };

            private readonly Dictionary<Touch, Circle> drawableTouches = new Dictionary<Touch, Circle>();

            public TouchVisualiser()
            {
                Depth = float.NegativeInfinity;
                RelativeSizeAxes = Axes.Both;
            }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

            protected override bool OnTouchDown(TouchDownEvent e)
            {
                var circle = new Circle
                {
                    Origin = Anchor.Centre,
                    Size = new Vector2(20),
                    Position = e.TouchPosition,
                    Colour = colours[e.ScreenSpaceTouch.Source - MouseButton.Touch1]
                };

                AddInternal(circle);
                drawableTouches.Add(e.ScreenSpaceTouch, circle);
                return false;
            }

            protected override void OnTouchUp(TouchUpEvent e)
            {
                drawableTouches[e.ScreenSpaceTouch].FadeOut(200, Easing.OutQuint).Expire();
                drawableTouches.Remove(e.ScreenSpaceTouch);
            }

            protected override bool OnTouchMove(TouchMoveEvent e)
            {
                var circle = drawableTouches[e.ScreenSpaceTouch];
                AddInternal(new FadingCircle(circle));
                circle.Position = e.TouchPosition;
                return false;
            }

            private class FadingCircle : Circle
            {
                public FadingCircle(Circle source)
                {
                    Origin = Anchor.Centre;
                    Size = source.Size;
                    Position = source.Position;
                    Colour = source.Colour;
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();
                    this.FadeOut(200).Expire();
                }
            }
        }
    }
}
