// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.EzLatency
{
#nullable disable

    public interface IEzLatencyTracker : IDisposable
    {
        void Start();
        void Stop();
        event Action<EzLatencyRecord> OnMeasurement;
        void SetSampleRate(int sampleRate);
    }
}
