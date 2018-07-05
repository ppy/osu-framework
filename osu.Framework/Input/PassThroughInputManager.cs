// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics;
using OpenTK;

namespace osu.Framework.Input
{
    public class PassThroughInputManager : CustomInputManager, IRequireHighFrequencyMousePosition
    {
        /// <summary>
        /// If there's an InputManager above us, decide whether we should use their available state.
        /// </summary>
        public bool UseParentInput = true;

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
                new MouseButtonInput(args.Button, true).Apply(CurrentState, this);
            return false;
        }

        protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
        {
            if (UseParentInput)
                new MouseButtonInput(args.Button, false).Apply(CurrentState, this);
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
            if (UseParentInput)
                new KeyboardKeyInput(args.Key, true).Apply(CurrentState, this);
            return false;
        }

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args)
        {
            if (UseParentInput)
                new KeyboardKeyInput(args.Key, false).Apply(CurrentState, this);
            return false;
        }

        protected override bool OnJoystickPress(InputState state, JoystickEventArgs args)
        {
            if (UseParentInput)
                new JoystickButtonInput(args.Button, true).Apply(CurrentState, this);
            return false;

        }

        protected override bool OnJoystickRelease(InputState state, JoystickEventArgs args)
        {
            if (UseParentInput)
                new JoystickButtonInput(args.Button, false).Apply(CurrentState, this);
            return false;
        }

        /// <summary>
        /// An input state which allows for transformations to state which don't affect the source state.
        /// </summary>
        public class PassThroughInputState : InputState
        {
            public PassThroughInputState(InputState state)
            {
                Mouse = (state.Mouse.NativeState as MouseState)?.Clone();
                Keyboard = (state.Keyboard as KeyboardState)?.Clone();
                Joystick = (state.Joystick as JoystickState)?.Clone();
            }
        }
    }
}
