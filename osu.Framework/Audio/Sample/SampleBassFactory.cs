// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Runtime.InteropServices;
using ManagedBass;
using osu.Framework.Allocation;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Bindables;
using osu.Framework.Platform;

namespace osu.Framework.Audio.Sample
{
    /// <summary>
    /// A factory for <see cref="SampleBass"/> objects sharing a common sample ID (and thus playback concurrency).
    /// </summary>
    internal class SampleBassFactory : AudioCollectionManager<AdjustableAudioComponent>
    {
        public int SampleId { get; private set; }

        public override bool IsLoaded => SampleId != 0;

        public double Length { get; private set; }

        /// <summary>
        /// Todo: Expose this to support per-sample playback concurrency once ManagedBass has been updated (https://github.com/ManagedBass/ManagedBass/pull/85).
        /// </summary>
        internal readonly Bindable<int> PlaybackConcurrency = new Bindable<int>(Sample.DEFAULT_CONCURRENCY);

        private readonly BassAudioMixer mixer;

        private NativeMemoryTracker.NativeMemoryLease? memoryLease;
        private byte[]? data;

        public SampleBassFactory(byte[] data, BassAudioMixer mixer)
        {
            this.data = data;
            this.mixer = mixer;

            EnqueueAction(loadSample);

            PlaybackConcurrency.BindValueChanged(updatePlaybackConcurrency);
        }

        private void updatePlaybackConcurrency(ValueChangedEvent<int> concurrency)
        {
            EnqueueAction(() =>
            {
                // Broken in ManagedBass (https://github.com/ManagedBass/ManagedBass/pull/85).
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
            // The sample may not have already loaded if a device wasn't present in a previous load attempt.
            if (!IsLoaded)
                loadSample();
        }

        private void loadSample()
        {
            Debug.Assert(CanPerformInline);
            Debug.Assert(!IsLoaded);

            if (data == null)
                return;

            int dataLength = data.Length;

            const BassFlags flags = BassFlags.Default | BassFlags.SampleOverrideLongestPlaying;
            using (var handle = new ObjectHandle<byte[]>(data, GCHandleType.Pinned))
                SampleId = Bass.SampleLoad(handle.Address, 0, dataLength, PlaybackConcurrency.Value, flags);

            if (Bass.LastError == Errors.Init)
                return;

            // We've done as best as we could to init the sample. It may still have failed by some other cause (such as malformed data), but allow the GC to now clean up the locally-stored data.
            data = null;

            if (!IsLoaded)
                return;

            Length = Bass.ChannelBytes2Seconds(SampleId, dataLength) * 1000;
            memoryLease = NativeMemoryTracker.AddMemory(this, dataLength);
        }

        public Sample CreateSample() => new SampleBass(this, mixer) { OnPlay = onPlay };

        private void onPlay(Sample sample)
        {
            AddItem(sample);
        }

        ~SampleBassFactory()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (IsLoaded)
            {
                Bass.SampleFree(SampleId);
                memoryLease?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
