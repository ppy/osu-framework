// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NET5_0
using System.Numerics;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;
using OpenTabletDriver.Plugin.Tablet;
using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Input.Handlers.Tablet
{
    public class OpenTabletDriverHandler : InputHandler, IAbsolutePointer, IVirtualTablet, IRelativePointer
    {
        public override bool IsActive => tabletDriver.EnableInput;

        public override int Priority => 0;

        private TabletDriver tabletDriver;

        public Bindable<Vector2> AreaOffset { get; } = new Bindable<Vector2>();

        public Bindable<Vector2> AreaSize { get; } = new Bindable<Vector2>();

        public override bool Initialize(GameHost host)
        {
            tabletDriver = new TabletDriver
            {
                // for now let's keep things simple and always use absolute mode.
                // this will likely be a user setting in the future.
                OutputMode = new AbsoluteTabletMode(this)
            };

            updateOutputArea(host.Window);
            updateInputArea();

            host.Window.Resized += () => updateOutputArea(host.Window);

            tabletDriver.TabletChanged += (sender, e) => updateInputArea();
            tabletDriver.ReportReceived += (sender, report) =>
            {
                switch (report)
                {
                    case ITabletReport tabletReport:
                        handleTabletReport(tabletReport);
                        break;

                    case IAuxReport auxiliaryReport:
                        handleAuxiliaryReport(auxiliaryReport);
                        break;
                }
            };

            Enabled.BindValueChanged(d =>
            {
                if (d.NewValue)
                {
                    if (tabletDriver.Tablet == null)
                        tabletDriver.DetectTablet();
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
            switch (tabletDriver.OutputMode)
            {
                case AbsoluteOutputMode absoluteOutputMode:
                {
                    float outputWidth, outputHeight;

                    // Set output area in pixels
                    absoluteOutputMode.Output = new Area
                    {
                        Width = outputWidth = window.ClientSize.Width,
                        Height = outputHeight = window.ClientSize.Height,
                        Position = new Vector2(outputWidth / 2, outputHeight / 2)
                    };
                    break;
                }
            }
        }

        private void updateInputArea()
        {
            if (tabletDriver.Tablet == null)
                return;

            switch (tabletDriver.OutputMode)
            {
                case AbsoluteOutputMode absoluteOutputMode:
                {
                    float inputWidth = tabletDriver.Tablet.Digitizer.Width;
                    float inputHeight = tabletDriver.Tablet.Digitizer.Height;

                    // Set input area in millimeters
                    absoluteOutputMode.Input = new Area
                    {
                        Width = inputWidth,
                        Height = inputHeight,
                        Position = new Vector2(inputWidth / 2, inputHeight / 2),
                        Rotation = 0
                    };
                    break;
                }
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
