// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        public override bool IsActive => true;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (host.Window is not SDL3Window window)
                return false;

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

        // Pen input is not necessarily direct on mobile platforms (specifically Android, where external tablets are supported),
        // but until users experience issues with this, consider it "direct" for now.
        private static readonly TabletPenDeviceType device_type = RuntimeInfo.IsMobile ? TabletPenDeviceType.Direct : TabletPenDeviceType.Unknown;

        private void handlePenMove(Vector2 position, bool pressed)
        {
            if (pressed && device_type == TabletPenDeviceType.Direct)
                enqueueInput(new TouchInput(new Input.Touch(TouchSource.PenTouch, position), true));
            else
                enqueueInput(new MousePositionAbsoluteInputFromPen { DeviceType = device_type, Position = position });
        }

        private void handlePenTouch(bool pressed, Vector2 position)
        {
            if (device_type == TabletPenDeviceType.Direct)
                enqueueInput(new TouchInput(new Input.Touch(TouchSource.PenTouch, position), pressed));
            else
                enqueueInput(new MouseButtonInputFromPen(pressed) { DeviceType = device_type });
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
    }
}
