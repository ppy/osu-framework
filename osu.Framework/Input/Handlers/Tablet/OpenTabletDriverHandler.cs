// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NET5_0
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;
using OpenTabletDriver.Plugin.Tablet;
using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;

namespace osu.Framework.Input.Handlers.Tablet
{
    public class OpenTabletDriverHandler : InputHandler, IAbsolutePointer, IVirtualTablet, IRelativePointer, ITabletHandler
    {
        public override bool IsActive => tabletDriver.EnableInput;

        private TabletDriver tabletDriver;

        public Bindable<Vector2> AreaOffset { get; } = new Bindable<Vector2>();

        public Bindable<Vector2> AreaSize { get; } = new Bindable<Vector2>();

        public Bindable<float> Rotation { get; } = new Bindable<float>();

        public IBindable<TabletInfo> Tablet => tablet;

        private readonly Bindable<TabletInfo> tablet = new Bindable<TabletInfo>();

        public override bool Initialize(GameHost host)
        {
            tabletDriver = new TabletDriver
            {
                // for now let's keep things simple and always use absolute mode.
                // this will likely be a user setting in the future.
                OutputMode = new AbsoluteTabletMode(this)
            };

            updateOutputArea(host.Window);

            host.Window.Resized += () => updateOutputArea(host.Window);

            AreaOffset.BindValueChanged(_ => updateInputArea());
            AreaSize.BindValueChanged(_ => updateInputArea(), true);
            Rotation.BindValueChanged(_ => updateInputArea(), true);

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

        void IAbsolutePointer.SetPosition(System.Numerics.Vector2 pos) => enqueueInput(new MousePositionAbsoluteInput { Position = new Vector2(pos.X, pos.Y) });

        void IVirtualTablet.SetPressure(float percentage) => enqueueInput(new MouseButtonInput(osuTK.Input.MouseButton.Left, percentage > 0));

        void IRelativePointer.Translate(System.Numerics.Vector2 delta) => enqueueInput(new MousePositionRelativeInput { Delta = new Vector2(delta.X, delta.Y) });

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
                        Position = new System.Numerics.Vector2(outputWidth / 2, outputHeight / 2)
                    };
                    break;
                }
            }
        }

        private void updateInputArea()
        {
            if (tabletDriver.Tablet == null)
            {
                tablet.Value = null;
                return;
            }

            float inputWidth = tabletDriver.Tablet.Digitizer.Width;
            float inputHeight = tabletDriver.Tablet.Digitizer.Height;

            AreaSize.Default = new Vector2(inputWidth, inputHeight);

            // if it's clear the user has not configured the area, take the full area from the tablet that was just found.
            if (AreaSize.Value == Vector2.Zero)
                AreaSize.SetDefault();

            AreaOffset.Default = new Vector2(inputWidth / 2, inputHeight / 2);

            // likewise with the position, use the centre point if it has not been configured.
            // it's safe to assume no user would set their centre point to 0,0 for now.
            if (AreaOffset.Value == Vector2.Zero)
                AreaOffset.SetDefault();

            tablet.Value = new TabletInfo(tabletDriver.Tablet.TabletProperties.Name, AreaSize.Default);

            switch (tabletDriver.OutputMode)
            {
                case AbsoluteOutputMode absoluteOutputMode:
                {
                    // Set input area in millimeters
                    absoluteOutputMode.Input = new Area
                    {
                        Width = AreaSize.Value.X,
                        Height = AreaSize.Value.Y,
                        Position = new System.Numerics.Vector2(AreaOffset.Value.X, AreaOffset.Value.Y),
                        Rotation = Rotation.Value
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
