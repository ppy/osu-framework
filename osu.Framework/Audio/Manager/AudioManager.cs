// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Audio.Manager
{
    public abstract class AudioManager : AudioCollectionManager<AudioComponent>, IAudioManager
    {
        /// <summary>
        /// The manager component responsible for audio tracks (e.g. songs).
        /// </summary>
        public ITrackStore Tracks => globalTrackStore.Value;

        /// <summary>
        /// The manager component responsible for audio samples (e.g. sound effects).
        /// </summary>
        public ISampleStore Samples => globalSampleStore.Value;

        /// <summary>
        /// The thread audio operations (mainly Bass calls) are ran on.
        /// </summary>
        private readonly AudioThread thread;

        /// <summary>
        /// The global mixer which all tracks are routed into by default.
        /// </summary>
        public readonly AudioMixer TrackMixer;

        /// <summary>
        /// The global mixer which all samples are routed into by default.
        /// </summary>
        public readonly AudioMixer SampleMixer;

        /// <summary>
        /// Is fired whenever a new audio device is discovered and provides its name.
        /// </summary>
        public event Action<KeyValuePair<string, string>>? OnNewDevice;

        /// <summary>
        /// Is fired whenever an audio device is lost and provides its name.
        /// </summary>
        public event Action<KeyValuePair<string, string>>? OnLostDevice;

        public abstract Bindable<string> AudioDevice { get; }

        public abstract ImmutableDictionary<string, string> AudioDevices { get; protected set; }

        public abstract string DefaultDevice { get; }

        public BindableDouble VolumeSample { get; } = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        public BindableDouble VolumeTrack { get; } = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        protected Scheduler Scheduler => thread.Scheduler;

        protected readonly CancellationTokenSource CancelSource = new CancellationTokenSource();

        public Scheduler? EventScheduler { get; set; }

        internal IBindableList<AudioMixer> ActiveMixers => activeMixers;
        private readonly BindableList<AudioMixer> activeMixers = new BindableList<AudioMixer>();

        private readonly Lazy<TrackStore> globalTrackStore;
        private readonly Lazy<SampleStore> globalSampleStore;

        /// <summary>
        /// Constructs an AudioStore.
        /// </summary>
        /// <param name="audioThread">The host's audio thread.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        protected AudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
        {
            thread = audioThread;

            thread.RegisterManager(this);

            AddItem(TrackMixer = CreateAudioMixer(null, nameof(TrackMixer)));
            AddItem(SampleMixer = CreateAudioMixer(null, nameof(SampleMixer)));

            globalTrackStore = new Lazy<TrackStore>(() =>
            {
                var store = new TrackStore(trackStore, TrackMixer);
                AddItem(store);
                store.AddAdjustment(AdjustableProperty.Volume, VolumeTrack);
                return store;
            });

            globalSampleStore = new Lazy<SampleStore>(() =>
            {
                var store = new SampleStore(sampleStore, SampleMixer);
                AddItem(store);
                store.AddAdjustment(AdjustableProperty.Volume, VolumeSample);
                return store;
            });
        }

        protected override void Dispose(bool disposing)
        {
            CancelSource.Cancel();

            thread.UnregisterManager(this);

            OnNewDevice = null;
            OnLostDevice = null;

            base.Dispose(disposing);
        }

        private static int userMixerID;

        /// <summary>
        /// Creates a new <see cref="AudioMixer"/>.
        /// </summary>
        /// <remarks>
        /// Channels removed from this <see cref="AudioMixer"/> fall back to the global <see cref="SampleMixer"/>.
        /// </remarks>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        public AudioMixer CreateAudioMixer(string? identifier = default) =>
            CreateAudioMixer(SampleMixer, !string.IsNullOrEmpty(identifier) ? identifier : $"user #{Interlocked.Increment(ref userMixerID)}");

        protected abstract AudioMixer CreateAudioMixer(AudioMixer? fallbackMixer, string identifier);

        protected override void ItemAdded(AudioComponent item)
        {
            base.ItemAdded(item);
            if (item is AudioMixer mixer)
                activeMixers.Add(mixer);
        }

        protected override void ItemRemoved(AudioComponent item)
        {
            base.ItemRemoved(item);
            if (item is AudioMixer mixer)
                activeMixers.Remove(mixer);
        }

        protected void InvokeOnNewDevice(KeyValuePair<string, string> device) => OnNewDevice?.Invoke(device);

        protected void InvokeOnLostDevice(KeyValuePair<string, string> device) => OnLostDevice?.Invoke(device);

        public ITrackStore GetTrackStore(IResourceStore<byte[]>? store = null, AudioMixer? mixer = null)
        {
            if (store == null) return globalTrackStore.Value;

            TrackStore tm = new TrackStore(store, mixer ?? TrackMixer);
            globalTrackStore.Value.AddItem(tm);
            return tm;
        }

        public ISampleStore GetSampleStore(IResourceStore<byte[]>? store = null, AudioMixer? mixer = null)
        {
            if (store == null) return globalSampleStore.Value;

            SampleStore sm = new SampleStore(store, mixer ?? SampleMixer);
            globalSampleStore.Value.AddItem(sm);
            return sm;
        }

        /// <summary>
        /// Initialises the audio device.
        /// </summary>
        /// <param name="device">The index of the <see cref="IAudioManager.AudioDevices"/> to initialise.</param>
        /// <returns>Whether the device was successfully initialised.</returns>
        internal abstract bool InitDevice(int device);

        /// <summary>
        /// Frees the audio device.
        /// </summary>
        /// <param name="device">The index of the <see cref="IAudioManager.AudioDevices"/> to free.</param>
        internal abstract void FreeDevice(int device);
    }
}
