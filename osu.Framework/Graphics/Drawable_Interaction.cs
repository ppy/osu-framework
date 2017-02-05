// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Input;
using osu.Framework.Lists;
using OpenTK;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;
using KeyboardState = osu.Framework.Input.KeyboardState;
using osu.Framework.Graphics.Primitives;
using System.Diagnostics;

namespace osu.Framework.Graphics
{
    public partial class Drawable : IDisposable, IHasLifetime
    {
        /// <summary>
        /// Find the first parent InputManager which this drawable is contained by.
        /// </summary>
        private InputManager ourInputManager => this as InputManager ?? (Parent as Drawable)?.ourInputManager;

        public bool TriggerHover(InputState state)
        {
            return OnHover(state);
        }

        protected virtual bool OnHover(InputState state)
        {
            return false;
        }

        public void TriggerHoverLost(InputState state)
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

        public bool TriggerWheel(InputState state) => OnWheel(getLocalState(state));

        protected virtual bool OnWheel(InputState state)
        {
            return false;
        }

        /// <summary>
        /// Request focus, but only receive if nothing else already has focus.
        /// </summary>
        /// <returns>Whether we received focus.</returns>
        protected bool RequestFocus()
        {
            if (ourInputManager.FocusedDrawable != null) return false;

            return TriggerFocus();
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

            if (!IsPresent)
                return false;

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
                state = new InputState { Keyboard = new KeyboardState(), Mouse = new MouseState() };

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

        public virtual bool Contains(Vector2 screenSpacePos)
        {
            return DrawRectangle.Contains(ToLocalSpace(screenSpacePos));
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
            public IMouseState NativeState { get; }

            private readonly Drawable us;

            public LocalMouseState(IMouseState state, Drawable us)
            {
                NativeState = state;
                this.us = us;
            }

            public bool BackButton => NativeState.BackButton;
            public bool ForwardButton => NativeState.ForwardButton;

            public Vector2 Delta => Position - LastPosition;

            public Vector2 Position => us.Parent?.ToLocalSpace(NativeState.Position) ?? NativeState.Position;

            public Vector2 LastPosition => us.Parent?.ToLocalSpace(NativeState.LastPosition) ?? NativeState.LastPosition;

            public Vector2? PositionMouseDown => NativeState.PositionMouseDown == null ? (Vector2?)null : us.Parent?.ToLocalSpace(NativeState.PositionMouseDown.Value) ?? NativeState.PositionMouseDown;
            public bool HasMainButtonPressed => NativeState.HasMainButtonPressed;
            public bool LeftButton => NativeState.LeftButton;
            public bool MiddleButton => NativeState.MiddleButton;
            public bool RightButton => NativeState.RightButton;
            public int Wheel => NativeState.Wheel;
            public int WheelDelta => NativeState.WheelDelta;
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
}
