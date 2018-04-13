// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using ManagedBass;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace osu.Framework.Audio.Sample
{
    internal class SampleBass : Sample, IBassAudio
    {
        private volatile int sampleId;

        public override bool IsLoaded => sampleId != 0;

        public SampleBass(byte[] data, ConcurrentQueue<Task> customPendingActions = null, int concurrency = DEFAULT_CONCURRENCY)
            : base(concurrency)
        {
            if (customPendingActions != null)
                PendingActions = customPendingActions;

            EnqueueAction(() => { sampleId = Bass.SampleLoad(data, 0, data.Length, PlaybackConcurrency, BassFlags.Default | BassFlags.SampleOverrideLongestPlaying); });
        }

        protected override void Dispose(bool disposing)
        {
            Bass.SampleFree(sampleId);
            base.Dispose(disposing);
        }

        void IBassAudio.UpdateDevice(int deviceIndex)
        {
            if (IsLoaded)
                // counter-intuitively, this is the correct API to use to migrate a sample to a new device.
                Bass.ChannelSetDevice(sampleId, deviceIndex);
        }

        public int CreateChannel() => Bass.SampleGetChannel(sampleId);
    }
}
