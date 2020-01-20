// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
    public abstract class ButtonEventManager
    {
        /// <summary>
        /// The input queue for propagating button up events.
        /// This is created from <see cref="InputQueue"/> when the the button is pressed.
        /// </summary>
        [CanBeNull]
        protected List<Drawable> ButtonDownInputQueue { get; private set; }

        /// <summary>
        /// The input queue.
        /// </summary>
        [NotNull]
        protected IEnumerable<Drawable> InputQueue => GetInputQueue.Invoke() ?? Enumerable.Empty<Drawable>();

        /// <summary>
        /// A function to retrieve the input queue.
        /// </summary>
        internal Func<IEnumerable<Drawable>> GetInputQueue;

        /// <summary>
        /// Handles the button state changing.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="kind">The type of change in the button's state.</param>
        public void HandleButtonStateChange(InputState state, ButtonStateChangeKind kind)
        {
            if (kind == ButtonStateChangeKind.Pressed)
                HandleButtonDown(state);
            else
                HandleButtonUp(state);
        }

        /// <summary>
        /// Handles the button being pressed.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <returns>Whether the event was handled.</returns>
        public bool HandleButtonDown(InputState state)
        {
            List<Drawable> inputQueue = InputQueue.ToList();
            Drawable handledBy = HandleButtonDownInternal(state, inputQueue);

            if (handledBy != null)
            {
                var count = inputQueue.IndexOf(handledBy) + 1;
                inputQueue.RemoveRange(count, inputQueue.Count - count);
            }

            // only drawables up to the one that handled mouse down should handle mouse up
            ButtonDownInputQueue = inputQueue;

            return handledBy != null;
        }

        /// <summary>
        /// Handles the button being pressed.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="targets">The list of possible targets that can handle the event.</param>
        /// <returns>The <see cref="Drawable"/> that handled the event.</returns>
        protected abstract Drawable HandleButtonDownInternal(InputState state, List<Drawable> targets);

        /// <summary>
        /// Handles the button being released.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        public void HandleButtonUp(InputState state)
        {
            HandleButtonUpInternal(state, ButtonDownInputQueue);
            ButtonDownInputQueue = null;
        }

        /// <summary>
        /// Handles the button being released.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="targets">The list of targets that must handle the event.</param>
        protected abstract void HandleButtonUpInternal(InputState state, List<Drawable> targets);

        /// <summary>
        /// Triggers events on drawables in <paramref name="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="e">The event.</param>
        /// <returns>The drawable which handled the event or null if none.</returns>
        protected Drawable PropagateButtonEvent(IEnumerable<Drawable> drawables, UIEvent e)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerEvent(e));

            if (handledBy != null)
                Logger.Log($"{e} handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy;
        }
    }
}
