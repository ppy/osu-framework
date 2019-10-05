// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSuiteMouseStates : ManualInputManagerTestSuite<TestSceneMouseStates>
    {
        protected override void LoadComplete()
        {
            ((Container)InputManager.Parent).Add(new TestSceneMouseStates.StateTracker(0));
        }

        private void initTestScene()
        {
            TestScene.EventCounts1.Clear();
            TestScene.EventCounts2.Clear();
            // InitialMousePosition cannot be used here because the event counters should be resetted after the initial mouse move.
            AddStep("move mouse to center", () => InputManager.MoveMouseTo(TestScene.ActionContainer));
            AddStep("reset event counters", () =>
            {
                TestScene.S1.Reset();
                TestScene.S2.Reset();
            });
        }

        [Test]
        public void BasicScroll()
        {
            initTestScene();

            AddStep("scroll some", () => InputManager.ScrollBy(new Vector2(-1, 1)));
            checkEventCount(TestSceneMouseStates.MOVE);
            checkEventCount(TestSceneMouseStates.SCROLL, 1);
            checkLastScrollDelta(new Vector2(-1, 1));

            AddStep("scroll some", () => InputManager.ScrollBy(new Vector2(1, -1)));
            checkEventCount(TestSceneMouseStates.SCROLL, 1);
            checkLastScrollDelta(new Vector2(1, -1));
        }

        [Test]
        public void BasicMovement()
        {
            initTestScene();

            AddStep("push move", () => InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.TopLeft));
            checkEventCount(TestSceneMouseStates.MOVE, 1);
            checkEventCount(TestSceneMouseStates.SCROLL);

            AddStep("push move", () => InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.TopRight));
            checkEventCount(TestSceneMouseStates.MOVE, 1);
            checkEventCount(TestSceneMouseStates.SCROLL);
            checkLastPositionDelta(() => TestScene.MarginBox.ScreenSpaceDrawQuad.Width);

            AddStep("push move", () => InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.BottomRight));
            checkEventCount(TestSceneMouseStates.MOVE, 1);
            checkLastPositionDelta(() => TestScene.MarginBox.ScreenSpaceDrawQuad.Height);

            AddStep("push two moves", () =>
            {
                InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.TopLeft);
                InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.BottomLeft);
            });
            checkEventCount(TestSceneMouseStates.MOVE, 2);
            checkLastPositionDelta(() => Vector2.Distance(TestScene.MarginBox.ScreenSpaceDrawQuad.TopLeft, TestScene.MarginBox.ScreenSpaceDrawQuad.BottomLeft));
        }

        [Test]
        public void BasicButtons()
        {
            initTestScene();

            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.MOUSE_DOWN, 1);

            AddStep("press right button", () => InputManager.PressButton(MouseButton.Right));
            checkEventCount(TestSceneMouseStates.MOUSE_DOWN, 1);

            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.MOUSE_UP, 1);

            AddStep("release right button", () => InputManager.ReleaseButton(MouseButton.Right));
            checkEventCount(TestSceneMouseStates.MOUSE_UP, 1);

            AddStep("press three buttons", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Right);
                InputManager.PressButton(MouseButton.Button1);
            });
            checkEventCount(TestSceneMouseStates.MOUSE_DOWN, 3);

            AddStep("Release mouse buttons", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Right);
                InputManager.ReleaseButton(MouseButton.Button1);
            });
            checkEventCount(TestSceneMouseStates.MOUSE_UP, 3);

            AddStep("press two buttons", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Right);
            });

            checkEventCount(TestSceneMouseStates.MOUSE_DOWN, 2);
            checkEventCount(TestSceneMouseStates.MOUSE_UP, 1);

            AddStep("release", () => InputManager.ReleaseButton(MouseButton.Right));

            checkEventCount(TestSceneMouseStates.MOVE);
            checkEventCount(TestSceneMouseStates.MOUSE_UP, 1);
        }

        [Test]
        public void Drag()
        {
            initTestScene();

            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.MOUSE_DOWN, 1);
            checkIsDragged(false);

            AddStep("move bottom left", () => InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.BottomLeft));
            checkEventCount(TestSceneMouseStates.DRAG_START, 1);
            checkIsDragged(true);

            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.MOUSE_UP, 1);
            checkIsDragged(false);
        }

        [Test]
        public void CombinationChanges()
        {
            initTestScene();

            AddStep("push move", () => InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.BottomLeft));
            checkEventCount(TestSceneMouseStates.MOVE, 1);

            AddStep("push move and scroll", () =>
            {
                InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.Centre);
                InputManager.ScrollBy(new Vector2(1, 2));
            });

            checkEventCount(TestSceneMouseStates.MOVE, 1);
            checkEventCount(TestSceneMouseStates.SCROLL, 1);
            checkLastScrollDelta(new Vector2(1, 2));
            checkLastPositionDelta(() => Vector2.Distance(TestScene.MarginBox.ScreenSpaceDrawQuad.BottomLeft, TestScene.MarginBox.ScreenSpaceDrawQuad.Centre));

            AddStep("Move mouse to out of bounds", () => InputManager.MoveMouseTo(Vector2.Zero));

            checkEventCount(TestSceneMouseStates.MOVE);
            checkEventCount(TestSceneMouseStates.SCROLL);

            AddStep("Move mouse", () =>
            {
                InputManager.MoveMouseTo(new Vector2(10));
                InputManager.ScrollBy(new Vector2(10));
            });

            // outside the bounds so should not increment.
            checkEventCount(TestSceneMouseStates.MOVE);
            checkEventCount(TestSceneMouseStates.SCROLL);
        }

        [Test]
        public void DragAndClick()
        {
            initTestScene();

            // mouseDown on a non-draggable -> mouseUp on a distant position: drag-clicking
            AddStep("move mouse", () => InputManager.MoveMouseTo(TestScene.OuterMarginBox.ScreenSpaceDrawQuad.TopLeft));
            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.DRAG_START);
            AddStep("drag non-draggable", () => InputManager.MoveMouseTo(TestScene.MarginBox));
            checkEventCount(TestSceneMouseStates.DRAG_START, 1, true);
            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.CLICK, 1, true);
            checkEventCount(TestSceneMouseStates.DRAG_END);

            // mouseDown on a draggable -> mouseUp on the original position: no drag-clicking
            AddStep("move mouse", () => InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.TopLeft));
            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            AddStep("drag draggable", () => InputManager.MoveMouseTo(TestScene.OuterMarginBox.ScreenSpaceDrawQuad.BottomRight));
            checkEventCount(TestSceneMouseStates.DRAG_START, 1);
            checkIsDragged(true);
            AddStep("return mouse position", () => InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.TopLeft));
            checkIsDragged(true);
            checkEventCount(TestSceneMouseStates.DRAG_END);
            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.CLICK);
            checkEventCount(TestSceneMouseStates.DRAG_END, 1);
            checkIsDragged(false);

            // mouseDown on a draggable -> mouseUp on a distant position: no drag-clicking
            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            AddStep("drag draggable", () => InputManager.MoveMouseTo(TestScene.MarginBox.ScreenSpaceDrawQuad.BottomRight));
            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.DRAG_START, 1);
            checkEventCount(TestSceneMouseStates.DRAG_END, 1);
            checkEventCount(TestSceneMouseStates.CLICK);
        }

        [Test]
        public void ClickAndDoubleClick()
        {
            initTestScene();

            waitDoubleClickTime();
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.CLICK, 1);
            waitDoubleClickTime();
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.CLICK, 1);
            waitDoubleClickTime();
            AddStep("double click", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
            });
            checkEventCount(TestSceneMouseStates.CLICK, 1);
            checkEventCount(TestSceneMouseStates.DOUBLE_CLICK, 1);
            waitDoubleClickTime();
            AddStep("triple click", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
            });
            checkEventCount(TestSceneMouseStates.CLICK, 2);
            checkEventCount(TestSceneMouseStates.DOUBLE_CLICK, 1);

            waitDoubleClickTime();
            AddStep("click then mouse down", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.PressButton(MouseButton.Left);
            });
            checkEventCount(TestSceneMouseStates.CLICK, 1);
            checkEventCount(TestSceneMouseStates.DOUBLE_CLICK, 1);
            AddStep("mouse up", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.CLICK);
            checkEventCount(TestSceneMouseStates.DOUBLE_CLICK);

            waitDoubleClickTime();
            AddStep("double click drag", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(TestScene.OuterMarginBox.ScreenSpaceDrawQuad.TopLeft);
            });
            checkEventCount(TestSceneMouseStates.CLICK, 1);
            checkEventCount(TestSceneMouseStates.DOUBLE_CLICK, 1);
            checkEventCount(TestSceneMouseStates.DRAG_START, 1);
        }

        [Test]
        public void SeparateMouseDown()
        {
            initTestScene();

            AddStep("right down", () => InputManager.PressButton(MouseButton.Right));
            checkEventCount(TestSceneMouseStates.MOUSE_DOWN, 1);
            AddStep("move away", () => InputManager.MoveMouseTo(TestScene.OuterMarginBox.ScreenSpaceDrawQuad.TopLeft));
            AddStep("left click", () => InputManager.Click(MouseButton.Left));
            checkEventCount(TestSceneMouseStates.MOUSE_DOWN, 1, true);
            checkEventCount(TestSceneMouseStates.MOUSE_UP, 1, true);
            AddStep("right up", () => InputManager.ReleaseButton(MouseButton.Right));
            checkEventCount(TestSceneMouseStates.MOUSE_UP, 1);
        }

        private void waitDoubleClickTime()
        {
            AddWaitStep("wait to don't double click", 2);
        }

        private void checkEventCount(Type type, int change = 0, bool outer = false)
        {
            TestScene.EventCounts1.TryGetValue(type, out var count1);
            TestScene.EventCounts2.TryGetValue(type, out var count2);

            if (outer)
            {
                count1 += change;
            }
            else
            {
                // those types are handled by state tracker 2
                if (!new[] { TestSceneMouseStates.DRAG_START, TestSceneMouseStates.DRAG_END, TestSceneMouseStates.CLICK, TestSceneMouseStates.DOUBLE_CLICK }.Contains(type))
                    count1 += change;
                count2 += change;
            }

            AddAssert($"{type.Name} count {count1}, {count2}", () => TestScene.S1.CounterFor(type).Count == count1 && TestScene.S2.CounterFor(type).Count == count2);

            TestScene.EventCounts1[type] = count1;
            TestScene.EventCounts2[type] = count2;
        }

        private void checkLastPositionDelta(Func<float> expected) => AddAssert("correct position delta", () =>
            Precision.AlmostEquals(TestScene.S1.LastDelta.Length, expected()) &&
            Precision.AlmostEquals(TestScene.S2.LastDelta.Length, expected()));

        private void checkLastScrollDelta(Vector2 expected) => AddAssert("correct scroll delta", () =>
            Precision.AlmostEquals(TestScene.S1.LastScrollDelta, expected) &&
            Precision.AlmostEquals(TestScene.S2.LastScrollDelta, expected));

        private void checkIsDragged(bool isDragged) => AddAssert(isDragged ? "dragged" : "not dragged", () => TestScene.S2.IsDragged == isDragged);
    }
}
