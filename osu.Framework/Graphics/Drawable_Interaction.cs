// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Input;
using osu.Framework.Lists;
using OpenTK;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;

namespace osu.Framework.Graphics
{
    public partial class Drawable : IDisposable, IHasLifetime
    {
        /// <summary>
        /// Find the first parent InputManager which this drawable is contained by.
        /// </summary>
        private InputManager ourInputManager => this as InputManager ?? Parent?.ourInputManager;

        public bool TriggerHover(InputState state)
        {
            return OnHover(state);
        }

        protected virtual bool OnHover(InputState state)
        {
            return false;
        }

        internal void TriggerHoverLost(InputState state)
        {
            OnHoverLost(state);
        }

        protected virtual void OnHoverLost(InputState state)
        {
        }

        public bool TriggerMouseDown(InputState state = null, MouseDownEventArgs args = null) => OnMouseDown(getLocalState(state), args);

        protected virtual bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            return false;
        }

        public bool TriggerMouseUp(InputState state = null, MouseUpEventArgs args = null) => OnMouseUp(getLocalState(state), args);

        protected virtual bool OnMouseUp(InputState state, MouseUpEventArgs args)
        {
            return false;
        }

        public bool TriggerClick(InputState state = null) => OnClick(getLocalState(state));

        protected virtual bool OnClick(InputState state)
        {
            return false;
        }

        public bool TriggerDoubleClick(InputState state) => OnDoubleClick(getLocalState(state));

        protected virtual bool OnDoubleClick(InputState state)
        {
            return false;
        }

        public bool TriggerDragStart(InputState state) => OnDragStart(getLocalState(state));

        protected virtual bool OnDragStart(InputState state)
        {
            return false;
        }

        public bool TriggerDrag(InputState state) => OnDrag(getLocalState(state));

        protected virtual bool OnDrag(InputState state)
        {
            return false;
        }

        public bool TriggerDragEnd(InputState state) => OnDragEnd(getLocalState(state));

        protected virtual bool OnDragEnd(InputState state)
        {
            return false;
        }

        public bool TriggerWheelUp(InputState state) => OnWheelUp(getLocalState(state));

        protected virtual bool OnWheelUp(InputState state)
        {
            return false;
        }

        public bool TriggerWheelDown(InputState state) => OnWheelDown(getLocalState(state));

        protected virtual bool OnWheelDown(InputState state)
        {
            return false;
        }

        /// <summary>
        /// Focuses this drawable.
        /// </summary>
        /// <param name="state">The input state.</param>
        /// <param name="checkCanFocus">Whether we should check this Drawable's OnFocus returns true before actually providing focus.</param>
        public bool TriggerFocus(InputState state = null, bool checkCanFocus = false)
        {
            if (HasFocus)
                return true;

            if (checkCanFocus & !OnFocus(state))
                return false;

            ourInputManager?.ChangeFocus(this);

            return true;
        }

        protected virtual bool OnFocus(InputState state)
        {
            return false;
        }

        /// <summary>
        /// Unfocuses this drawable.
        /// </summary>
        /// <param name="state">The input state.</param>
        /// <param name="isCallback">Used to aavoid cyclid recursion.</param>
        public void TriggerFocusLost(InputState state = null, bool isCallback = false)
        {
            if (!HasFocus)
                return;

            if (state == null)
                state = new InputState();

            if (!isCallback) ourInputManager.ChangeFocus(null);
            OnFocusLost(state);
        }

        protected virtual void OnFocusLost(InputState state)
        {
        }

        public bool TriggerKeyDown(InputState state, KeyDownEventArgs args) => OnKeyDown(getLocalState(state), args);

        protected virtual bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            return false;
        }

        public bool TriggerKeyUp(InputState state, KeyUpEventArgs args) => OnKeyUp(getLocalState(state), args);

        protected virtual bool OnKeyUp(InputState state, KeyUpEventArgs args)
        {
            return false;
        }

        public bool TriggerMouseMove(InputState state) => OnMouseMove(getLocalState(state));

        protected virtual bool OnMouseMove(InputState state)
        {
            return false;
        }

        public virtual bool HandleInput => false;

        public virtual bool HasFocus => ourInputManager?.FocusedDrawable == this;

        internal bool Hovering;

        /// <summary>
        /// Convert a position to the local coordinate system from either native or local to another drawable.
        /// </summary>
        /// <param name="screenSpacePos">The input position.</param>
        /// <param name="relativeDrawable">The drawable which the input position is local to, or null for native.</param>
        /// <returns>The output position.</returns>
        internal Vector2 GetLocalPosition(Vector2 screenSpacePos, Drawable relativeDrawable = null)
        {
            if (relativeDrawable != null)
                screenSpacePos *= relativeDrawable.DrawInfo.Matrix;
            return screenSpacePos * DrawInfo.MatrixInverse;
        }

        internal Vector2 GetLocalDelta(Vector2 delta)
        {
            Vector3 scale = DrawInfo.Matrix.ExtractScale();
            return new Vector2(delta.X * scale.X, delta.Y * scale.Y);
        }

        public virtual bool Contains(Vector2 screenSpacePos)
        {
            return ScreenSpaceInputQuad.Contains(screenSpacePos);
        }

        private InputState getLocalState(InputState state)
        {
            if (state == null) return null;

            return new InputState
            {
                Keyboard = state.Keyboard,
                Mouse = new LocalMouseState(state.Mouse, this)
            };
        }

        private struct LocalMouseState : IMouseState
        {
            private readonly IMouseState state;
            private readonly Drawable us;

            public LocalMouseState(IMouseState state, Drawable us)
            {
                this.state = state;
                this.us = us;
            }

            public bool BackButton => state.BackButton;
            public bool ForwardButton => state.ForwardButton;
            public Vector2 Delta => us.GetLocalDelta(state.Delta);
            public Vector2 Position => us.GetLocalPosition(state.Position);
            public Vector2? PositionMouseDown => state.PositionMouseDown == null ? (Vector2?)null : us.GetLocalPosition(state.PositionMouseDown.Value);
            public bool HasMainButtonPressed => state.HasMainButtonPressed;
            public bool LeftButton => state.LeftButton;
            public bool MiddleButton => state.MiddleButton;
            public bool RightButton => state.RightButton;
        }
    }

    public class KeyDownEventArgs : EventArgs
    {
        public Key Key;
        public bool Repeat;
    }

    public class MouseUpEventArgs : MouseEventArgs
    {
    }

    public class MouseDownEventArgs : MouseEventArgs
    {
    }

    public class MouseEventArgs : EventArgs
    {
        public MouseButton Button;
    }

    public class KeyUpEventArgs : EventArgs
    {
        public Key Key;
    }

    public delegate bool MouseEventHandlerDelegate(object sender, InputState state);

    internal delegate bool KeyDownEventHandlerDelegate(object sender, KeyDownEventArgs e, InputState state);

    internal delegate bool KeyUpEventHandlerDelegate(object sender, KeyUpEventArgs e, InputState state);
}
