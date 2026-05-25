// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Logging;

namespace osu.Framework.Audio.EzLatency
{
#nullable disable

    public class EzLatencyService
    {
        private static readonly Lazy<EzLatencyService> lazy = new Lazy<EzLatencyService>(() => new EzLatencyService());
        public static EzLatencyService Instance => lazy.Value;
        private EzLatencyService() { }

        public void PushRecord(EzLatencyRecord record)
        {
            try
            {
                OnMeasurement?.Invoke(record);
            }
            catch (Exception ex)
            {
                Logger.Log($"EzLatencyService.PushRecord 异常: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        public event Action<EzLatencyRecord> OnMeasurement;
    }
}
