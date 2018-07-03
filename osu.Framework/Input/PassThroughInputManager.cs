// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics;
using OpenTK;

namespace osu.Framework.Input
{
    /// <summary>An <see cref="InputManager"/> with an arbirity to use parent <see cref="InputManager"/>'s input.</summary>
    /// <remarks>
    /// This <see cref="InputManager"/> can be used in one of two states.
    /// When <see cref="UseParentInput"/> is false, this input manager gets inputs only from own input handlers.
    /// When <see cref="UseParentInput"/> becomes false, all pressed buttons and keys are released.
    /// When <see cref="UseParentInput"/> is true, this input manager ignore own input handlers and
    /// gets inputs from the parent (an ancestor in the scene graph) <see cref="InputManager"/> in the following way:
    /// For mouse input, this only considers input that is passed as events such as <see cref="OnMouseDown"/>.
    /// For keyboard and other inputs, this input manager try to reflect parent <see cref="InputManager"/>'s <see cref="InputState"/> closely as possible.
    /// Thus, when this is attached to the scene graph initially and when <see cref="UseParentInput"/> is changed to true from false,
    /// multiple keyboard events is called when the parent's <see cref="InputState"/> is a state with keys pressed for example.
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
                    Sync();
                else
                    Reset();
            }
        }

        private bool useParentInput = true;

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!CanReceiveKeyboardInput) return false;

            if (!allowBlocking)
            {
                base.BuildKeyboardInputQueue(queue, false);
                return false;
            }

            if (UseParentInput)
                queue.Add(this);
            return false;
        }

        internal override bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue)
        {
            if (!CanReceiveMouseInput) return false;

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

        protected override bool OnMouseMove(InputState state)
        {
            if (UseParentInput)
                new MousePositionAbsoluteInput { Position = state.Mouse.NativeState.Position }.Apply(CurrentState, this);
            return false;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            if (UseParentInput)
            {
                // safe-guard for edge cases.
                if (!CurrentState.Mouse.IsPressed(args.Button))
                    new MouseButtonInput(args.Button, true).Apply(CurrentState, this);
            }
            return false;
        }

        protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
        {
            if (UseParentInput)
            {
                // safe-guard for edge cases.
                if (!CurrentState.Mouse.IsPressed(args.Button))
                    new MouseButtonInput(args.Button, true).Apply(CurrentState, this);
                new MouseButtonInput(args.Button, false).Apply(CurrentState, this);
            }

            return false;
        }

        protected override bool OnScroll(InputState state)
        {
            if (UseParentInput)
                new MouseScrollRelativeInput { Delta = state.Mouse.NativeState.ScrollDelta, IsPrecise = state.Mouse.HasPreciseScroll }.Apply(CurrentState, this);
            return false;
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (UseParentInput) SyncInputState(state);
            return false;
        }

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args)
        {
            if (UseParentInput) SyncInputState(state);
            return false;
        }

        protected override bool OnJoystickPress(InputState state, JoystickEventArgs args)
        {
            if (UseParentInput) SyncInputState(state);
            return false;
        }

        protected override bool OnJoystickRelease(InputState state, JoystickEventArgs args)
        {
            if (UseParentInput) SyncInputState(state);
            return false;
        }

        private InputManager parentInputManager;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Sync();
        }

        protected override void Update()
        {
            base.Update();
            // Some keyboard/joystick events are blocked. Sync every frame.
            if (UseParentInput) Sync(true);
        }

        /// <summary>
        /// Sync input state to parent <see cref="InputManager"/>'s <see cref="InputState"/>.
        /// Call this when parent <see cref="InputManager"/> changed somehow.
        /// </summary>
        /// <param name="useCachedParentInputManager">If this is false, assume parent input manager is unchanged from before.</param>
        public void Sync(bool useCachedParentInputManager = false)
        {
            if (!UseParentInput) return;
            if (!useCachedParentInputManager)
                parentInputManager = GetContainingInputManager();
            SyncInputState(parentInputManager?.CurrentState);
        }

        /// <summary>
        /// Sync keyboard and joystick state to parent state.
        /// </summary>
        /// <param name="parentState">Parent's state. If this is null, it is regarded as an empty state.</param>
        protected virtual void SyncInputState(InputState parentState)
        {
            new KeyboardKeyInput(parentState?.Keyboard?.Keys, CurrentState.Keyboard.Keys).Apply(CurrentState, this);
            new JoystickButtonInput(parentState?.Joystick?.Buttons, CurrentState.Joystick.Buttons).Apply(CurrentState, this);
        }

        /// <summary>
        /// Reset all input state.
        /// </summary>
        protected virtual void Reset()
        {
            new MouseButtonInput(null, CurrentState.Mouse.Buttons).Apply(CurrentState, this);
            new KeyboardKeyInput(null, CurrentState.Keyboard.Keys).Apply(CurrentState, this);
            new JoystickButtonInput(null, CurrentState.Joystick.Buttons).Apply(CurrentState, this);
        }
    }
}
