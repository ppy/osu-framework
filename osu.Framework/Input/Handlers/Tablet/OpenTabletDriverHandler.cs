// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

#if NET6_0_OR_GREATER
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;
using OpenTabletDriver.Plugin.Tablet;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;

namespace osu.Framework.Input.Handlers.Tablet
{
    public class OpenTabletDriverHandler : InputHandler, IAbsolutePointer, IRelativePointer, IPressureHandler, ITabletHandler
    {
        private TabletDriver tabletDriver;

        [CanBeNull]
        private InputDeviceTree device;

        private AbsoluteOutputMode outputMode;

        public override bool IsActive => tabletDriver != null;

        public Bindable<Vector2> AreaOffset { get; } = new Bindable<Vector2>();

        public Bindable<Vector2> AreaSize { get; } = new Bindable<Vector2>();

        public Bindable<float> Rotation { get; } = new Bindable<float>();

        public IBindable<TabletInfo> Tablet => tablet;

        private readonly Bindable<TabletInfo> tablet = new Bindable<TabletInfo>();

        private Task lastInitTask;

        public override bool Initialize(GameHost host)
        {
            outputMode = new AbsoluteTabletMode(this);

            host.Window.Resized += () => updateOutputArea(host.Window);

            AreaOffset.BindValueChanged(_ => updateInputArea(device));
            AreaSize.BindValueChanged(_ => updateInputArea(device), true);
            Rotation.BindValueChanged(_ => updateInputArea(device), true);

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    lastInitTask = Task.Run(() =>
                    {
                        tabletDriver = TabletDriver.Create();
                        tabletDriver.TabletsChanged += (_, e) =>
                        {
                            device = e.Any() ? tabletDriver.InputDevices.First() : null;

                            if (device != null)
                            {
                                device.OutputMode = outputMode;
                                outputMode.Tablet = device.CreateReference();

                                updateInputArea(device);
                                updateOutputArea(host.Window);
                            }
                        };
                        tabletDriver.DeviceReported += handleDeviceReported;
                        tabletDriver.Detect();
                    });
                }
                else
                {
                    lastInitTask?.WaitSafely();
                    tabletDriver?.Dispose();
                    tabletDriver = null;
                }
            }, true);

            return true;
        }

        void IAbsolutePointer.SetPosition(System.Numerics.Vector2 pos) => enqueueInput(new MousePositionAbsoluteInput { Position = new Vector2(pos.X, pos.Y) });

        void IRelativePointer.SetPosition(System.Numerics.Vector2 delta) => enqueueInput(new MousePositionRelativeInput { Delta = new Vector2(delta.X, delta.Y) });

        void IPressureHandler.SetPressure(float percentage) => enqueueInput(new MouseButtonInput(osuTK.Input.MouseButton.Left, percentage > 0));

        private void handleDeviceReported(object sender, IDeviceReport report)
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
        }

        private void updateOutputArea(IWindow window)
        {
            if (device == null)
                return;

            switch (device.OutputMode)
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

        private void updateInputArea([CanBeNull] InputDeviceTree inputDevice)
        {
            if (inputDevice == null)
                return;

            var digitizer = inputDevice.Properties.Specifications.Digitizer;
            float inputWidth = digitizer.Width;
            float inputHeight = digitizer.Height;

            AreaSize.Default = new Vector2(inputWidth, inputHeight);

            // if it's clear the user has not configured the area, take the full area from the tablet that was just found.
            if (AreaSize.Value == Vector2.Zero)
                AreaSize.SetDefault();

            AreaOffset.Default = new Vector2(inputWidth / 2, inputHeight / 2);

            // likewise with the position, use the centre point if it has not been configured.
            // it's safe to assume no user would set their centre point to 0,0 for now.
            if (AreaOffset.Value == Vector2.Zero)
                AreaOffset.SetDefault();

            tablet.Value = new TabletInfo(inputDevice.Properties.Name, AreaSize.Default);

            switch (inputDevice.OutputMode)
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
