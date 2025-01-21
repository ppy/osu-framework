// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;
using JoystickState = osu.Framework.Input.States.JoystickState;

namespace osu.Framework.Input
{
    /// <summary>An <see cref="InputManager"/> with an ability to use parent <see cref="InputManager"/>'s input.</summary>
    /// <remarks>
    /// There are two modes of operation for this class:
    /// When <see cref="UseParentInput"/> is false, this input manager gets inputs only from own input handlers.
    /// When <see cref="UseParentInput"/> is true, this input manager ignore any local input handlers and
    /// gets inputs from the parent (its ancestor in the scene graph) <see cref="InputManager"/> in the following way:
    /// For mouse input, this only considers input that is passed as events such as <see cref="MouseDownEvent"/>.
    /// For keyboard and other inputs, this input manager try to reflect parent <see cref="InputManager"/>'s <see cref="InputState"/> closely as possible.
    /// Thus, when this is attached to the scene graph initially and when <see cref="UseParentInput"/> becomes true,
    /// multiple events may fire to synchronise the local <see cref="InputState"/> with the parent's.
    /// </remarks>
    public partial class PassThroughInputManager : CustomInputManager, IRequireHighFrequencyMousePosition
    {
        private bool useParentInput = true;

        /// <summary>
        /// If there's an InputManager above us, decide whether we should use their available state.
        /// </summary>
        public virtual bool UseParentInput
        {
            get => useParentInput;
            set
            {
                if (useParentInput == value) return;

                useParentInput = value;

                if (UseParentInput)
                {
                    syncReleasedInputs();
                    syncJoystickAxes();
                }
            }
        }

        private InputManager? parentInputManager;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            parentInputManager = GetContainingInputManager();
        }

        public override bool HandleHoverEvents => parentInputManager != null && UseParentInput ? parentInputManager.HandleHoverEvents : base.HandleHoverEvents;

