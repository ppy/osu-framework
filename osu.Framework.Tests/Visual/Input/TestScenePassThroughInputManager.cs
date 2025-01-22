// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public partial class TestScenePassThroughInputManager : ManualInputManagerTestScene
    {
        public TestScenePassThroughInputManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        private TestInputManager testInputManager;
        private InputState state;
        private ButtonStates<MouseButton> mouse;
        private ButtonStates<Key> keyboard;
        private ButtonStates<JoystickButton> joystick;

        private void addTestInputManagerStep()
        {
            AddStep("Add InputManager", () =>
            {
                testInputManager = new TestInputManager();
                Add(testInputManager);
                state = testInputManager.CurrentState;
                mouse = state.Mouse.Buttons;
                keyboard = state.Keyboard.Keys;
                joystick = state.Joystick.Buttons;
            });
        }

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
            ChildrenEnumerable = Enumerable.Empty<Drawable>();
        });

        [Test]
        public void UseParentInputChange()
        {
            addTestInputManagerStep();
            AddStep("Press buttons", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.PressKey(Key.A);
                InputManager.PressJoystickButton(JoystickButton.Button1);
            });
            AddAssert("pressed", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddAssert("still pressed", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.IsPressed(JoystickButton.Button1));
            AddStep("Release on parent", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("doen't affect child", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("all synced", () => !mouse.IsPressed(MouseButton.Left) && !keyboard.IsPressed(Key.A) && !joystick.IsPressed(JoystickButton.Button1));
        }

        [Test]
        public void TestMouseDownNoSync()
        {
            addTestInputManagerStep();
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("not pressed", () => !mouse.IsPressed(MouseButton.Left));
        }

        [Test]
        public void TestNoMouseUp()
        {
            addTestInputManagerStep();
            AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("Release and press", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("pressed", () => mouse.IsPressed(MouseButton.Left));
            AddAssert("mouse up count == 0", () => testInputManager.Status.MouseUpCount == 0);
        }

        [Test]
        public void TestKeyInput()
        {
            addTestInputManagerStep();
            AddStep("press key", () => InputManager.PressKey(Key.A));
            AddAssert("key pressed", () => testInputManager.CurrentState.Keyboard.Keys.Single() == Key.A);

            AddStep("release key", () => InputManager.ReleaseKey(Key.A));
            AddAssert("key released", () => !testInputManager.CurrentState.Keyboard.Keys.HasAnyButtonPressed);

            AddStep("press key", () => InputManager.PressKey(Key.A));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("release key", () => InputManager.ReleaseKey(Key.A));
            AddStep("press another key", () => InputManager.PressKey(Key.B));

            AddAssert("only first key pressed", () => testInputManager.CurrentState.Keyboard.Keys.Single() == Key.A);

            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("key released", () => !testInputManager.CurrentState.Keyboard.Keys.HasAnyButtonPressed);
        }

        [Test]
        public void TestPressKeyThenReleaseWhileDisabled()
        {
            addTestInputManagerStep();
            AddStep("press key", () => InputManager.PressKey(Key.A));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("release key", () => InputManager.ReleaseKey(Key.A));
            AddStep("press key again", () => InputManager.PressKey(Key.A));
            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddStep("release key", () => InputManager.ReleaseKey(Key.A));
            AddAssert("key released", () => !testInputManager.CurrentState.Keyboard.Keys.HasAnyButtonPressed);

            AddStep("press key", () => InputManager.PressKey(Key.A));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);

            AddStep("add blocking layer", () => Add(new HandlingBox
            {
                RelativeSizeAxes = Axes.Both,
                OnHandle = _ => true,
            }));

            // with a blocking layer existing, the next key press will not be seen by PassThroughInputManager...
            AddStep("release key", () => InputManager.ReleaseKey(Key.A));
            AddStep("press key again", () => InputManager.PressKey(Key.A));

            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);

            // ...but ensure it'll still release the key regardless of not seeing the corresponding press event (it does that by syncing releases every frame).
            AddStep("release key", () => InputManager.ReleaseKey(Key.A));
            AddAssert("key released", () => !testInputManager.CurrentState.Keyboard.Keys.HasAnyButtonPressed);
        }

        [Test]
        public void TestTouchInput()
        {
            addTestInputManagerStep();
            AddStep("begin first touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, Vector2.Zero)));
            AddAssert("first touch active", () =>
                testInputManager.CurrentState.Touch.ActiveSources.Single() == TouchSource.Touch1 &&
                testInputManager.CurrentState.Touch.TouchPositions[(int)TouchSource.Touch1] == Vector2.Zero);

            AddStep("end first touch", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, Vector2.Zero)));
            AddAssert("first touch not active", () => !testInputManager.CurrentState.Touch.ActiveSources.HasAnyButtonPressed);

            AddStep("begin first touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, Vector2.Zero)));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("end first touch", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, Vector2.Zero)));
            AddStep("begin second touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch2, Vector2.One)));

            AddAssert("only first touch active", () =>
                testInputManager.CurrentState.Touch.ActiveSources.Single() == TouchSource.Touch1 &&
                testInputManager.CurrentState.Touch.TouchPositions[(int)TouchSource.Touch1] == Vector2.Zero);

            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("no touches active", () => !testInputManager.CurrentState.Touch.ActiveSources.HasAnyButtonPressed);
        }

        [Test]
        public void TestMidiInput()
        {
            addTestInputManagerStep();

            AddStep("press C3", () => InputManager.PressMidiKey(MidiKey.C3, 70));
            AddAssert("C3 pressed", () =>
                testInputManager.CurrentState.Midi.Keys.IsPressed(MidiKey.C3)
                && testInputManager.CurrentState.Midi.Velocities[MidiKey.C3] == 70);

            AddStep("release C3", () => InputManager.ReleaseMidiKey(MidiKey.C3, 40));
            AddAssert("C3 released", () => !testInputManager.CurrentState.Midi.Keys.HasAnyButtonPressed);

            AddStep("press C3", () => InputManager.PressMidiKey(MidiKey.C3, 70));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("release C3", () => InputManager.ReleaseMidiKey(MidiKey.C3, 40));
            AddStep("press F#3", () => InputManager.PressMidiKey(MidiKey.FSharp3, 65));

            AddAssert("only C3 pressed", () =>
                testInputManager.CurrentState.Midi.Keys.Single() == MidiKey.C3
                && testInputManager.CurrentState.Midi.Velocities[MidiKey.C3] == 70);

            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("C3 released", () => !testInputManager.CurrentState.Midi.Keys.HasAnyButtonPressed);
        }

        [Test]
        public void TestTabletButtonInput()
        {
            addTestInputManagerStep();

            AddStep("press primary pen button", () => InputManager.PressTabletPenButton(TabletPenButton.Primary));
            AddStep("press auxiliary button 4", () => InputManager.PressTabletAuxiliaryButton(TabletAuxiliaryButton.Button4));
            AddAssert("primary pen button pressed", () => testInputManager.CurrentState.Tablet.PenButtons.Single() == TabletPenButton.Primary);
            AddAssert("auxiliary button 4 pressed", () => testInputManager.CurrentState.Tablet.AuxiliaryButtons.Single() == TabletAuxiliaryButton.Button4);

            AddStep("release primary pen button", () => InputManager.ReleaseTabletPenButton(TabletPenButton.Primary));
            AddStep("release auxiliary button 4", () => InputManager.ReleaseTabletAuxiliaryButton(TabletAuxiliaryButton.Button4));
            AddAssert("primary pen button pressed", () => !testInputManager.CurrentState.Tablet.PenButtons.HasAnyButtonPressed);
            AddAssert("auxiliary button 4 pressed", () => !testInputManager.CurrentState.Tablet.AuxiliaryButtons.HasAnyButtonPressed);

            AddStep("press primary pen button", () => InputManager.PressTabletPenButton(TabletPenButton.Primary));
            AddStep("press auxiliary button 4", () => InputManager.PressTabletAuxiliaryButton(TabletAuxiliaryButton.Button4));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("release primary pen button", () => InputManager.ReleaseTabletPenButton(TabletPenButton.Primary));
            AddStep("release auxiliary button 4", () => InputManager.ReleaseTabletAuxiliaryButton(TabletAuxiliaryButton.Button4));
            AddStep("press secondary pen button", () => InputManager.PressTabletPenButton(TabletPenButton.Secondary));
            AddStep("press auxiliary button 2", () => InputManager.PressTabletAuxiliaryButton(TabletAuxiliaryButton.Button2));

            AddAssert("only primary pen button pressed", () => testInputManager.CurrentState.Tablet.PenButtons.Single() == TabletPenButton.Primary);
            AddAssert("only auxiliary button 4 pressed", () => testInputManager.CurrentState.Tablet.AuxiliaryButtons.Single() == TabletAuxiliaryButton.Button4);

            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("primary pen button released", () => !testInputManager.CurrentState.Tablet.PenButtons.HasAnyButtonPressed);
            AddAssert("auxiliary button 4 released", () => !testInputManager.CurrentState.Tablet.AuxiliaryButtons.HasAnyButtonPressed);
        }

        [Test]
        public void TestMouseTouchProductionOnPassThrough()
        {
            addTestInputManagerStep();
            AddStep("setup hierarchy", () =>
            {
                Add(new HandlingBox
                {
                    Alpha = 0.5f,
                    Depth = 1,
                    RelativeSizeAxes = Axes.Both,
                    OnHandle = e => e is MouseEvent,
                });

                testInputManager.Add(new HandlingBox
                {
                    Alpha = 0.5f,
                    RelativeSizeAxes = Axes.Both,
                    OnHandle = e => e is TouchEvent,
                });
            });

            AddStep("begin touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, testInputManager.ScreenSpaceDrawQuad.Centre)));
            AddAssert("ensure parent manager produced mouse", () =>
                InputManager.CurrentState.Mouse.Buttons.Single() == MouseButton.Left &&
                InputManager.CurrentState.Mouse.Position == testInputManager.ScreenSpaceDrawQuad.Centre);

            AddAssert("pass-through did not produce mouse", () =>
                !testInputManager.CurrentState.Mouse.Buttons.HasAnyButtonPressed &&
                testInputManager.CurrentState.Mouse.Position != testInputManager.ScreenSpaceDrawQuad.Centre);

            AddStep("end touch", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, testInputManager.ScreenSpaceDrawQuad.Centre)));

            AddStep("press mouse", () => InputManager.PressButton(MouseButton.Left));
            AddAssert("pass-through handled mouse", () => testInputManager.CurrentState.Mouse.Buttons.Single() == MouseButton.Left);
        }

        [Test]
        public void TestPenInputPassThrough()
        {
            MouseBox outer = null!;
            MouseBox inner = null!;

            addTestInputManagerStep();
            AddStep("setup hierarchy", () =>
            {
                Add(outer = new MouseBox
                {
                    Alpha = 0.5f,
                    Depth = 1,
                    RelativeSizeAxes = Axes.Both,
                });

                testInputManager.Add(inner = new MouseBox
                {
                    Alpha = 0.5f,
                    RelativeSizeAxes = Axes.Both,
                });
            });

            AddStep("move pen to box", () => InputManager.MovePenTo(testInputManager));

            AddAssert("ensure parent manager produced mouse", () => InputManager.CurrentState.Mouse.Position == testInputManager.ScreenSpaceDrawQuad.Centre);
            AddAssert("ensure pass-through produced mouse", () => testInputManager.CurrentState.Mouse.Position == testInputManager.ScreenSpaceDrawQuad.Centre);

            AddAssert("outer box received 1 pen event", () => outer.PenEvents, () => Is.EqualTo(1));
            AddAssert("outer box received no mouse events", () => outer.MouseEvents, () => Is.EqualTo(0));

            AddAssert("inner box received 1 pen event", () => inner.PenEvents, () => Is.EqualTo(1));
            AddAssert("inner box received no mouse events", () => inner.MouseEvents, () => Is.EqualTo(0));

            AddStep("press pen", () => InputManager.PressPen());

            AddAssert("ensure parent manager produced mouse", () => InputManager.CurrentState.Mouse.Buttons.Single() == MouseButton.Left);
            AddAssert("ensure pass-through produced mouse", () => testInputManager.CurrentState.Mouse.Buttons.Single() == MouseButton.Left);

            AddAssert("outer box received 2 pen events", () => outer.PenEvents, () => Is.EqualTo(2));
            AddAssert("outer box received no mouse events", () => outer.MouseEvents, () => Is.EqualTo(0));

            AddAssert("inner box received 2 pen events", () => inner.PenEvents, () => Is.EqualTo(2));
            AddAssert("inner box received no mouse events", () => inner.MouseEvents, () => Is.EqualTo(0));

            AddStep("release pen", () => InputManager.ReleasePen());

            AddAssert("ensure parent manager produced mouse", () => InputManager.CurrentState.Mouse.Buttons.HasAnyButtonPressed, () => Is.False);
            AddAssert("ensure pass-through produced mouse", () => testInputManager.CurrentState.Mouse.Buttons.HasAnyButtonPressed, () => Is.False);

            AddAssert("outer box received 3 pen events", () => outer.PenEvents, () => Is.EqualTo(3));
            AddAssert("outer box received no mouse events", () => outer.MouseEvents, () => Is.EqualTo(0));

            AddAssert("inner box received 3 pen events", () => inner.PenEvents, () => Is.EqualTo(3));
            AddAssert("inner box received no mouse events", () => inner.MouseEvents, () => Is.EqualTo(0));
        }

        public partial class TestInputManager : ManualInputManager
        {
            public readonly TestSceneInputManager.ContainingInputManagerStatusText Status;

            public TestInputManager()
            {
                Size = new Vector2(0.8f);
                Origin = Anchor.Centre;
                Anchor = Anchor.Centre;
                Child = Status = new TestSceneInputManager.ContainingInputManagerStatusText();
            }
        }

        public partial class HandlingBox : Box
        {
            public Func<UIEvent, bool> OnHandle;

            protected override bool Handle(UIEvent e) => OnHandle?.Invoke(e) ?? false;
        }

        public partial class MouseBox : Box
        {
            public int MouseEvents { get; private set; }
            public int PenEvents { get; private set; }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                switch (e.CurrentState.Mouse.LastSource)
                {
                    case ISourcedFromPen:
                        PenEvents++;
                        break;

                    default:
                        MouseEvents++;
                        break;
                }

                return base.OnMouseMove(e);
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                switch (e.CurrentState.Mouse.LastSource)
                {
                    case ISourcedFromPen:
                        PenEvents++;
                        break;

                    default:
                        MouseEvents++;
                        break;
                }

                return base.OnMouseDown(e);
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                switch (e.CurrentState.Mouse.LastSource)
                {
                    case ISourcedFromPen:
                        PenEvents++;
                        break;

                    default:
                        MouseEvents++;
                        break;
                }

                base.OnMouseUp(e);
            }
        }
    }
}
