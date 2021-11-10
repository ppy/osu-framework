// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NET5_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Tablet;
using osu.Framework.Logging;
using LogLevel = osu.Framework.Logging.LogLevel;

namespace osu.Framework.Input.Handlers.Tablet
{
    public class TabletDriver : Driver
    {
        private static readonly IEnumerable<int> known_vendors = Enum.GetValues<DeviceVendor>().Cast<int>();

        public TabletDriver()
        {
            Log.Output += (sender, logMessage) => Logger.Log($"{logMessage.Group}: {logMessage.Message}", level: (LogLevel)logMessage.Level);
            DevicesChanged += (sender, args) =>
            {
                // it's worth noting that this event fires on *any* device change system-wide, including non-tablet devices.
                if (Tablet == null && args.Additions.Any())
                    DetectTablet();
            };
        }

        private readonly object detectLock = new object();

        private CancellationTokenSource cancellationSource;

        public void DetectTablet()
        {
            lock (detectLock)
            {
                cancellationSource?.Cancel();

                var cancellationToken = (cancellationSource = new CancellationTokenSource()).Token;

                Task.Run(async () =>
                {
                    // wait a small delay as multiple devices may appear over a very short interval.
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);

                    int foundVendor = CurrentDevices.Select(d => d.VendorID).Intersect(known_vendors).FirstOrDefault();

                    if (foundVendor > 0)
                    {
                        Logger.Log($"Tablet detected (vid{foundVendor}), searching for usable configuration...");

                        foreach (var config in getConfigurations())
                        {
                            if (TryMatch(config))
                                break;
                        }
                    }
                }, cancellationToken);
            }
        }

        private IEnumerable<TabletConfiguration> getConfigurations()
        {
            // Retrieve all embedded configurations
            var asm = typeof(Driver).Assembly;
            return asm.GetManifestResourceNames()
                      .Where(path => path.Contains(".json"))
                      .Select(path => deserialize(asm.GetManifestResourceStream(path)));
        }

        private TabletConfiguration deserialize(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
                return new JsonSerializer().Deserialize<TabletConfiguration>(jsonReader);
        }
    }
}
#endif
