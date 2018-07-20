// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using OpenTK;

namespace osu.Framework.Input.Handlers.Mouse
{
    internal abstract class OpenTKMouseHandlerBase : InputHandler
    {
        protected GameHost Host;
        protected bool MouseInWindow;

        public override bool Initialize(GameHost host)
        {
            Host = host;

            MouseInWindow = host.Window.CursorInWindow;
            Host.Window.MouseLeave += (s, e) => MouseInWindow = false;
            Host.Window.MouseEnter += (s, e) => MouseInWindow = true;

            return true;
        }

        private Vector2 currentPosition;

        protected void HandleState(OpenTKMouseState state, OpenTKMouseState lastState, bool isAbsolutePosition)
        {
            if (lastState == null || isAbsolutePosition)
            {
                PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = state.Position });
                currentPosition = state.Position;
            }
            else
            {
                var delta = state.Position - lastState.Position;
                if (delta != Vector2.Zero)
                {
                    PendingInputs.Enqueue(new MousePositionRelativeInput { Delta = delta });
                    currentPosition += delta;
                }
            }

            if (lastState != null)
            {
                var scrollDelta = state.Scroll - lastState.Scroll;
                if (scrollDelta != Vector2.Zero)
                {
                    PendingInputs.Enqueue(new MouseScrollRelativeInput { Delta = scrollDelta, IsPrecise = state.HasPreciseScroll });
                }
            }

            PendingInputs.Enqueue(new MouseButtonInput(state.Buttons, lastState?.Buttons));

            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
        }

        /// <summary>
        /// This input handler is always active, handling the cursor position if no other input handler does.
        /// </summary>
        public override bool IsActive => true;

        /// <summary>
        /// Lowest priority. We want the normal mouse handler to only kick in if all other handlers don't do anything.
        /// </summary>
        public override int Priority => 0;
    }
}
