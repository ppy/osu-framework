// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
                    Sync();
            }
        }

        private bool useParentInput = true;

        /// <summary>
        /// Whether to synchronise newly pressed buttons on <see cref="Sync"/>.
        /// Pressed buttons are still synchronised at the point of loading the input manager.
        /// </summary>
        public bool SyncNewPresses { get; init; } = true;

        public override bool HandleHoverEvents => UseParentInput ? parentInputManager.HandleHoverEvents : base.HandleHoverEvents;

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
            {
                pendingInputs.Clear();
            }

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
                    // safe-guard for edge cases.
                    if (!CurrentState.Mouse.IsPressed(mouseDown.Button))
                        new MouseButtonInput(mouseDown.Button, true).Apply(CurrentState, this);
                    break;

                case MouseUpEvent mouseUp:
                    // safe-guard for edge cases.
                    if (CurrentState.Mouse.IsPressed(mouseUp.Button))
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
                    if (!keyDown.Repeat)
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

        private InputManager parentInputManager;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Sync(applyPresses: true);
        }

        protected override void Update()
        {
            base.Update();

            // Some non-positional events are blocked. Sync every frame.
            if (UseParentInput) Sync(true);
        }

        /// <summary>
        /// Sync input state to parent <see cref="InputManager"/>'s <see cref="InputState"/>.
        /// Call this when parent <see cref="InputManager"/> changed somehow.
        /// </summary>
        /// <param name="useCachedParentInputManager">If this is false, assume parent input manager is unchanged from before.</param>
        /// <param name="applyPresses">Whether to also apply press inputs for buttons that are shown to be pressed in the parent input manager.</param>
        public void Sync(bool useCachedParentInputManager = false, bool applyPresses = false)
        {
            if (!UseParentInput) return;

            if (!useCachedParentInputManager)
                parentInputManager = GetContainingInputManager();

            SyncInputState(parentInputManager?.CurrentState, applyPresses);
        }

        /// <summary>
        /// Sync current state to a certain state.
        /// </summary>
        /// <param name="state">The state to synchronise current with. If this is null, it is regarded as an empty state.</param>
        /// <param name="applyPresses">Whether to also apply press inputs for buttons that are shown to be pressed in the parent input manager.</param>
        protected virtual void SyncInputState(InputState state, bool applyPresses)
        {
            var mouseDiff = (state?.Mouse?.Buttons ?? new ButtonStates<MouseButton>()).EnumerateDifference(CurrentState.Mouse.Buttons);
            var keyDiff = (state?.Keyboard.Keys ?? new ButtonStates<Key>()).EnumerateDifference(CurrentState.Keyboard.Keys);
            var touchDiff = (state?.Touch ?? new TouchState()).EnumerateDifference(CurrentState.Touch);
            var joyButtonDiff = (state?.Joystick?.Buttons ?? new ButtonStates<JoystickButton>()).EnumerateDifference(CurrentState.Joystick.Buttons);
            var midiDiff = (state?.Midi?.Keys ?? new ButtonStates<MidiKey>()).EnumerateDifference(CurrentState.Midi.Keys);
            var tabletPenDiff = (state?.Tablet?.PenButtons ?? new ButtonStates<TabletPenButton>()).EnumerateDifference(CurrentState.Tablet.PenButtons);
            var tabletAuxiliaryDiff = (state?.Tablet?.AuxiliaryButtons ?? new ButtonStates<TabletAuxiliaryButton>()).EnumerateDifference(CurrentState.Tablet.AuxiliaryButtons);

            if (mouseDiff.Released.Length > 0)
                new MouseButtonInput(mouseDiff.Released.Select(button => new ButtonInputEntry<MouseButton>(button, false))).Apply(CurrentState, this);
            foreach (var key in keyDiff.Released)
                new KeyboardKeyInput(key, false).Apply(CurrentState, this);
            if (touchDiff.deactivated.Length > 0)
                new TouchInput(touchDiff.deactivated, false).Apply(CurrentState, this);
            foreach (var button in joyButtonDiff.Released)
                new JoystickButtonInput(button, false).Apply(CurrentState, this);
            foreach (var key in midiDiff.Released)
                new MidiKeyInput(key, state?.Midi?.Velocities.GetValueOrDefault(key) ?? 0, false).Apply(CurrentState, this);
            foreach (var button in tabletPenDiff.Released)
                new TabletPenButtonInput(button, false).Apply(CurrentState, this);
            foreach (var button in tabletAuxiliaryDiff.Released)
                new TabletAuxiliaryButtonInput(button, false).Apply(CurrentState, this);

            // Basically only perform the full state diff if we have found that any axis changed.
            // This avoids unnecessary alloc overhead.
            for (int i = 0; i < JoystickState.MAX_AXES; i++)
            {
                if (state?.Joystick?.AxesValues[i] != CurrentState.Joystick.AxesValues[i])
                {
                    new JoystickAxisInput(state?.Joystick?.GetAxes() ?? Array.Empty<JoystickAxis>()).Apply(CurrentState, this);
                    break;
                }
            }

            if (SyncNewPresses || applyPresses)
            {
                if (mouseDiff.Pressed.Length > 0)
                    new MouseButtonInput(mouseDiff.Pressed.Select(button => new ButtonInputEntry<MouseButton>(button, true))).Apply(CurrentState, this);
                foreach (var key in keyDiff.Pressed)
                    new KeyboardKeyInput(key, true).Apply(CurrentState, this);
                if (touchDiff.activated.Length > 0)
                    new TouchInput(touchDiff.activated, true).Apply(CurrentState, this);
                foreach (var button in joyButtonDiff.Pressed)
                    new JoystickButtonInput(button, true).Apply(CurrentState, this);
                foreach (var key in midiDiff.Pressed)
                    new MidiKeyInput(key, state?.Midi?.Velocities.GetValueOrDefault(key) ?? 0, true).Apply(CurrentState, this);
                foreach (var button in tabletPenDiff.Pressed)
                    new TabletPenButtonInput(button, true).Apply(CurrentState, this);
                foreach (var button in tabletAuxiliaryDiff.Pressed)
                    new TabletAuxiliaryButtonInput(button, true).Apply(CurrentState, this);
            }
        }
    }
}
