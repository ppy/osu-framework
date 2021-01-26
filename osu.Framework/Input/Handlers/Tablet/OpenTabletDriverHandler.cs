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
        public override bool IsActive => tabletDriver.EnableInput;

        public override int Priority => 0;

        private FrameworkTabletDriver tabletDriver;

        public override bool Initialize(GameHost host)
        {
            tabletDriver = new FrameworkTabletDriver
            {
                OutputMode = new AbsoluteTabletMode(this)
            };

            updateOutputArea(host.Window);
            updateInputArea();
            host.Window.Resized += () => updateOutputArea(host.Window);
            tabletDriver.TabletChanged += (sender, e) => updateInputArea();

            tabletDriver.ReportRecieved += (sender, report) =>
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
                    if (tabletDriver.Tablet == null)
                    {
                        Logger.Log("Detecting tablets...");
                        tabletDriver.DetectTablet();
                    }
                }

                tabletDriver.EnableInput = d.NewValue;
            }, true);

            return true;
        }

        void IAbsolutePointer.SetPosition(Vector2 pos) => enqueueInput(new MousePositionAbsoluteInput { Position = new osuTK.Vector2(pos.X, pos.Y) });

        void IVirtualTablet.SetPressure(float percentage) => enqueueInput(new MouseButtonInput(osuTK.Input.MouseButton.Left, percentage > 0));

        void IRelativePointer.Translate(Vector2 delta) => enqueueInput(new MousePositionRelativeInput { Delta = new osuTK.Vector2(delta.X, delta.Y) });

        private void updateOutputArea(IWindow window)
        {
            if (tabletDriver.OutputMode is AbsoluteOutputMode absoluteOutputMode)
            {
                float outputWidth, outputHeight;

                // Set output area in pixels
                absoluteOutputMode.Output = new Area
                {
                    Width = outputWidth = window.ClientSize.Width,
                    Height = outputHeight = window.ClientSize.Height,
                    Position = new Vector2(outputWidth / 2, outputHeight / 2)
                };
            }
        }

        private void updateInputArea()
        {
            if (tabletDriver.OutputMode is AbsoluteOutputMode absoluteOutputMode && tabletDriver.Tablet != null)
            {
                float inputWidth, inputHeight;

                // Set input area in millimeters
                absoluteOutputMode.Input = new Area
                {
                    Width = inputWidth = tabletDriver.Tablet.Digitizer.Width,
                    Height = inputHeight = tabletDriver.Tablet.Digitizer.Height,
                    Position = new Vector2(inputWidth / 2, inputHeight / 2),
                    Rotation = 0
                };
            }
        }

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
