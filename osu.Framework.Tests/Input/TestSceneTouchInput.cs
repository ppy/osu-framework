// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Input;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Lists;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Input
{
    [HeadlessTest]
    public class TestSceneTouchInput : ManualInputManagerTestScene
    {
        private PositionalPointer getPointer(MouseButton source, Vector2 position = default) => new PositionalPointer(source, position);

        [Test]
        public void TestSinglePointer()
        {
            addTouchStep(TouchInputKind.Down, MouseButton.Button1, getPointer(MouseButton.Button1, new Vector2(0)));
            addTouchStep(TouchInputKind.Move, MouseButton.Button1, getPointer(MouseButton.Button1, new Vector2(1)));
            addTouchStep(TouchInputKind.Up, pointers: getPointer(MouseButton.Button1));
        }

        [Test]
        public void TestMultiPointers()
        {
            addTouchStep(TouchInputKind.Down, MouseButton.Button1, getPointer(MouseButton.Button1, new Vector2(0, 0)));
            addTouchStep(TouchInputKind.Down, MouseButton.Button2, getPointer(MouseButton.Button2, new Vector2(0, 1)));
            addTouchStep(TouchInputKind.Move, MouseButton.Button2, getPointer(MouseButton.Button1, new Vector2(1, 0)), getPointer(MouseButton.Button2, new Vector2(1, 1)));
            addTouchStep(TouchInputKind.Up, MouseButton.Button1, getPointer(MouseButton.Button2));
            addTouchStep(TouchInputKind.Up, pointers: getPointer(MouseButton.Button1));
        }

        /// <summary>
        /// Adds touch input steps and asserts.
        /// </summary>
        /// <param name="kind">Kind of touch input step.</param>
        /// <param name="expectedPrimary">Expected primary pointer source. (null if there shouldn't)</param>
        /// <param name="pointers">List of pointers to pass onto the <see cref="IInput"/></param>
        private void addTouchStep(TouchInputKind kind, MouseButton? expectedPrimary = null, params PositionalPointer[] pointers)
        {
            TouchState touch() => InputManager.CurrentState.Touch;

            switch (kind)
            {
                case TouchInputKind.Down:
                    AddStep("touch down", () => InputManager.ActivateTouchPointers(pointers));
                    AddAssert("are pointers active", () => pointers.All(p => touch().Pointers.IsPressed(p)));
                    break;

                case TouchInputKind.Up:
                    AddStep("touch up", () => InputManager.DeactivateTouchPointers(pointers));
                    AddAssert("are pointers inactive", () => pointers.All(p => !touch().Pointers.IsPressed(p)));
                    break;

                case TouchInputKind.Move:
                    AddStep("touch move", () => InputManager.MoveTouchPointers(pointers));
                    AddAssert("pointers position changed", () => touch().Pointers.OrderBy(p => p.Source).Intersect(pointers).SequenceEqual(pointers, new PositionEqualityComparer()));
                    break;
            }

            if (expectedPrimary.HasValue)
                AddAssert("is expected primary", () => touch().PrimaryPointer?.Source == expectedPrimary);
            else
                AddAssert("has no primary", () => touch().PrimaryPointer == null);

            // Primary touch pointer acts as a "left mouse button" for compatibility as of now.
            AddAssert($"is left mouse button {(touch().PrimaryPointer.HasValue ? "pressed" : "released")}", () => InputManager.CurrentState.Mouse.IsPressed(MouseButton.Left) == touch().PrimaryPointer.HasValue);
        }

        private enum TouchInputKind
        {
            Down,
            Up,
            Move,
        }

        private class PositionEqualityComparer : FuncEqualityComparer<PositionalPointer>
        {
            public PositionEqualityComparer()
                : base((p1, p2) => p1.Equals(p2) && p1.Position == p2.Position)
            {
            }
        }
    }
}
