// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Logging;

namespace osu.Framework.Input
{
    public class JoystickAxisEventManager
    {
        private readonly JoystickAxisSource source;

        /// <summary>
        /// The input queue.
        /// </summary>
        [NotNull]
        protected IEnumerable<Drawable> InputQueue => GetInputQueue.Invoke() ?? Enumerable.Empty<Drawable>();

        /// <summary>
        /// A function to retrieve the input queue.
        /// </summary>
        internal Func<IEnumerable<Drawable>> GetInputQueue;

        public JoystickAxisEventManager(JoystickAxisSource source)
        {
            this.source = source;
        }

        /// <summary>
        /// Handles axis value changing.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="newValue">The new value for this axis.</param>
        /// <param name="lastValue">The last value for this axis.</param>
        public void HandleAxisChange(InputState state, float newValue, float lastValue)
        {
            PropagateEvent(InputQueue, new JoystickAxisMoveEvent(state, new JoystickAxis(source, newValue), lastValue));
        }

        /// <summary>
        /// Triggers events on drawables in <paramref name="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="e">The event.</param>
        /// <returns>The drawable which handled the event or null if none.</returns>
        protected Drawable PropagateEvent(IEnumerable<Drawable> drawables, UIEvent e)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerEvent(e));

            if (handledBy != null)
                Logger.Log($"{e} handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy;
        }
    }
}
