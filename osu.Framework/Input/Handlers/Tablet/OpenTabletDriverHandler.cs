// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private static readonly GlobalStatistic<ulong> statistic_total_events = GlobalStatistics.Get<ulong>(StatisticGroupFor<OpenTabletDriverHandler>(), "Total events");

        private TabletDriver? tabletDriver;

        private InputDeviceTree? device;

        private AbsoluteOutputMode outputMode = null!;

        private GameHost host = null!;

        public override string Description => "Tablet";

        public override bool IsActive => tabletDriver != null;

        public Bindable<Vector2> AreaOffset { get; } = new Bindable<Vector2>();

        public Bindable<Vector2> AreaSize { get; } = new Bindable<Vector2>();

        public Bindable<Vector2> OutputAreaOffset { get; } = new Bindable<Vector2>(new Vector2(0.5f, 0.5f));

        public Bindable<Vector2> OutputAreaSize { get; } = new Bindable<Vector2>(new Vector2(1f, 1f));

        public Bindable<float> Rotation { get; } = new Bindable<float>();

        public BindableFloat PressureThreshold { get; } = new BindableFloat(0.0f)
        {
            MinValue = 0f,
            MaxValue = 1f,
            Precision = 0.005f,
        };

        public IBindable<TabletInfo?> Tablet => tablet;

        private readonly Bindable<TabletInfo?> tablet = new Bindable<TabletInfo?>();

        private Task? lastInitTask;

        private IBindable<bool> windowActive = null!;

        public override bool Initialize(GameHost host)
        {
            this.host = host;

            outputMode = new AbsoluteTabletMode(this);

            host.Window.Resized += () => updateOutputArea(host.Window);
            windowActive = host.Window.IsActive.GetBoundCopy();

            AreaOffset.BindValueChanged(_ => updateTabletAndInputArea(device));
            AreaSize.BindValueChanged(_ => updateTabletAndInputArea(device));
            Rotation.BindValueChanged(_ => updateTabletAndInputArea(device), true);

            OutputAreaOffset.BindValueChanged(_ => updateOutputArea(host.Window));
            OutputAreaSize.BindValueChanged(_ => updateOutputArea(host.Window), true);

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    lastInitTask = Task.Run(() =>
                    {
                        tabletDriver = TabletDriver.Create();
                        tabletDriver.PostLog = Log;
                        tabletDriver.TabletsChanged += handleTabletsChanged;
                        tabletDriver.DeviceReported += handleDeviceReported;
                        tabletDriver.Detect();
                    });
                }
                else
                {
                    lastInitTask?.WaitSafely();

                    if (tabletDriver != null)
                    {
                        tabletDriver.DeviceReported -= handleDeviceReported;
                        tabletDriver.TabletsChanged -= handleTabletsChanged;
                        tabletDriver.Dispose();
                        tabletDriver = null;
                    }
                }
            }, true);

            return true;
        }

        private TabletPenDeviceType lastTabletDeviceType = TabletPenDeviceType.Unknown;

        void IAbsolutePointer.SetPosition(System.Numerics.Vector2 pos)
        {
            lastTabletDeviceType = TabletPenDeviceType.Unknown;
            enqueueInput(new MousePositionAbsoluteInputFromPen { Position = new Vector2(pos.X, pos.Y), DeviceType = lastTabletDeviceType });
        }

        void IRelativePointer.SetPosition(System.Numerics.Vector2 delta)
        {
            lastTabletDeviceType = TabletPenDeviceType.Indirect;
            enqueueInput(new MousePositionRelativeInputFromPen { Delta = new Vector2(delta.X, delta.Y), DeviceType = lastTabletDeviceType });
        }

        void IPressureHandler.SetPressure(float percentage) => enqueueInput(new MouseButtonInputFromPen(percentage > PressureThreshold.Value) { DeviceType = lastTabletDeviceType });

        private void handleTabletsChanged(object? sender, IEnumerable<TabletReference> tablets)
        {
            device = tablets.Any() ? tabletDriver?.InputDevices.First() : null;

            if (device != null)
            {
                // Important to reinitialise outputMode here as the previous one is likely disposed.
                device.OutputMode = outputMode = new AbsoluteTabletMode(this);
                outputMode.Tablet = device.CreateReference();

                updateTabletAndInputArea(device);
                updateOutputArea(host.Window);
            }
            else
                tablet.Value = null;
        }

        private void handleDeviceReported(object? sender, IDeviceReport report)
        {
            if (report is ITabletReport tabletReport)
                handleTabletReport(tabletReport);

            if (report is IAuxReport auxiliaryReport)
                handleAuxiliaryReport(auxiliaryReport);
        }

        private void updateOutputArea(IWindow window)
        {
            if (device == null)
                return;

            switch (device.OutputMode)
            {
                case AbsoluteOutputMode absoluteOutputMode:
                {
                    Vector2 windowSize = new Vector2(window.ClientSize.Width, window.ClientSize.Height);
                    Vector2 scaledSize = windowSize * OutputAreaSize.Value;
                    Vector2 offsetFromCenter = (OutputAreaOffset.Value - new Vector2(0.5f, 0.5f)) * (windowSize - scaledSize);
                    Vector2 position = (windowSize / 2) + offsetFromCenter;

                    absoluteOutputMode.Output = new Area
                    {
                        Width = scaledSize.X,
                        Height = scaledSize.Y,
                        Position = position.ToSystemNumerics()
                    };
                    break;
                }
            }
        }

        private void updateTabletAndInputArea(InputDeviceTree? inputDevice)
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
            if (!windowActive.Value)
                return;

            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.TabletEvents);
            statistic_total_events.Value++;
        }
    }
}
