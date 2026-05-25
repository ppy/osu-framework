// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.EzLatency
{
    public interface IEzLatencyPlayback : IDisposable
    {
        void PlayTestTone();
        void StopTestTone();
        void SetSampleRate(int sampleRate);
    }
}
