// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.EzLatency
{
#nullable disable

    public struct EzLatencyInputData
    {
        public double InputTime;
        public object KeyValue;
        public double JudgeTime;

        public double PlaybackTime;

        // Consider input+playback or input+judge as valid for best-effort measurements.
        public bool IsValid => InputTime > 0 && (PlaybackTime > 0 || JudgeTime > 0);
    }
}
