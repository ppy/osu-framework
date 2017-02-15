// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Input;
using osu.Framework.Lists;
using OpenTK;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;
using KeyboardState = osu.Framework.Input.KeyboardState;
using System.Collections.Generic;

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

        /// <summary>
        /// If we are not the current focus, this will force our parent InputManager to reconsider what to focus.
        /// Useful in combination with <see cref="RequestingFocus"/>
        /// Make sure you are already Present (ie. you've run Update at least once after becoming visible). Schedule recommended.
        /// </summary>
        protected void TriggerFocusContention()
        {
            Debug.Assert(IsPresent, @"Calling this without being present is likely a mistake. We may not obtain focus when we expect to.");

            if (ourInputManager.FocusedDrawable != this)
                ourInputManager.ChangeFocus(null);
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

        /// <summary>
        /// Check whether we have active focus. Walks up the drawable tree; use sparingly.
        /// </summary>
        public bool HasFocus => ourInputManager?.FocusedDrawable == this;

        /// <summary>
        /// If true, we are eagerly requesting focus. If nothing else above us has (or is requesting focus) we will get it.
        /// </summary>
        public virtual bool RequestingFocus => false;

        internal bool Hovering;

        /// <summary>
        /// Computes whether a given screen-space position is contained within this drawable.
        /// Mouse input events are only received when this function is true, or when the drawable
        /// is in focus.
        /// </summary>
        /// <param name="screenSpacePos">The screen space position to be checked against this drawable.</param>
        public virtual bool Contains(Vector2 screenSpacePos) => DrawRectangle.Contains(ToLocalSpace(screenSpacePos));

        /// <summary>
        /// Whether this Drawable can receive, taking into account all optimizations and masking.
        /// </summary>
        public bool CanReceiveInput => HandleInput && IsPresent && !IsMaskedAway;

        /// <summary>
        /// Whether this Drawable is hovered by the given screen space mouse position,
        /// taking into account whether this Drawable can receive input.
        /// </summary>
        /// <param name="screenSpaceMousePos">The mouse position to be checked.</param>
        public bool IsHovered(Vector2 screenSpaceMousePos) => CanReceiveInput && Contains(screenSpaceMousePos);

        /// <summary>
        /// Transforms a screen-space input state to the parent's space of this Drawable.
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

        /// <summary>
        /// This method is responsible for building a queue of Drawables to receive keyboard input
        /// in-order. This method is overridden by <see cref="T:Container"/> to be called on all
        /// children such that the entire scene graph is covered.
        /// </summary>
        /// <param name="queue">The input queue to be built.</param>
        /// <returns>Whether we have added ourself to the queue.</returns>
        internal virtual bool BuildKeyboardInputQueue(List<Drawable> queue)
        {
            if (!CanReceiveInput)
                return false;

            queue.Add(this);
            return true;
        }

        /// <summary>
        /// This method is responsible for building a queue of Drawables to receive mouse input
        /// in-order. This method is overridden by <see cref="T:Container"/> to be called on all
        /// children such that the entire scene graph is covered.
        /// </summary>
        /// <param name="screenSpaceMousePos">The current position of the mouse cursor in screen space.</param>
        /// <param name="queue">The input queue to be built.</param>
        /// <returns>Whether we have added ourself to the queue.</returns>
        internal virtual bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue)
        {
            if (!IsHovered(screenSpaceMousePos))
                return false;

            queue.Add(this);
            return true;
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

            public Vector2? PositionMouseDown => NativeState.PositionMouseDown == null ? null : us.Parent?.ToLocalSpace(NativeState.PositionMouseDown.Value) ?? NativeState.PositionMouseDown;
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
