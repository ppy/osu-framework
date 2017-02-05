// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Input;
using osu.Framework.Lists;
using OpenTK;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;
using KeyboardState = osu.Framework.Input.KeyboardState;

namespace osu.Framework.Graphics
{
    public partial class Drawable : IDisposable, IHasLifetime
    {
        /// <summary>
        /// Find the first parent InputManager which this drawable is contained by.
        /// </summary>
        private InputManager ourInputManager => this as InputManager ?? (Parent as Drawable)?.ourInputManager;

        public bool TriggerHover(InputState screenSpaceState) => OnHover(toParentSpace(screenSpaceState));

        protected virtual bool OnHover(InputState state) => false;

        public void TriggerHoverLost(InputState screenSpaceState) => OnHoverLost(toParentSpace(screenSpaceState));

        protected virtual void OnHoverLost(InputState state)
        {
        }

        public bool TriggerMouseDown(InputState screenSpaceState = null, MouseDownEventArgs args = null) => OnMouseDown(toParentSpace(screenSpaceState), args);

        protected virtual bool OnMouseDown(InputState state, MouseDownEventArgs args) => false;

        public bool TriggerMouseUp(InputState screenSpaceState = null, MouseUpEventArgs args = null) => OnMouseUp(toParentSpace(screenSpaceState), args);

        protected virtual bool OnMouseUp(InputState state, MouseUpEventArgs args) => false;

        public bool TriggerClick(InputState screenSpaceState = null) => OnClick(toParentSpace(screenSpaceState));

        protected virtual bool OnClick(InputState state) => false;

        public bool TriggerDoubleClick(InputState screenSpaceState) => OnDoubleClick(toParentSpace(screenSpaceState));

        protected virtual bool OnDoubleClick(InputState state) => false;

        public bool TriggerDragStart(InputState screenSpaceState) => OnDragStart(toParentSpace(screenSpaceState));

        protected virtual bool OnDragStart(InputState state) => false;

        public bool TriggerDrag(InputState screenSpaceState) => OnDrag(toParentSpace(screenSpaceState));

        protected virtual bool OnDrag(InputState state) => false;

        public bool TriggerDragEnd(InputState screenSpaceState) => OnDragEnd(toParentSpace(screenSpaceState));

        protected virtual bool OnDragEnd(InputState state) => false;

        public bool TriggerWheel(InputState screenSpaceState) => OnWheel(toParentSpace(screenSpaceState));

        protected virtual bool OnWheel(InputState state) => false;

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
        /// <param name="screenSpaceState">The input state.</param>
        /// <param name="checkCanFocus">Whether we should check this Drawable's OnFocus returns true before actually providing focus.</param>
        public bool TriggerFocus(InputState screenSpaceState = null, bool checkCanFocus = false)
        {
            if (HasFocus)
                return true;

            if (!IsPresent)
                return false;

            if (checkCanFocus & !OnFocus(toParentSpace(screenSpaceState)))
                return false;

            ourInputManager?.ChangeFocus(this);

            return true;
        }

        protected virtual bool OnFocus(InputState state) => false;

        /// <summary>
        /// Unfocuses this drawable.
        /// </summary>
        /// <param name="screenSpaceState">The input state.</param>
        /// <param name="isCallback">Used to aavoid cyclid recursion.</param>
        public void TriggerFocusLost(InputState screenSpaceState = null, bool isCallback = false)
        {
            if (!HasFocus)
                return;

            if (screenSpaceState == null)
                screenSpaceState = new InputState { Keyboard = new KeyboardState(), Mouse = new MouseState() };

            if (!isCallback) ourInputManager.ChangeFocus(null);
            OnFocusLost(toParentSpace(screenSpaceState));
        }

        protected virtual void OnFocusLost(InputState state)
        {
        }

        public bool TriggerKeyDown(InputState screenSpaceState, KeyDownEventArgs args) => OnKeyDown(toParentSpace(screenSpaceState), args);

        protected virtual bool OnKeyDown(InputState state, KeyDownEventArgs args) => false;

        public bool TriggerKeyUp(InputState screenSpaceState, KeyUpEventArgs args) => OnKeyUp(toParentSpace(screenSpaceState), args);

        protected virtual bool OnKeyUp(InputState state, KeyUpEventArgs args) => false;

        public bool TriggerMouseMove(InputState screenSpaceState) => OnMouseMove(toParentSpace(screenSpaceState));

        protected virtual bool OnMouseMove(InputState state) => false;

        /// <summary>
        /// This drawable only receives input events if HandleInput is true.
        /// </summary>
        public virtual bool HandleInput => false;

        public virtual bool HasFocus => ourInputManager?.FocusedDrawable == this;

        internal bool Hovering;

        /// <summary>
        /// Computes whether a given screen-space position is contained within this drawable.
        /// Mouse input events are only received when this function is true, or when the drawable
        /// is in focus.
        /// </summary>
        /// <param name="screenSpacePos">The screen space position to be checked against this drawable.</param>
        /// <returns>Whether the given position is contained within this drawable.</returns>
        public virtual bool Contains(Vector2 screenSpacePos) => DrawRectangle.Contains(ToLocalSpace(screenSpacePos));

        /// <summary>
        /// Transforms a screen-space input state to the parent's space of this drawable.
        /// </summary>
        /// <param name="screenSpaceState">The screen-space input state to be transformed.</param>
        /// <returns>The transformed state in parent space.</returns>
        private InputState toParentSpace(InputState screenSpaceState)
        {
            if (screenSpaceState == null) return null;

            return new InputState
            {
                Keyboard = screenSpaceState.Keyboard,
                Mouse = new LocalMouseState(screenSpaceState.Mouse, this)
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
