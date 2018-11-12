// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using OpenTK;
using OpenTK.Input;

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
    public class PassThroughInputManager : CustomInputManager, IRequireHighFrequencyMousePosition
    {
        /// <summary>
        /// If there's an InputManager above us, decide whether we should use their available state.
        /// </summary>
        public bool UseParentInput
        {
            get => useParentInput;
            set
            {
                if (useParentInput == value) return;
                useParentInput = value;

                if (UseParentInput)
                {
                    parentInputManager = GetContainingInputManager();

                    parentInputUpdatedInternal(parentInputManager.CurrentState);

                    parentInputManager.InputUpdated += parentInputUpdated;
                }
                else if (parentInputManager != null)
                {
                    parentInputManager.InputUpdated -= parentInputUpdated;
                    parentInputManager = null;
                }
            }
        }

        private bool useParentInput = true;

        internal override bool BuildNonPositionalInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!PropagateNonPositionalInputSubTree) return false;

            if (!allowBlocking)
            {
                base.BuildNonPositionalInputQueue(queue, false);
                return false;
            }

            if (UseParentInput)
                queue.Add(this);
            return false;
        }

        internal override bool BuildPositionalInputQueue(Vector2 screenSpacePos, List<Drawable> queue)
        {
            if (!PropagatePositionalInputSubTree) return false;

            if (UseParentInput)
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

            switch (e)
            {
                case MouseMoveEvent mouseMove:
                    new MousePositionAbsoluteInput { Position = mouseMove.ScreenSpaceMousePosition }.Apply(CurrentState, this);
                    break;

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

                case ScrollEvent scroll:
                    new MouseScrollRelativeInput { Delta = scroll.ScrollDelta, IsPrecise = scroll.IsPrecise }.Apply(CurrentState, this);
                    break;

                case KeyboardEvent _:
                case JoystickButtonEvent _:
                    parentInputUpdatedInternal(e.CurrentState);
                    break;
            }

            return false;
        }

        private InputManager parentInputManager;

        private void parentInputUpdated(InputState inputState, Drawable drawable)
        {
            if (drawable == null || drawable.GetContainingInputManager() == this)
                parentInputUpdatedInternal(inputState);
        }

        private void parentInputUpdatedInternal(InputState inputState)
        {
            var mouseButtonDifference = (inputState?.Mouse?.Buttons ?? new ButtonStates<MouseButton>()).EnumerateDifference(CurrentState.Mouse.Buttons);
            new MouseButtonInput(mouseButtonDifference.Released.Select(button => new ButtonInputEntry<MouseButton>(button, false))).Apply(CurrentState, this);

            new KeyboardKeyInput(inputState?.Keyboard?.Keys, CurrentState.Keyboard.Keys).Apply(CurrentState, this);
            new JoystickButtonInput(inputState?.Joystick?.Buttons, CurrentState.Joystick.Buttons).Apply(CurrentState, this);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (parentInputManager != null)
                parentInputManager.InputUpdated -= parentInputUpdated;

            base.Dispose(isDisposing);
        }
    }
}
