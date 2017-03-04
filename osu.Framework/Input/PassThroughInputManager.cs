// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics;
using OpenTK;

namespace osu.Framework.Input
{
    public class PassThroughInputManager : InputManager
    {
        /// <summary>
        /// If there's an InputManager above us, decide whether we should use their available state.
        /// </summary>
        public bool UseParentState = true;

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue)
        {
            if (!CanReceiveInput) return false;

            if (UseParentState)
                queue.Add(this);
            return false;
        }

        internal override bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue)
        {
            if (!CanReceiveInput) return false;

            if (UseParentState)
                queue.Add(this);
            return false;
        }

        protected override List<InputState> GetPendingStates()
        {
            //we still want to call the base method to clear any pending states that may build up.
            var pendingStates = base.GetPendingStates();

            if (!UseParentState)
                return pendingStates;

            pendingStates.Clear();

            foreach (var s in pendingParentStates)
                pendingStates.Add(new InputState
                {
                    Mouse = s.Mouse.NativeState,
                    Keyboard = s.Keyboard
                });

            pendingParentStates.Clear();

            return pendingStates;
        }

        List<InputState> pendingParentStates = new List<InputState>();

        private bool acceptState(InputState state)
        {
            if (UseParentState)
                pendingParentStates.Add(state);
            return false;
        }

        protected override bool OnMouseMove(InputState state) => acceptState(state);

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args) => acceptState(state);

        protected override bool OnMouseUp(InputState state, MouseUpEventArgs args) => acceptState(state);

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args) => acceptState(state);

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args) => acceptState(state);
    }
}