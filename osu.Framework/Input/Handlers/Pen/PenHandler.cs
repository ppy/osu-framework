// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Platform.SDL3;
using osu.Framework.Statistics;
using osuTK;

namespace osu.Framework.Input.Handlers.Pen
{
    /// <summary>
    /// SDL3 pen handler
    /// </summary>
    public class PenHandler : InputHandler
    {
        private static readonly GlobalStatistic<ulong> statistic_total_events = GlobalStatistics.Get<ulong>(StatisticGroupFor<PenHandler>(), "Total events");

        public BindableDouble Sensitivity { get; } = new BindableDouble(1)
        {
            MinValue = 0.1,
            MaxValue = 10,
            Precision = 0.01
        };

        public override bool IsActive => true;

        private SDL3Window window = null!;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (host.Window is not SDL3Window sdlWindow)
                return false;

            window = sdlWindow;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    window.PenMove += handlePenMove;
                    window.PenTouch += handlePenTouch;
                    window.PenButton += handlePenButton;
                }
                else
                {
                    window.PenMove -= handlePenMove;
                    window.PenTouch -= handlePenTouch;
                    window.PenButton -= handlePenButton;
                }
            }, true);

            return true;
        }

        public override void Reset()
        {
            Sensitivity.SetDefault();
            base.Reset();
        }

        private void handlePenMove(TabletPenDeviceType deviceType, Vector2 position, bool pressed)
        {
            if (pressed && deviceType == TabletPenDeviceType.Direct)
                enqueueInput(new TouchInput(new Input.Touch(TouchSource.PenTouch, position), true));
            else
                enqueueInput(new MousePositionAbsoluteInputFromPen { DeviceType = deviceType, Position = applySensitivity(position) });
        }

        private void handlePenTouch(TabletPenDeviceType deviceType, bool pressed, Vector2 position)
        {
            if (deviceType == TabletPenDeviceType.Direct)
                enqueueInput(new TouchInput(new Input.Touch(TouchSource.PenTouch, position), pressed));
            else
                enqueueInput(new MouseButtonInputFromPen(pressed) { DeviceType = deviceType });
        }

        private void handlePenButton(TabletPenButton button, bool pressed)
        {
            enqueueInput(new TabletPenButtonInput(button, pressed));
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.TabletEvents);
            statistic_total_events.Value++;
        }

        private Vector2 windowPosition()
        {
            var position = window.Position;
            return new Vector2(position.X, position.Y);
        }

        private Vector2 currentDisplayPosition()
        {
            var position = window.CurrentDisplayBindable.Value.Bounds.Location;
            return new Vector2(position.X, position.Y);
        }

        private Vector2 currentDisplaySize()
        {
            var size = window.CurrentDisplayBindable.Value.Bounds.Size;
            return new Vector2(size.Width, size.Height);
        }

        /// <summary>
        /// Converts a position relative to the top-left corner of the window to a position relative to the top-left corner of the current display.
        /// </summary>
        /// <param name="positionInWindow">A position relative to the top-left corner of the window.</param>
        /// <returns>The same position, but relative to the top-left corner of the current display.</returns>
        private Vector2 windowToCurrentDisplay(Vector2 positionInWindow)
        {
            return windowPosition() - currentDisplayPosition() + positionInWindow;
        }

        /// <summary>
        /// Gets the delta needed to apply sensitivity.
        /// </summary>
        /// <param name="positionInCurrentDisplay">A position relative to the top-left corner of the current display.</param>
        /// <returns>A vector that can be added to a position to apply sensitivity.</returns>
        private Vector2 getSensitivityDelta(Vector2 positionInCurrentDisplay)
        {
            var displayCentre = currentDisplaySize() * 0.5f;
            var relativeToCentre = positionInCurrentDisplay - displayCentre;
            return relativeToCentre * (float)(Sensitivity.Value - 1);
        }

        /// <summary>
        /// Applies sensitivity to a pixel position relative to the top-left corner of the window.
        /// </summary>
        /// <param name="position">A pixel position relative to the top-left corner of the window.</param>
        /// <returns>A pixel position relative to the top-left corner of the window with sensitivity applied.</returns>
        private Vector2 applySensitivity(Vector2 position)
        {
            var delta = getSensitivityDelta(windowToCurrentDisplay(position / window.Scale)) * window.Scale;
            return position + delta;
        }
    }
}
