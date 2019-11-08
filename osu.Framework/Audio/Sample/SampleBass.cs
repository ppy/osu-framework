// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Allocation;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using osu.Framework.Platform;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleBass : Sample, IBassAudio
    {
        private volatile int sampleId;

        public override bool IsLoaded => sampleId != 0;

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        internal SampleBass(byte[] data, ConcurrentQueue<Task> customPendingActions = null, int concurrency = DEFAULT_CONCURRENCY)
            : base(concurrency)
        {
            if (customPendingActions != null)
                PendingActions = customPendingActions;

            if (data.Length > 0)
            {
                EnqueueAction(() =>
                {
                    sampleId = loadSample(data);
                    memoryLease = NativeMemoryTracker.AddMemory(this, data.Length);
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsLoaded)
            {
                Bass.SampleFree(sampleId);
                memoryLease?.Dispose();
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