        internal override bool BuildNonPositionalInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!PropagateNonPositionalInputSubTree) return false;

            if (!allowBlocking)
                base.BuildNonPositionalInputQueue(queue, false);
            else
                queue.Add(this);

            return false;
        }

        internal override bool BuildPositionalInputQueue(Vector2 screenSpacePos, List<Drawable> queue)
        {
            if (!PropagatePositionalInputSubTree) return false;

            queue.Add(this);
            return false;
        }

        protected override List<IInput> GetPendingInputs()
        {
            //we still want to call the base method to clear any pending states that may build up.
            var pendingInputs = base.GetPendingInputs();

            if (UseParentInput)
                pendingInputs.Clear();

            return pendingInputs;
        }

        protected override bool Handle(UIEvent e)
        {
            if (!UseParentInput) return false;

            // Don't handle mouse events sourced from touches, we may have a
            // child drawable handling actual touches, we will produce one ourselves.
            if (e is MouseEvent && e.CurrentState.Mouse.LastSource is ISourcedFromTouch)
                return false;

            // Synthesize pen inputs from pen events
            if (e is MouseEvent && e.CurrentState.Mouse.LastSource is ISourcedFromPen penInput)
            {
                switch (e)
                {
                    case MouseDownEvent penDown:
                        Debug.Assert(penDown.Button == MouseButton.Left);
                        new MouseButtonInputFromPen(true) { DeviceType = penInput.DeviceType }.Apply(CurrentState, this);
                        return false;

                    case MouseUpEvent penUp:
                        Debug.Assert(penUp.Button == MouseButton.Left);
                        new MouseButtonInputFromPen(false) { DeviceType = penInput.DeviceType }.Apply(CurrentState, this);
                        return false;

                    case MouseMoveEvent penMove:
                        if (penMove.ScreenSpaceMousePosition != CurrentState.Mouse.Position)
                        {
                            new MousePositionAbsoluteInputFromPen
                            {
                                Position = penMove.ScreenSpaceMousePosition,
                                DeviceType = penInput.DeviceType
                            }.Apply(CurrentState, this);
                        }

                        return false;
                }
            }

            switch (e)
            {
                case MouseDownEvent mouseDown:
                    new MouseButtonInput(mouseDown.Button, true).Apply(CurrentState, this);
                    break;

                case MouseUpEvent mouseUp:
                    new MouseButtonInput(mouseUp.Button, false).Apply(CurrentState, this);
                    break;

                case MouseMoveEvent mouseMove:
                    if (mouseMove.ScreenSpaceMousePosition != CurrentState.Mouse.Position)
                        new MousePositionAbsoluteInput { Position = mouseMove.ScreenSpaceMousePosition }.Apply(CurrentState, this);
                    break;

                case ScrollEvent scroll:
                    new MouseScrollRelativeInput { Delta = scroll.ScrollDelta, IsPrecise = scroll.IsPrecise }.Apply(CurrentState, this);
                    break;

                case KeyDownEvent keyDown:
                    if (keyDown.Repeat)
                        return false;

                    new KeyboardKeyInput(keyDown.Key, true).Apply(CurrentState, this);
                    break;

                case KeyUpEvent keyUp:
                    new KeyboardKeyInput(keyUp.Key, false).Apply(CurrentState, this);
                    break;

                case TouchEvent touch:
                    new TouchInput(touch.ScreenSpaceTouch, touch.IsActive(touch.ScreenSpaceTouch)).Apply(CurrentState, this);
                    break;

                case JoystickPressEvent joystickPress:
                    new JoystickButtonInput(joystickPress.Button, true).Apply(CurrentState, this);
                    break;

                case JoystickReleaseEvent joystickRelease:
                    new JoystickButtonInput(joystickRelease.Button, false).Apply(CurrentState, this);
                    break;

                case JoystickAxisMoveEvent joystickAxisMove:
                    new JoystickAxisInput(joystickAxisMove.Axis).Apply(CurrentState, this);
                    break;

                case MidiDownEvent midiDown:
                    new MidiKeyInput(midiDown.Key, midiDown.Velocity, true).Apply(CurrentState, this);
                    break;

                case MidiUpEvent midiUp:
                    new MidiKeyInput(midiUp.Key, midiUp.Velocity, false).Apply(CurrentState, this);
                    break;

                case TabletPenButtonPressEvent tabletPenButtonPress:
                    new TabletPenButtonInput(tabletPenButtonPress.Button, true).Apply(CurrentState, this);
                    break;

                case TabletPenButtonReleaseEvent tabletPenButtonRelease:
                    new TabletPenButtonInput(tabletPenButtonRelease.Button, false).Apply(CurrentState, this);
                    break;

                case TabletAuxiliaryButtonPressEvent tabletAuxiliaryButtonPress:
                    new TabletAuxiliaryButtonInput(tabletAuxiliaryButtonPress.Button, true).Apply(CurrentState, this);
                    break;

                case TabletAuxiliaryButtonReleaseEvent tabletAuxiliaryButtonRelease:
                    new TabletAuxiliaryButtonInput(tabletAuxiliaryButtonRelease.Button, false).Apply(CurrentState, this);
                    break;
            }

            return false;
        }

        protected override void Update()
        {
            base.Update();

            // There are scenarios wherein we cannot receive the release events of pressed inputs. For simplicity, sync every frame.
            if (UseParentInput)
            {
                syncReleasedInputs();
                syncJoystickAxes();
            }
        }

        /// <summary>
        /// Updates state of any buttons that have been released by parent while <see cref="UseParentInput"/> was disabled.
        /// </summary>
        private void syncReleasedInputs()
        {
            if (parentInputManager == null)
                return;

            var parentState = parentInputManager.CurrentState;
            var mouseDiff = (parentState?.Mouse?.Buttons ?? new ButtonStates<MouseButton>()).EnumerateDifference(CurrentState.Mouse.Buttons);
            var keyDiff = (parentState?.Keyboard.Keys ?? new ButtonStates<Key>()).EnumerateDifference(CurrentState.Keyboard.Keys);
            var touchDiff = (parentState?.Touch ?? new TouchState()).EnumerateDifference(CurrentState.Touch);
            var joyButtonDiff = (parentState?.Joystick?.Buttons ?? new ButtonStates<JoystickButton>()).EnumerateDifference(CurrentState.Joystick.Buttons);
            var midiDiff = (parentState?.Midi?.Keys ?? new ButtonStates<MidiKey>()).EnumerateDifference(CurrentState.Midi.Keys);
            var tabletPenDiff = (parentState?.Tablet?.PenButtons ?? new ButtonStates<TabletPenButton>()).EnumerateDifference(CurrentState.Tablet.PenButtons);
            var tabletAuxiliaryDiff = (parentState?.Tablet?.AuxiliaryButtons ?? new ButtonStates<TabletAuxiliaryButton>()).EnumerateDifference(CurrentState.Tablet.AuxiliaryButtons);

            if (mouseDiff.Released.Length > 0)
                new MouseButtonInput(mouseDiff.Released.Select(button => new ButtonInputEntry<MouseButton>(button, false))).Apply(CurrentState, this);
            foreach (var key in keyDiff.Released)
                new KeyboardKeyInput(key, false).Apply(CurrentState, this);
            if (touchDiff.deactivated.Length > 0)
                new TouchInput(touchDiff.deactivated, false).Apply(CurrentState, this);
            foreach (var button in joyButtonDiff.Released)
                new JoystickButtonInput(button, false).Apply(CurrentState, this);
            foreach (var key in midiDiff.Released)
                new MidiKeyInput(key, parentState?.Midi?.Velocities.GetValueOrDefault(key) ?? 0, false).Apply(CurrentState, this);
            foreach (var button in tabletPenDiff.Released)
                new TabletPenButtonInput(button, false).Apply(CurrentState, this);
            foreach (var button in tabletAuxiliaryDiff.Released)
                new TabletAuxiliaryButtonInput(button, false).Apply(CurrentState, this);
        }

        /// <summary>
        /// Updates state of joystick axes that have changed values while <see cref="UseParentInput"/> was disabled.
        /// </summary>
        private void syncJoystickAxes()
        {
            if (parentInputManager == null)
                return;

            var parentState = parentInputManager.CurrentState;

            // Basically only perform the full state diff if we have found that any axis changed.
            // This avoids unnecessary alloc overhead.
            for (int i = 0; i < JoystickState.MAX_AXES; i++)
            {
                if (parentState?.Joystick?.AxesValues[i] != CurrentState.Joystick.AxesValues[i])
                {
                    new JoystickAxisInput(parentState?.Joystick?.GetAxes() ?? Array.Empty<JoystickAxis>()).Apply(CurrentState, this);
                    break;
                }
            }
        }
    }
}
