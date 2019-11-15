// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Platform;

namespace osu.Framework.Backends.Audio
{
    /// <summary>
    /// Headless implementation of <see cref="IAudioBackend"/> that can be used in non-visual tests.
    /// </summary>
    public class HeadlessAudioBackend : IAudioBackend
    {
        public Track CreateTrack(Stream data, bool quick) => new HeadlessTrack();
        public Sample CreateSample(byte[] data, ConcurrentQueue<Task> customPendingActions, int concurrency) => new HeadlessSample();
        public SampleChannel CreateSampleChannel(Sample sample, Action<SampleChannel> onPlay) => new HeadlessSampleChannel();

        public void Dispose()
        {
        }

        public void Initialise(IGameHost host)
        {
        }

        /// <summary>
        /// Headless implementation of <see cref="Track"/> that should do nothing.
        /// </summary>
        internal class HeadlessTrack : Track
        {
            public override double CurrentTime => 0;
            public override bool IsRunning => false;
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

            public override bool Playing => false;
        }
    }
}
