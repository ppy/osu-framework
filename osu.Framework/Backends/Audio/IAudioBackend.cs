// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;

namespace osu.Framework.Backends.Audio
{
    /// <summary>
    /// Interface for an <see cref="IBackend"/> that creates audio tracks and samples.
    /// </summary>
    public interface IAudioBackend : IBackend
    {
        /// <summary>
        /// Creates a <see cref="Track"/> using the given data <see cref="Stream"/>.
        /// </summary>
        /// <param name="data">The audio source data stream.</param>
        /// <param name="quick">Whether we should do the bare minimum required for previewing.</param>
        Track CreateTrack(Stream data, bool quick = false);

        /// <summary>
        /// Creates a <see cref="Sample"/> using the given data byte array.
        /// </summary>
        /// <param name="data">The sample data.</param>
        /// <param name="customPendingActions">Optionally, a specific queue to use when performing audio tasks.</param>
        /// <param name="concurrency">The maximum number of times the sample can be concurrently played.</param>
        Sample CreateSample(byte[] data, ConcurrentQueue<Task> customPendingActions = null, int concurrency = Sample.DEFAULT_CONCURRENCY);

        /// <summary>
        /// Creates a <see cref="SampleChannel"/> from the given <see cref="Sample"/>.
        /// </summary>
        /// <param name="sample">The sample to use as the source for the channel.</param>
        /// <param name="onPlay">An action to execute when the sample is played.</param>
        SampleChannel CreateSampleChannel(Sample sample, Action<SampleChannel> onPlay);
    }
}
