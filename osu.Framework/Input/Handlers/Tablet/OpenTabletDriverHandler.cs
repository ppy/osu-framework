// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NET5_0
using System.Numerics;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;
using OpenTabletDriver.Plugin.Tablet;
using osu.Framework.Input.StateChanges;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Input.Handlers.Tablet
{
    public class OpenTabletDriverHandler : InputHandler, IAbsolutePointer, IVirtualTablet, IRelativePointer
    {
        public override bool IsActive => TabletDriver.EnableInput;

        public override int Priority => 0;

        protected FrameworkTabletDriver TabletDriver { set; get; }

        public override bool Initialize(GameHost host)
        {
            TabletDriver = new FrameworkTabletDriver
            {
                OutputMode = new AbsoluteTabletMode(this)
            };

            TabletDriver.TabletChanged += (sender, e) =>
            {
                if (TabletDriver.OutputMode is AbsoluteOutputMode absoluteOutputMode && TabletDriver.Tablet is TabletState tablet)
                {
                    float inputWidth, inputHeight, outputWidth, outputHeight;

                    // Set input area in millimeters
                    absoluteOutputMode.Input = new Area
                    {
                        Width = inputWidth = tablet.Digitizer.Width,
                        Height = inputHeight = tablet.Digitizer.Height,
                        Position = new Vector2(inputWidth / 2, inputHeight / 2),
                        Rotation = 0
                    };

                    // Set output area in pixels
                    absoluteOutputMode.Output = new Area
                    {
                        // Ideally would be the maximum window dimensions
                        Width = outputWidth = host.Window.ClientSize.Width,
                        Height = outputHeight = host.Window.ClientSize.Height,
                        Position = new Vector2(outputWidth / 2, outputHeight / 2)
                    };
                }
            };

            TabletDriver.ReportRecieved += (sender, report) =>
            {
                if (report is ITabletReport tabletReport)
                    handleTabletReport(tabletReport);
                if (report is IAuxReport auxiliaryReport)
                    handleAuxiliaryReport(auxiliaryReport);
            };

            Enabled.BindValueChanged(d =>
            {
                if (d.NewValue)
                {
                    if (TabletDriver.Tablet == null)
                    {
                        Logger.Log("Detecting tablets...");
                        TabletDriver.DetectTablet();
                    }
                }

                TabletDriver.EnableInput = d.NewValue;
            }, true);

            return true;
        }

        public void SetPosition(Vector2 pos) => enqueueInput(new MousePositionAbsoluteInput { Position = new osuTK.Vector2(pos.X, pos.Y) });

        public void SetPressure(float percentage) => enqueueInput(new MouseButtonInput(osuTK.Input.MouseButton.Left, percentage > 0));

        public void Translate(Vector2 delta) => enqueueInput(new MousePositionRelativeInput { Delta = new osuTK.Vector2(delta.X, delta.Y) });

        private void handleTabletReport(ITabletReport tabletReport)
        {
            int buttonCount = tabletReport.PenButtons.Length;
            var buttons = new ButtonInputEntry<TabletPenButton>[buttonCount];
            for (int i = 0; i < buttonCount; i++)
                buttons[i] = new ButtonInputEntry<TabletPenButton>((TabletPenButton)i, tabletReport.PenButtons[i]);

            enqueueInput(new TabletPenButtonInput(buttons));
        }

        private void handleAuxiliaryReport(IAuxReport auxiliaryReport)
        {
            int buttonCount = auxiliaryReport.AuxButtons.Length;
            var buttons = new ButtonInputEntry<TabletAuxiliaryButton>[buttonCount];
            for (int i = 0; i < buttonCount; i++)
                buttons[i] = new ButtonInputEntry<TabletAuxiliaryButton>((TabletAuxiliaryButton)i, auxiliaryReport.AuxButtons[i]);

            enqueueInput(new TabletAuxiliaryButtonInput(buttons));
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.TabletEvents);
        }
    }
}
#endif
