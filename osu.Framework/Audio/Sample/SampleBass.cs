// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Allocation;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using osu.Framework.Statistics;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleBass : Sample, IBassAudio
    {
        private volatile int sampleId;

        public override bool IsLoaded => sampleId != 0;

        internal SampleBass(byte[] data, ConcurrentQueue<Task> customPendingActions = null, int concurrency = DEFAULT_CONCURRENCY)
            : base(concurrency)
        {
            if (customPendingActions != null)
                PendingActions = customPendingActions;

            EnqueueAction(() =>
            {
                sampleId = loadSample(data);
                loaded_sample_bytes.Value += loadedBytes = data.Length;
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (IsLoaded)
            {
                Bass.SampleFree(sampleId);
                loaded_sample_bytes.Value -= loadedBytes;
            }

            base.Dispose(disposing);
        }

        void IBassAudio.UpdateDevice(int deviceIndex)
        {
            if (IsLoaded)
                // counter-intuitively, this is the correct API to use to migrate a sample to a new device.
                Bass.ChannelSetDevice(sampleId, deviceIndex);
        }

        public int CreateChannel() => Bass.SampleGetChannel(sampleId);

        private long loadedBytes;

        private static readonly GlobalStatistic<long> loaded_sample_bytes = GlobalStatistics.Get<long>("Native", nameof(SampleBass));

        private int loadSample(byte[] data)
        {
            const BassFlags flags = BassFlags.Default | BassFlags.SampleOverrideLongestPlaying;

            if (RuntimeInfo.SupportsJIT)
                return Bass.SampleLoad(data, 0, data.Length, PlaybackConcurrency, flags);

            using (var handle = new ObjectHandle<byte[]>(data, GCHandleType.Pinned))
                return Bass.SampleLoad(handle.Address, 0, data.Length, PlaybackConcurrency, flags);
        }
    }
}
