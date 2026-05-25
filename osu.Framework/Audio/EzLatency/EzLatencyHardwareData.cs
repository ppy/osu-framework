// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.EzLatency
{
#nullable disable

    public struct EzLatencyHardwareData
    {
        public double DriverTime;
        public double OutputHardwareTime;
        public double InputHardwareTime;
        public double LatencyDifference;
        public bool IsValid => OutputHardwareTime > 0 && DriverTime > 0;
    }
}
