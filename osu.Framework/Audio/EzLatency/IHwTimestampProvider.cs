// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.EzLatency
{
    public interface IHwTimestampProvider
    {
        bool TryGetHardwareTimestamps(int channelHandle, out double driverTimeMs, out double outputHardwareTimeMs, out double inputHardwareTimeMs, out double latencyDifferenceMs);
    }
}
