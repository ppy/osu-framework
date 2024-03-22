// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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
                    syncReleasedButtons();
            }
        }

        private InputManager? parentInputManager;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            parentInputManager = GetContainingInputManager();
            applyInitialState();
        }

        protected override void Update()
        {
            base.Update();

            // this is usually not needed because we're guaranteed to receive release events as long as we have received press events beforehand.
            // however, we sync our state with parent input manager on load, and we cannot receive release events for those inputs,
            // therefore we're forced to check every update frame.
            if (UseParentInput)
                syncReleasedButtons();
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

                case TabletAuxiliaryButtonReleaseEvent tabletAuxiliaryButtonPress:
                    new TabletAuxiliaryButtonInput(tabletAuxiliaryButtonPress.Button, true).Apply(CurrentState, this);
                    break;
            }

            return false;
        }

        private void applyInitialState()
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

            // we should not read mouse button presses from parent as the source of those presses may originate from touch.
            // e.g. if a parent input manager does not have a drawable handling touch and transforms touch1 to left mouse,
            // then this input manager shouldn't apply left mouse, as it may have a drawable handling touch. this is covered in tests.

            foreach (var key in keyDiff.Pressed)
                new KeyboardKeyInput(key, true).Apply(CurrentState, this);
            if (touchDiff.deactivated.Length > 0)
                new TouchInput(touchDiff.deactivated, true).Apply(CurrentState, this);
            foreach (var button in joyButtonDiff.Pressed)
                new JoystickButtonInput(button, true).Apply(CurrentState, this);
            foreach (var key in midiDiff.Pressed)
                new MidiKeyInput(key, parentState?.Midi?.Velocities.GetValueOrDefault(key) ?? 0, true).Apply(CurrentState, this);
            foreach (var button in tabletPenDiff.Pressed)
                new TabletPenButtonInput(button, true).Apply(CurrentState, this);
            foreach (var button in tabletAuxiliaryDiff.Pressed)
                new TabletAuxiliaryButtonInput(button, true).Apply(CurrentState, this);

            syncJoystickAxes();
        }

        private void syncReleasedButtons()
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

            syncJoystickAxes();
        }

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
