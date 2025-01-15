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

        // iPadOS doesn't support external tablets, so we are sure it's direct Apple Pencil input.
        // Other platforms support both direct and indirect tablet input, but SDL doesn't provide any information on the current device type.
        private static readonly TabletPenDeviceType device_type = RuntimeInfo.OS == RuntimeInfo.Platform.iOS ? TabletPenDeviceType.Direct : TabletPenDeviceType.Unknown;

        private void handlePenMove(Vector2 position)
        {
            enqueueInput(new MousePositionAbsoluteInputFromPen
            {
                Position = position,
                DeviceType = device_type
            });
        }

        private void handlePenTouch(bool pressed)
        {
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
