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
        /// The <see cref="InputEventType"/>s that this <see cref="AndroidInputHandler"/> will handle.
        /// </summary>
        /// <remarks>For each <see cref="InputEventType"/> specified here, this <see cref="AndroidInputHandler"/> should override the appropriate method.</remarks>
        protected abstract IEnumerable<InputEventType> HandledEventTypes { get; }

        /// <summary>
        /// The view that this <see cref="AndroidInputHandler"/> is handling events from.
        /// </summary>
        protected readonly AndroidGameView View;

        /// <summary>
        /// Bitmask of all <see cref="HandledEventSources"/>.
        /// </summary>
        private InputSourceType inputSourceBitmask;

        protected AndroidInputHandler(AndroidGameView view)
        {
            View = view;
        }

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            // compute the bitmask for later use.
            foreach (var inputSource in HandledEventSources)
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags (InputSourceType is a flags enum, but is not properly marked as such)
                inputSourceBitmask |= inputSource;
            }

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    foreach (var eventType in HandledEventTypes)
                        subscribeToEvent(eventType);
                }
                else
                {
                    foreach (var eventType in HandledEventTypes)
                        unsubscribeFromEvent(eventType);
                }
            }, true);

            return true;
        }

        /// <summary>
        /// Invoked on every <see cref="InputEventType.CapturedPointer"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="InputEventType.CapturedPointer"/> must be specified in <see cref="HandledEventTypes"/> to receive events here.
        /// </remarks>
        protected virtual void OnCapturedPointer(MotionEvent capturedPointerEvent)
        {
        }

        /// <summary>
        /// Invoked on every <see cref="InputEventType.GenericMotion"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="InputEventType.GenericMotion"/> must be specified in <see cref="HandledEventTypes"/> to receive events here.
        /// </remarks>
        protected virtual void OnGenericMotion(MotionEvent genericMotionEvent)
        {
        }

        /// <summary>
        /// Invoked on every <see cref="InputEventType.Hover"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="InputEventType.Hover"/> must be specified in <see cref="HandledEventTypes"/> to receive events here.
        /// </remarks>
        protected virtual void OnHover(MotionEvent hoverEvent)
        {
        }

        /// <summary>
        /// Invoked on every <see cref="InputEventType.KeyDown"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="InputEventType.KeyDown"/> must be specified in <see cref="HandledEventTypes"/> to receive events here.
        /// </remarks>
        protected virtual void OnKeyDown(Keycode keycode, KeyEvent e)
        {
        }

        /// <summary>
        /// Invoked on every <see cref="InputEventType.KeyUp"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="InputEventType.KeyUp"/> must be specified in <see cref="HandledEventTypes"/> to receive events here.
        /// </remarks>
        protected virtual void OnKeyUp(Keycode keycode, KeyEvent e)
        {
        }

        /// <summary>
        /// Invoked on every <see cref="InputEventType.Touch"/> event that matches an <see cref="InputSourceType"/> from <see cref="HandledEventSources"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="InputEventType.Touch"/> must be specified in <see cref="HandledEventTypes"/> to receive events here.
        /// </remarks>
        protected virtual void OnTouch(MotionEvent touchEvent)
        {
        }

        #region Event handlers

        private void subscribeToEvent(InputEventType eventType)
        {
            switch (eventType)
            {
                case InputEventType.CapturedPointer:
                    View.CapturedPointer += handleCapturedPointer;
                    break;

                case InputEventType.GenericMotion:
                    View.GenericMotion += handleGenericMotion;
                    break;

                case InputEventType.Hover:
                    View.Hover += handleHover;
                    break;

                case InputEventType.KeyDown:
                    View.KeyDown += handleKeyDown;
                    break;

                case InputEventType.KeyUp:
                    View.KeyUp += handleKeyUp;
                    break;

                case InputEventType.Touch:
                    View.Touch += handleTouch;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, $"Invalid {nameof(InputEventType)}.");
            }
        }

        private void unsubscribeFromEvent(InputEventType eventType)
        {
            switch (eventType)
            {
                case InputEventType.CapturedPointer:
                    View.CapturedPointer -= handleCapturedPointer;
                    break;

                case InputEventType.GenericMotion:
                    View.GenericMotion -= handleGenericMotion;
                    break;

                case InputEventType.Hover:
                    View.Hover -= handleHover;
                    break;

                case InputEventType.KeyDown:
                    View.KeyDown -= handleKeyDown;
                    break;

                case InputEventType.KeyUp:
                    View.KeyUp -= handleKeyUp;
                    break;

                case InputEventType.Touch:
                    View.Touch -= handleTouch;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, $"Invalid {nameof(InputEventType)}.");
            }
        }

        /// <summary>
        /// Checks whether the <paramref name="inputEvent"/> should be handled by this <see cref="AndroidInputHandler"/>.
        /// </summary>
        /// <param name="inputEvent">The <see cref="InputEvent"/> to check.</param>
        /// <returns><c>true</c> if the <paramref name="inputEvent"/>'s <see cref="InputSourceType"/> matches <see cref="HandledEventSources"/>.</returns>
        /// <remarks>Should be checked before handling events.</remarks>
        private bool shouldHandleEvent([NotNullWhen(true)] InputEvent? inputEvent)
        {
            return inputEvent != null && inputSourceBitmask.HasFlagFast(inputEvent.Source);
        }

        private void handleCapturedPointer(object sender, View.CapturedPointerEventArgs e)
        {
            if (shouldHandleEvent(e.Event))
            {
                OnCapturedPointer(e.Event);
                e.Handled = true;
            }
        }

        private void handleGenericMotion(object sender, View.GenericMotionEventArgs e)
        {
            if (shouldHandleEvent(e.Event))
            {
                OnGenericMotion(e.Event);
                e.Handled = true;
            }
        }

        private void handleHover(object sender, View.HoverEventArgs e)
        {
            if (shouldHandleEvent(e.Event))
            {
                OnHover(e.Event);
                e.Handled = true;
            }
        }

        private void handleKeyDown(Keycode keycode, KeyEvent e)
        {
            if (shouldHandleEvent(e))
            {
                OnKeyDown(keycode, e);
            }
        }

        private void handleKeyUp(Keycode keycode, KeyEvent e)
        {
            if (shouldHandleEvent(e))
            {
                OnKeyUp(keycode, e);
            }
        }

        private void handleTouch(object sender, View.TouchEventArgs e)
        {
            if (shouldHandleEvent(e.Event))
            {
                OnTouch(e.Event);
                e.Handled = true;
            }
        }

        #endregion

        /// <summary>
        /// Types of <see cref="InputEvent"/>s that are provided by <see cref="View"/> and <see cref="AndroidGameView"/>.
        /// </summary>
        protected enum InputEventType
        {
            /// <summary>
            /// Event type for <see cref="View.CapturedPointer"/>.
            /// </summary>
            /// <remarks>Relevant method to override: <see cref="OnCapturedPointer"/>.</remarks>
            CapturedPointer,

            /// <summary>
            /// Event type for <see cref="View.GenericMotion"/>.
            /// </summary>
            /// <remarks>Relevant method to override: <see cref="OnGenericMotion"/>.</remarks>
            GenericMotion,

            /// <summary>
            /// Event type for <see cref="View.Hover"/>.
            /// </summary>
            /// <remarks>Relevant method to override: <see cref="OnHover"/>.</remarks>
            Hover,

            /// <summary>
            /// Event type for <see cref="AndroidGameView.KeyDown"/>.
            /// </summary>
            /// <remarks>Relevant method to override: <see cref="OnKeyDown"/>.</remarks>
            KeyDown,

            /// <summary>
            /// Event type for <see cref="AndroidGameView.KeyUp"/>.
            /// </summary>
            /// <remarks>Relevant method to override: <see cref="OnKeyUp"/>.</remarks>
            KeyUp,

            /// <summary>
            /// Event type for <see cref="View.Touch"/>.
            /// </summary>
            /// <remarks>Relevant method to override: <see cref="OnTouch"/>.</remarks>
            Touch
        }
    }
}
