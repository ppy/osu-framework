// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Platform;

namespace osu.Framework.Backends.Audio.Bass
{
    /// <summary>
    /// Provides an <see cref="IAudioBackend"/> backend that creates tracks and samples using BASS.
    /// </summary>
    public class BassAudioBackend : IAudioBackend
    {
        public Track CreateTrack(Stream data, bool quick) => new TrackBass(data, quick);

        public Sample CreateSample(byte[] data, ConcurrentQueue<Task> customPendingActions, int concurrency) => new SampleBass(data, customPendingActions, concurrency);

        public SampleChannel CreateSampleChannel(Sample sample, Action<SampleChannel> onPlay) => new SampleChannelBass(sample, onPlay);

        public void Dispose()
        {
        }

        public void Initialise(IGameHost host)
        {
        }
    }
}
