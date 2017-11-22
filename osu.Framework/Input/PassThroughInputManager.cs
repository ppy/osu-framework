// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics;
using OpenTK;

namespace osu.Framework.Input
{
    public class PassThroughInputManager : CustomInputManager, IHandleMouseMove, IHandleMouseButtons, IHandleKeys
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
                pendingStates.Add(new PassThroughInputState(s));

            pendingParentStates.Clear();

            return pendingStates;
        }

        private readonly List<InputState> pendingParentStates = new List<InputState>();

        private bool acceptState(InputState state)
        {
            if (UseParentState)
                pendingParentStates.Add(state);
            return false;
        }

        public virtual bool OnMouseMove(InputState state) => acceptState(state);

        public virtual bool OnMouseDown(InputState state, MouseDownEventArgs args) => acceptState(state);

        public virtual bool OnMouseUp(InputState state, MouseUpEventArgs args) => acceptState(state);
        public virtual bool OnClick(InputState state) => false;

        public virtual bool OnDoubleClick(InputState state) => false;

        public virtual bool OnKeyDown(InputState state, KeyDownEventArgs args) => acceptState(state);

        public virtual bool OnKeyUp(InputState state, KeyUpEventArgs args) => acceptState(state);

        /// <summary>
        /// An input state which allows for transformations to state which don't affect the source state.
        /// </summary>
        public class PassThroughInputState : InputState
        {
            public PassThroughInputState(InputState state)
            {
                Mouse = (state.Mouse.NativeState as MouseState)?.Clone();
                Keyboard = (state.Keyboard as KeyboardState)?.Clone();
            }
        }
    }
}
