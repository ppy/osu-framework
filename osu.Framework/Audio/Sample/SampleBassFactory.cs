// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using ManagedBass;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Platform;

namespace osu.Framework.Audio.Sample
{
    /// <summary>
    /// A factory for <see cref="SampleBass"/> objects sharing a common sample ID (and thus concurrent playback).
    /// </summary>
    internal class SampleBassFactory : AudioCollectionManager<AdjustableAudioComponent>
    {
        public int SampleId { get; private set; }

        public override bool IsLoaded => SampleId != 0;

        public double Length { get; private set; }

        public readonly Bindable<int> PlaybackConcurrency = new Bindable<int>(Sample.DEFAULT_CONCURRENCY);

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        public SampleBassFactory(byte[] data)
        {
            if (data.Length > 0)
            {
                EnqueueAction(() =>
                {
                    SampleId = loadSample(data);
                    memoryLease = NativeMemoryTracker.AddMemory(this, data.Length);
                });
            }
        }

        private void updatePlaybackConcurrency(ValueChangedEvent<int> concurrency)
        {
            EnqueueAction(() =>
            {
                // Todo: Broken (SEGV on SampleGetInfo())
                // if (!IsLoaded)
                //     return;
                //
                // var sampleInfo = Bass.SampleGetInfo(SampleId);
                // sampleInfo.Max = concurrency.NewValue;
                // Bass.SampleSetInfo(SampleId, sampleInfo);
            });
        }

        internal override void UpdateDevice(int deviceIndex)
        {
            if (!IsLoaded)
                return;

            // counter-intuitively, this is the correct API to use to migrate a sample to a new device.
            Bass.ChannelSetDevice(SampleId, deviceIndex);
            BassUtils.CheckFaulted(true);
        }

        private int loadSample(byte[] data)
        {
            int handle = getSampleHandle(data);
            Length = Bass.ChannelBytes2Seconds(handle, data.Length) * 1000;
            return handle;
        }

        private int getSampleHandle(byte[] data)
        {
            const BassFlags flags = BassFlags.Default | BassFlags.SampleOverrideLongestPlaying;

            if (RuntimeInfo.SupportsJIT)
                return Bass.SampleLoad(data, 0, data.Length, PlaybackConcurrency.Value, flags);

            using (var handle = new ObjectHandle<byte[]>(data, GCHandleType.Pinned))
                return Bass.SampleLoad(handle.Address, 0, data.Length, PlaybackConcurrency.Value, flags);
        }

        public Sample CreateSample()
        {
            var sample = new SampleBass(this);
            AddItem(sample);
            return sample;
        }

        protected override void Dispose(bool disposing)
        {
            if (IsLoaded)
            {
                Bass.SampleFree(SampleId);
                memoryLease?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
