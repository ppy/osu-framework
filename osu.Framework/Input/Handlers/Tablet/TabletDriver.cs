// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NET5_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Components;
using OpenTabletDriver.Plugin.Tablet;
using osu.Framework.Logging;
using LogLevel = osu.Framework.Logging.LogLevel;

namespace osu.Framework.Input.Handlers.Tablet
{
    public class TabletDriver : Driver
    {
        private static readonly IEnumerable<int> known_vendors = Enum.GetValues<DeviceVendor>().Cast<int>();

        public TabletDriver([NotNull] ICompositeDeviceHub deviceHub, [NotNull] IReportParserProvider reportParserProvider, [NotNull] IDeviceConfigurationProvider configurationProvider)
            : base(deviceHub, reportParserProvider, configurationProvider)
        {
            Log.Output += (sender, logMessage) => Logger.Log($"{logMessage.Group}: {logMessage.Message}", level: (LogLevel)logMessage.Level);
            deviceHub.DevicesChanged += (sender, args) =>
            {
                // it's worth noting that this event fires on *any* device change system-wide, including non-tablet devices.
                if (!Tablets.Any() && args.Additions.Any())
                    Detect();
            };
        }

        private readonly object detectLock = new object();

        private CancellationTokenSource cancellationSource;

        public override bool Detect()
        {
            lock (detectLock)
            {
                cancellationSource?.Cancel();

                var cancellationToken = (cancellationSource = new CancellationTokenSource()).Token;

                Task.Run(async () =>
                {
                    // wait a small delay as multiple devices may appear over a very short interval.
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);

                    int foundVendor = CompositeDeviceHub.GetDevices().Select(d => d.VendorID).Intersect(known_vendors).FirstOrDefault();

                    if (foundVendor > 0)
                    {
                        Logger.Log($"Tablet detected (vid{foundVendor}), searching for usable configuration...");

                        base.Detect();
                    }
                }, cancellationToken);

                // ideally this would return if a tablet was detected, however it is not required.
                // it is only used a hint that one or tablets were detected to be used by inheritors.
                return true;
            }
        }
    }
}
#endif
