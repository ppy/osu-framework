// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.Touchpad
{
    /// <summary>
    /// For <see cref="IWindow"/> implementing <see cref="IHasTouchpadInput"/>. Translate the touchpad events to mouse events.
    /// </summary>
    public class TouchpadHandler : InputHandler, IHasCursorSensitivity
    {
        private static readonly GlobalStatistic<ulong> statistic_total_events = GlobalStatistics.Get<ulong>(StatisticGroupFor<TouchpadHandler>(), "Total events");

        public override string Description => "Touchpad";

        public override bool IsActive => true;

        public BindableDouble Sensitivity { get; } = new BindableDouble(1)
        {
            MinValue = 1,
            MaxValue = 10,
            Precision = 0.01
        };

        private IHasTouchpadInput? window;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (host.Window is not IHasTouchpadInput hasTouchpadInput)
                return false;

            window = hasTouchpadInput;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    window!.TouchpadDataUpdate += handleTouchpadUpdate;
                }
                else
                {
                    window!.TouchpadDataUpdate -= handleTouchpadUpdate;
                }
            }, true);

            return true;
        }

        public override void Reset()
        {
            Sensitivity.SetDefault();
            base.Reset();
        }

        private void handleTouchpadUpdate(TouchpadData data)
        {
            // We just use the first reported point (For PoC).
            // This might not be the first finger touched.
            foreach (var point in data.Points)
            {
                if (!point.Valid || !point.Confidence) continue;

                var position = mapToWindow(data.Info, point);
                enqueueInput(new MousePositionAbsoluteInput { Position = position });
                break;
            }

            // TODO Real mouse button event should be suppressed (???) otherwise tapping can be converted to clicks by the OS
            // TODO only enqueue when state changed
            enqueueInput(new MouseButtonInput(MouseButton.Left, data.ButtonDown));
        }

        private Vector2 mapToWindow(TouchpadInfo info, TouchpadPoint point)
        {
            var center = window!.Size / 2;

            // centered (-range/2 ~ range/2)
            int x = point.X - info.XMin - info.XRange / 2;
            int y = point.Y - info.YMin - info.YRange / 2;

            // Minimum ratio to cover the whole window
            float minimumRatio = Math.Max(
                (float)window.Size.Width / info.XRange,
                (float)window.Size.Height / info.YRange);
            var toWindow = new Vector2(
                center.Width + x * minimumRatio * (float)Sensitivity.Value,
                center.Height + y * minimumRatio * (float)Sensitivity.Value);

            return Vector2.Clamp(
                toWindow,
                Vector2.Zero,
                new Vector2(window.Size.Width - 1, window.Size.Height - 1));
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
            statistic_total_events.Value++;
        }
    }
}
