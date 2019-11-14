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
    /// Headless implementation of <see cref="IAudio"/> that can be used in non-visual tests.
    /// </summary>
    public class HeadlessAudioBackend : AudioBackend
    {
        public override Track CreateTrack(Stream data, bool quick) => new HeadlessTrack();
        public override Sample CreateSample(byte[] data, ConcurrentQueue<Task> customPendingActions, int concurrency) => new HeadlessSample();
        public override SampleChannel CreateSampleChannel(Sample sample, Action<SampleChannel> onPlay) => new HeadlessSampleChannel();

        /// <summary>
        /// Headless implementation of <see cref="Track"/> that should do nothing.
        /// </summary>
        internal class HeadlessTrack : Track
        {
            public override double CurrentTime { get; }
            public override bool IsRunning { get; }
            public override bool Seek(double seek) => true;
        }

        /// <summary>
        /// Headless implementation of <see cref="Sample"/> that should do nothing.
        /// </summary>
        internal class HeadlessSample : Sample
        {
        }

        /// <summary>
        /// Headless implementation of <see cref="SampleChannel"/> that should do nothing.
        /// </summary>
        internal class HeadlessSampleChannel : SampleChannel
        {
            public HeadlessSampleChannel()
                : base(new HeadlessSample(), _ => { })
            {
            }

            public override bool Playing { get; }
        }
    }
}
