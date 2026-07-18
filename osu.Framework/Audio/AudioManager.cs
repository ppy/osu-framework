// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Audio
{
    public abstract class AudioManager : AudioCollectionManager<AudioComponent>
    {
        /// <summary>
        /// The thread audio operations (mainly Bass calls) are ran on.
        /// </summary>
        protected readonly AudioThread CurrentAudioThread;

        /// <summary>
        /// The manager component responsible for audio tracks (e.g. songs).
        /// </summary>
        public ITrackStore Tracks => AudioGlobalTrackStore.Value;

        /// <summary>
        /// The manager component responsible for audio samples (e.g. sound effects).
        /// </summary>
        public ISampleStore Samples => AudioGlobalSampleStore.Value;

        /// <summary>
        /// The global mixer which all tracks are routed into by default.
        /// </summary>
        public readonly AudioMixer TrackMixer;

        /// <summary>
        /// The global mixer which all samples are routed into by default.
        /// </summary>
        public readonly AudioMixer SampleMixer;

        /// <summary>
        /// The names of all available audio devices.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property does not contain the names of disabled audio devices.
        /// </para>
        /// <para>
        /// This property may also not necessarily contain the name of the default audio device provided by the OS.
        /// Consumers should provide a "Default" audio device entry which sets <see cref="AudioDevice"/> to an empty string.
        /// </para>
        /// </remarks>
        public IEnumerable<string> AudioDeviceNames => DeviceNames;

        /// <summary>
        /// Is fired whenever a new audio device is discovered and provides its name.
        /// </summary>
        public event Action<string> OnNewDevice;

        // workaround as c# doesn't allow actions to get invoked outside of this class
        protected virtual void InvokeOnNewDevice(string deviceName) => OnNewDevice?.Invoke(deviceName);

        /// <summary>
        /// Is fired whenever an audio device is lost and provides its name.
        /// </summary>
        public event Action<string> OnLostDevice;

        // same as above
        protected virtual void InvokeOnLostDevice(string deviceName) => OnLostDevice?.Invoke(deviceName);

        /// <summary>
        /// The preferred audio device we should use. A value of
        /// <see cref="string.Empty"/> denotes the OS default.
        /// </summary>
        public readonly Bindable<string> AudioDevice = new Bindable<string>();

        /// <summary>
        /// Whether to use experimental WASAPI initialisation on windows.
        /// This generally results in lower audio latency, but also changes the audio synchronisation from
        /// historical expectations, meaning users / application will have to account for different offsets.
        /// </summary>
        public readonly BindableBool UseExperimentalWasapi = new BindableBool();

        /// <summary>
        /// Volume of all samples played game-wide.
        /// </summary>
        public readonly BindableDouble VolumeSample = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        /// <summary>
        /// Volume of all tracks played game-wide.
        /// </summary>
        public readonly BindableDouble VolumeTrack = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        /// <summary>
        /// Whether a global mixer is being used for audio routing.
        /// For now, this is only the case on Windows when using shared mode WASAPI initialisation.
        /// </summary>
        public IBindable<bool> UsingGlobalMixer => usingGlobalMixer;

        private readonly Bindable<bool> usingGlobalMixer = new BindableBool();

        // Mutated by multiple threads, must be thread safe.
        protected ImmutableList<string> DeviceNames = ImmutableList<string>.Empty;

        protected Scheduler AudioScheduler => CurrentAudioThread.Scheduler;

        protected readonly CancellationTokenSource CancelSource = new CancellationTokenSource();

        /// <summary>
        /// The scheduler used for invoking publicly exposed delegate events.
        /// </summary>
        public Scheduler EventScheduler;

        internal IBindableList<AudioMixer> ActiveMixers => AudioActiveMixers;
        protected readonly BindableList<AudioMixer> AudioActiveMixers = new BindableList<AudioMixer>();

        private protected readonly Lazy<TrackStore> AudioGlobalTrackStore;
        private protected readonly Lazy<SampleStore> AudioGlobalSampleStore;

        /// <summary>
        /// Constructs an AudioStore given a track resource store, and a sample resource store.
        /// </summary>
        /// <param name="audioThread">The host's audio thread.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        /// <param name="config"></param>
        protected AudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore, [CanBeNull] FrameworkConfigManager config)
        {
            Prepare();

            CurrentAudioThread = audioThread;

            CurrentAudioThread.RegisterManager(this);

            if (config != null)
            {
                // attach config bindables
                config.BindWith(FrameworkSetting.AudioDevice, AudioDevice);
                config.BindWith(FrameworkSetting.AudioUseExperimentalWasapi, UseExperimentalWasapi);
                config.BindWith(FrameworkSetting.VolumeUniversal, Volume);
                config.BindWith(FrameworkSetting.VolumeEffect, VolumeSample);
                config.BindWith(FrameworkSetting.VolumeMusic, VolumeTrack);
            }

            AudioDevice.ValueChanged += _ => AudioScheduler.AddOnce(InitCurrentDevice);
            UseExperimentalWasapi.ValueChanged += _ => AudioScheduler.AddOnce(InitCurrentDevice);
            // InitCurrentDevice not required for changes to `GlobalMixerHandle` as it is only changed when experimental wasapi is toggled (handled above).

            AddItem(TrackMixer = AudioCreateAudioMixer(null, nameof(TrackMixer)));
            AddItem(SampleMixer = AudioCreateAudioMixer(null, nameof(SampleMixer)));

            AudioGlobalTrackStore = new Lazy<TrackStore>(() =>
            {
                var store = new TrackStore(trackStore, TrackMixer, GetNewTrack);
                AddItem(store);
                store.AddAdjustment(AdjustableProperty.Volume, VolumeTrack);
                return store;
            });

            AudioGlobalSampleStore = new Lazy<SampleStore>(() =>
            {
                var store = new SampleStore(sampleStore, SampleMixer, GetSampleFactory);
                AddItem(store);
                store.AddAdjustment(AdjustableProperty.Volume, VolumeSample);
                return store;
            });
        }

        protected virtual void Prepare()
        {
        }

        internal abstract Track.Track GetNewTrack(Stream data, string name);

        internal abstract SampleFactory GetSampleFactory(Stream data, string name, AudioMixer mixer, int playbackConcurrency);

        protected override void Dispose(bool disposing)
        {
            CancelSource.Cancel();

            CurrentAudioThread.UnregisterManager(this);

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
        public AudioMixer CreateAudioMixer(string identifier = null) =>
            AudioCreateAudioMixer(SampleMixer, !string.IsNullOrEmpty(identifier) ? identifier : $"user #{Interlocked.Increment(ref userMixerID)}");

        protected abstract AudioMixer AudioCreateAudioMixer(AudioMixer fallbackMixer, string identifier);

        protected override void ItemAdded(AudioComponent item)
        {
            base.ItemAdded(item);
            if (item is AudioMixer mixer)
                AudioActiveMixers.Add(mixer);
        }

        protected override void ItemRemoved(AudioComponent item)
        {
            base.ItemRemoved(item);
            if (item is AudioMixer mixer)
                AudioActiveMixers.Remove(mixer);
        }

        /// <summary>
        /// Obtains the <see cref="TrackStore"/> corresponding to a given resource store.
        /// Returns the global <see cref="TrackStore"/> if no resource store is passed.
        /// </summary>
        /// <param name="store">The <see cref="IResourceStore{T}"/> of which to retrieve the <see cref="TrackStore"/>.</param>
        /// <param name="mixer">The <see cref="AudioMixer"/> to use for tracks created by this store. Defaults to the global <see cref="TrackMixer"/>.</param>
        public ITrackStore GetTrackStore(IResourceStore<byte[]> store = null, AudioMixer mixer = null)
        {
            if (store == null) return AudioGlobalTrackStore.Value;

            TrackStore tm = new TrackStore(store, mixer ?? TrackMixer, GetNewTrack);
            AudioGlobalTrackStore.Value.AddItem(tm);
            return tm;
        }

        /// <summary>
        /// Obtains the <see cref="SampleStore"/> corresponding to a given resource store.
        /// Returns the global <see cref="SampleStore"/> if no resource store is passed.
        /// </summary>
        /// <remarks>
        /// By default, <c>.wav</c> and <c>.ogg</c> extensions will be automatically appended to lookups on the returned store
        /// if the lookup does not correspond directly to an existing filename.
        /// Additional extensions can be added via <see cref="ISampleStore.AddExtension"/>.
        /// </remarks>
        /// <param name="store">The <see cref="IResourceStore{T}"/> of which to retrieve the <see cref="SampleStore"/>.</param>
        /// <param name="mixer">The <see cref="AudioMixer"/> to use for samples created by this store. Defaults to the global <see cref="SampleMixer"/>.</param>
        public ISampleStore GetSampleStore(IResourceStore<byte[]> store = null, AudioMixer mixer = null)
        {
            if (store == null) return AudioGlobalSampleStore.Value;

            SampleStore sm = new SampleStore(store, mixer ?? SampleMixer, GetSampleFactory);
            AudioGlobalSampleStore.Value.AddItem(sm);
            return sm;
        }

        protected abstract void InitCurrentDevice();

        // The current device is considered valid if it is enabled, initialized, and not a fallback device.
        protected abstract bool IsCurrentDeviceValid();

        public abstract override string ToString();
    }
}
