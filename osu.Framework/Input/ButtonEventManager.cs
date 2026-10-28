// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Logging;

namespace osu.Framework.Input
{
    /// <summary>
    /// Manages state and events for a single button.
    /// </summary>
    public abstract class ButtonEventManager<TButton>
    {
        internal InputManager InputManager { get; set; } = null!;

        /// <summary>
        /// The button this <see cref="ButtonEventManager{TButton}"/> manages.
        /// </summary>
        public readonly TButton Button;

        /// <summary>
        /// The input queue for propagating button up events.
        /// This is created from <see cref="InputQueue"/> when the button is pressed.
        /// </summary>
        protected List<Drawable>? ButtonDownInputQueue { get; private set; }

        /// <summary>
        /// The input queue.
        /// </summary>
        protected IEnumerable<Drawable> InputQueue => GetInputQueue.Invoke();

        /// <summary>
        /// A function to retrieve the input queue.
        /// </summary>
        internal Func<IEnumerable<Drawable>> GetInputQueue = null!;

        protected ButtonEventManager(TButton button)
        {
            Button = button;
        }

        /// <summary>
        /// Handles the button state changing.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="kind">The type of change in the button's state.</param>
        public void HandleButtonStateChange(InputState state, ButtonStateChangeKind kind)
        {
            if (kind == ButtonStateChangeKind.Pressed)
                handleButtonDown(state);
            else
                handleButtonUp(state);
        }

        /// <summary>
        /// Handles the button being pressed.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <returns>Whether the event was handled.</returns>
        private bool handleButtonDown(InputState state)
        {
            List<Drawable> inputQueue = InputQueue.ToList();
            Drawable? handledBy = HandleButtonDown(state, inputQueue);

            if (handledBy != null)
            {
                // only drawables up to the one that handled mouse down should handle mouse up, so remove all subsequent drawables from the queue (for future use).
                int count = inputQueue.IndexOf(handledBy) + 1;
                inputQueue.RemoveRange(count, inputQueue.Count - count);
            }

            ButtonDownInputQueue = inputQueue;

            return handledBy != null;
        }

        /// <summary>
        /// Handles the button being pressed.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="targets">The list of possible targets that can handle the event.</param>
        /// <returns>The <see cref="Drawable"/> that handled the event.</returns>
        protected abstract Drawable? HandleButtonDown(InputState state, List<Drawable> targets);

        /// <summary>
        /// Handles the button being released.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        private void handleButtonUp(InputState state)
        {
            // in rare cases, a button up event may arrive without a preceding mouse down event.
            // one example of this is an absolute mouse up input from a tablet, which happened when the stylus was positioned
            // outside the bounds of the active tablet area, with confine mouse to window off.
            // it's an awkward configuration and as such it is not exactly clear what should happen in that case,
            // but what should definitely not happen is a crash.
            if (ButtonDownInputQueue == null)
                return;

            HandleButtonUp(state, ButtonDownInputQueue.Where(d => d.IsRootedAt(InputManager)).ToList());
            ButtonDownInputQueue = null;
        }

        /// <summary>
        /// Handles the button being released.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="targets">The list of targets that must handle the event. This will contain targets up to the target that handled the button down event.</param>
        protected abstract void HandleButtonUp(InputState state, List<Drawable> targets);

        /// <summary>
        /// Triggers events on drawables in <paramref name="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="e">The event.</param>
        /// <returns>The drawable which handled the event or null if none.</returns>
        protected Drawable? PropagateButtonEvent(IEnumerable<Drawable> drawables, UIEvent e)
        {
            Drawable? handledBy = null;

            foreach (Drawable target in drawables)
            {
                if (target.TriggerEvent(e))
                {
                    handledBy = target;
                    break;
                }
            }

            if (handledBy != null)
            {
                Logger.Log(SuppressLoggingEventInformation(handledBy)
                    ? $"{e.GetType().Name} handled by {handledBy}."
                    : $"{e} handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return handledBy;
        }

        /// <summary>
        /// Whether information about the event should be suppressed from logging for the given drawable.
        /// </summary>
        protected virtual bool SuppressLoggingEventInformation(Drawable drawable) => false;
    }
}
