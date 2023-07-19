// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Components;
using OpenTabletDriver.Plugin.Tablet;
using LogLevel = osu.Framework.Logging.LogLevel;

namespace osu.Framework.Input.Handlers.Tablet
{
    public sealed class TabletDriver : Driver
    {
        private readonly int[] knownVendors;

        private CancellationTokenSource? cancellationSource;

        public event EventHandler<IDeviceReport>? DeviceReported;

        public Action<string, LogLevel, Exception?>? PostLog;

        public TabletDriver(ICompositeDeviceHub deviceHub, IReportParserProvider reportParserProvider, IDeviceConfigurationProvider configurationProvider)
            : base(deviceHub, reportParserProvider, configurationProvider)
        {
            var vendors = from config in configurationProvider.TabletConfigurations
                          from id in config.DigitizerIdentifiers
                          select id.VendorID;

            knownVendors = vendors.Distinct().ToArray();

            Log.Output += (_, logMessage) =>
            {
                LogLevel level = (int)logMessage.Level > (int)LogLevel.Error ? LogLevel.Error : (LogLevel)logMessage.Level;
                PostLog?.Invoke($"{logMessage.Group}: {logMessage.Message}", level, null);
            };

            deviceHub.DevicesChanged += (_, args) =>
            {
                // it's worth noting that this event fires on *any* device change system-wide, including non-tablet devices.
                if (!Tablets.Any() && args.Additions.Any())
                    detectAsync();
            };

            detectAsync();
        }

        private void detectAsync()
        {
            cancellationSource?.Cancel();
            cancellationSource = new CancellationTokenSource();

            Task.Run(() => detectAsync(cancellationSource.Token), cancellationSource.Token);
        }

        private async Task detectAsync(CancellationToken cancellationToken)
        {
            // wait a small delay as multiple devices may appear over a very short interval.
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);

            int foundVendor = CompositeDeviceHub.GetDevices().Select(d => d.VendorID).Intersect(knownVendors).FirstOrDefault();

            if (foundVendor > 0)
            {
                PostLog?.Invoke($"Tablet detected (vid{foundVendor}), searching for usable configuration...", LogLevel.Verbose, null);

                Detect();

                foreach (var device in InputDevices)
                {
                    foreach (var endpoint in device.InputDevices)
                    {
                        endpoint.Report += DeviceReported;
                        endpoint.ConnectionStateChanged += (_, connected) =>
                        {
                            if (!connected)
                                endpoint.Report -= DeviceReported;
                        };
                    }
                }
            }
        }

        public static TabletDriver Create()
        {
            IServiceCollection serviceCollection = new DriverServiceCollection()
                .AddTransient<TabletDriver>();

            var provider = serviceCollection.BuildServiceProvider();

            return provider.GetRequiredService<TabletDriver>();
        }
    }
}
