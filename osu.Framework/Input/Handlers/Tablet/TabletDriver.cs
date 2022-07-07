// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Components;
using OpenTabletDriver.Plugin.Tablet;
using osu.Framework.Logging;
using LogLevel = osu.Framework.Logging.LogLevel;

namespace osu.Framework.Input.Handlers.Tablet
{
    public sealed class TabletDriver : Driver
    {
        private static readonly IEnumerable<int> known_vendors = Enum.GetValues<DeviceVendor>().Cast<int>();

        private CancellationTokenSource cancellationSource;

        public event EventHandler<IDeviceReport> DeviceReported;

        public TabletDriver([NotNull] ICompositeDeviceHub deviceHub, [NotNull] IReportParserProvider reportParserProvider, [NotNull] IDeviceConfigurationProvider configurationProvider)
            : base(deviceHub, reportParserProvider, configurationProvider)
        {
            Log.Output += (_, logMessage) => Logger.Log($"{logMessage.Group}: {logMessage.Message}", level: (LogLevel)logMessage.Level);

            deviceHub.DevicesChanged += (_, args) =>
            {
                // it's worth noting that this event fires on *any* device change system-wide, including non-tablet devices.
                if (!Tablets.Any() && args.Additions.Any())
                {
                    cancellationSource?.Cancel();
                    cancellationSource = new CancellationTokenSource();

                    Task.Run(() => detectAsync(cancellationSource.Token), cancellationSource.Token);
                }
            };
        }

        private async Task detectAsync(CancellationToken cancellationToken)
        {
            // wait a small delay as multiple devices may appear over a very short interval.
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);

            int foundVendor = CompositeDeviceHub.GetDevices().Select(d => d.VendorID).Intersect(known_vendors).FirstOrDefault();

            if (foundVendor > 0)
            {
                Logger.Log($"Tablet detected (vid{foundVendor}), searching for usable configuration...");

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
#endif
