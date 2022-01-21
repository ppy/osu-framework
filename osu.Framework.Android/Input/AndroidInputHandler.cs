// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Android.Views;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;

#nullable enable

namespace osu.Framework.Android.Input
{
    /// <summary>
    /// Base input handler for handling events dispatched by <see cref="AndroidGameView"/>.
    /// Provides consistent and unified means for handling <see cref="InputEvent"/>s only from specific <see cref="InputSourceType"/>s.
    /// </summary>
    public abstract class AndroidInputHandler : InputHandler
    {
        /// <summary>
        /// The <see cref="InputSourceType"/>s that this <see cref="AndroidInputHandler"/> will handle.
        /// </summary>
        protected abstract IEnumerable<InputSourceType> HandledEventSources { get; }

        /// <summary>
        /// The view that this <see cref="AndroidInputHandler"/> is handling events from.
        /// </summary>
        protected readonly AndroidGameView View;

        /// <summary>
        /// Bitmask of all <see cref="HandledEventSources"/>.
        /// </summary>
        private InputSourceType eventSourceBitmask;

        protected AndroidInputHandler(AndroidGameView view)
        {
            View = view;
        }

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            // compute the bitmask for later use.
            foreach (var eventSource in HandledEventSources)
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                // (InputSourceType is a flags enum, but is not properly marked as such)
                eventSourceBitmask |= eventSource;
            }

            return true;
        }

        /// <summary>
        /// Invoked on every <see cref="AndroidGameView.CapturedPointer"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// Subscribe <see cref="HandleCapturedPointer"/> to <see cref="View"/>.<see cref="AndroidGameView.CapturedPointer"/> to receive events here.
        /// </remarks>
        protected virtual void OnCapturedPointer(MotionEvent capturedPointerEvent)
        {
            throw new NotSupportedException($"{nameof(HandleCapturedPointer)} subscribed to {nameof(View.CapturedPointer)} but the relevant method was not overriden.");
        }

        /// <summary>
        /// Invoked on every <see cref="AndroidGameView.GenericMotion"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// Subscribe <see cref="HandleGenericMotion"/> to <see cref="View"/>.<see cref="AndroidGameView.GenericMotion"/> to receive events here.
        /// </remarks>
        protected virtual void OnGenericMotion(MotionEvent genericMotionEvent)
        {
            throw new NotSupportedException($"{nameof(HandleGenericMotion)} subscribed to {nameof(View.GenericMotion)} but the relevant method was not overriden.");
        }

        /// <summary>
        /// Invoked on every <see cref="AndroidGameView.Hover"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// Subscribe <see cref="HandleHover"/> to <see cref="View"/>.<see cref="AndroidGameView.Hover"/> to receive events here.
        /// </remarks>
        protected virtual void OnHover(MotionEvent hoverEvent)
        {
            throw new NotSupportedException($"{nameof(HandleHover)} subscribed to {nameof(View.Hover)} but the relevant method was not overriden.");
        }

        /// <summary>
        /// Invoked on every <see cref="AndroidGameView.KeyDown"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// Subscribe <see cref="HandleKeyDown"/> to <see cref="View"/>.<see cref="AndroidGameView.KeyDown"/> to receive events here.
        /// </remarks>
        protected virtual void OnKeyDown(Keycode keycode, KeyEvent e)
        {
            throw new NotSupportedException($"{nameof(HandleKeyDown)} subscribed to {nameof(View.KeyDown)} but the relevant method was not overriden.");
        }

        /// <summary>
        /// Invoked on every <see cref="AndroidGameView.KeyUp"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// Subscribe <see cref="HandleKeyUp"/> to <see cref="View"/>.<see cref="AndroidGameView.KeyUp"/> to receive events here.
        /// </remarks>
        protected virtual void OnKeyUp(Keycode keycode, KeyEvent e)
        {
            throw new NotSupportedException($"{nameof(HandleKeyUp)} subscribed to {nameof(View.KeyUp)} but the relevant method was not overriden.");
        }

        /// <summary>
        /// Invoked on every <see cref="AndroidGameView.Touch"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// Subscribe <see cref="HandleTouch"/> to <see cref="View"/>.<see cref="AndroidGameView.Touch"/> to receive events here.
        /// </remarks>
        protected virtual void OnTouch(MotionEvent touchEvent)
        {
            throw new NotSupportedException($"{nameof(HandleTouch)} subscribed to {nameof(View.Touch)} but the relevant method was not overriden.");
        }

        #region Event handlers

        /// <summary>
        /// Checks whether the <paramref name="inputEvent"/> should be handled by this <see cref="AndroidInputHandler"/>.
        /// </summary>
        /// <param name="inputEvent">The <see cref="InputEvent"/> to check.</param>
        /// <returns><c>true</c> if the <paramref name="inputEvent"/>'s <see cref="InputSourceType"/> matches <see cref="HandledEventSources"/>.</returns>
        /// <remarks>Should be checked before handling events.</remarks>
        private bool shouldHandleEvent([NotNullWhen(true)] InputEvent? inputEvent)
        {
            return inputEvent != null && eventSourceBitmask.HasFlagFast(inputEvent.Source);
        }

        /// <summary>
        /// Handler for <see cref="AndroidGameView.CapturedPointer"/> events.
        /// </summary>
        protected void HandleCapturedPointer(object sender, View.CapturedPointerEventArgs e)
        {
            if (shouldHandleEvent(e.Event))
            {
                OnCapturedPointer(e.Event);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handler for <see cref="AndroidGameView.GenericMotion"/> events.
        /// </summary>
        protected void HandleGenericMotion(object sender, View.GenericMotionEventArgs e)
        {
            if (shouldHandleEvent(e.Event))
            {
                OnGenericMotion(e.Event);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handler for <see cref="AndroidGameView.Hover"/> events.
        /// </summary>
        protected void HandleHover(object sender, View.HoverEventArgs e)
        {
            if (shouldHandleEvent(e.Event))
            {
                OnHover(e.Event);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handler for <see cref="AndroidGameView.KeyDown"/> events.
        /// </summary>
        protected void HandleKeyDown(Keycode keycode, KeyEvent e)
        {
            if (shouldHandleEvent(e))
            {
                OnKeyDown(keycode, e);
            }
        }

        /// <summary>
        /// Handler for <see cref="AndroidGameView.KeyUp"/> events.
        /// </summary>
        protected void HandleKeyUp(Keycode keycode, KeyEvent e)
        {
            if (shouldHandleEvent(e))
            {
                OnKeyUp(keycode, e);
            }
        }

        /// <summary>
        /// Handler for <see cref="AndroidGameView.Touch"/> events.
        /// </summary>
        protected void HandleTouch(object sender, View.TouchEventArgs e)
        {
            if (shouldHandleEvent(e.Event))
            {
                OnTouch(e.Event);
                e.Handled = true;
            }
        }

        #endregion
    }
}
