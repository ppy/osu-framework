// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Audio
{
    public class AudioManager : AudioCollectionManager<AudioComponent>
    {
        /// <summary>
        /// The number of BASS audio devices preceding the first real audio device.
        /// Consisting of <see cref="Bass.NoSoundDevice"/> and <see cref="bass_default_device"/>.
        /// </summary>
        protected const int BASS_INTERNAL_DEVICE_COUNT = 2;

        /// <summary>
        /// The index of the BASS audio device denoting the OS default.
        /// </summary>
        /// <remarks>
        /// See http://www.un4seen.com/doc/#bass/BASS_CONFIG_DEV_DEFAULT.html for more information on the included device.
        /// </remarks>
        private const int bass_default_device = 1;

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
        /// The names of all available audio devices.
        /// </summary>
        /// <remarks>
        /// This property does not contain the names of disabled audio devices.
        /// </remarks>
        public IEnumerable<string> AudioDeviceNames => audioDeviceNames;

        /// <summary>
        /// Is fired whenever a new audio device is discovered and provides its name.
        /// </summary>
        public event Action<string> OnNewDevice;

        /// <summary>
        /// Is fired whenever an audio device is lost and provides its name.
        /// </summary>
        public event Action<string> OnLostDevice;

        /// <summary>
        /// The preferred audio device we should use. A value of
        /// <see cref="string.Empty"/> denotes the OS default.
        /// </summary>
        public readonly Bindable<string> AudioDevice = new Bindable<string>();

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

        public override bool IsLoaded => base.IsLoaded &&
                                         // bass default device is a null device (-1), not the actual system default.
                                         Bass.CurrentDevice != Bass.DefaultDevice;

        // Mutated by multiple threads, must be thread safe.
        private ImmutableList<DeviceInfo> audioDevices = ImmutableList<DeviceInfo>.Empty;
        private ImmutableList<string> audioDeviceNames = ImmutableList<string>.Empty;

        private Scheduler scheduler => thread.Scheduler;

        private Scheduler eventScheduler => EventScheduler ?? scheduler;

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();
        private readonly DeviceInfoUpdateComparer updateComparer = new DeviceInfoUpdateComparer();

        /// <summary>
        /// The scheduler used for invoking publicly exposed delegate events.
        /// </summary>
        public Scheduler EventScheduler;

        internal IBindableList<AudioMixer> ActiveMixers => activeMixers;
        private readonly BindableList<AudioMixer> activeMixers = new BindableList<AudioMixer>();

        private readonly Lazy<TrackStore> globalTrackStore;
        private readonly Lazy<SampleStore> globalSampleStore;

        /// <summary>
        /// Constructs an AudioStore given a track resource store, and a sample resource store.
        /// </summary>
        /// <param name="audioThread">The host's audio thread.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        public AudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
        {
            thread = audioThread;

            thread.RegisterManager(this);

            AudioDevice.ValueChanged += onDeviceChanged;

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

            AddItem(TrackMixer = createAudioMixer(null, nameof(TrackMixer)));
            AddItem(SampleMixer = createAudioMixer(null, nameof(SampleMixer)));

            CancellationToken token = cancelSource.Token;

            scheduler.Add(() =>
            {
                // sync audioDevices every 1000ms
                new Thread(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            syncAudioDevices();
                            Thread.Sleep(1000);
                        }
                        catch
                        {
                        }
                    }
                })
                {
                    IsBackground = true
                }.Start();
            });
        }

        protected override void Dispose(bool disposing)
        {
            cancelSource.Cancel();

            thread.UnregisterManager(this);

            OnNewDevice = null;
            OnLostDevice = null;

            base.Dispose(disposing);
        }

        private void onDeviceChanged(ValueChangedEvent<string> args)
        {
            scheduler.Add(() => setAudioDevice(args.NewValue));
        }

        private void onDevicesChanged()
        {
            scheduler.Add(() =>
            {
                if (cancelSource.IsCancellationRequested)
                    return;

                if (!IsCurrentDeviceValid())
                    setAudioDevice();
            });
        }

        private static int userMixerID;

        /// <summary>
        /// Creates a new <see cref="AudioMixer"/>.
        /// </summary>
        /// <remarks>
        /// Channels removed from this <see cref="AudioMixer"/> fall back to the global <see cref="SampleMixer"/>.
        /// </remarks>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        public AudioMixer CreateAudioMixer(string identifier = default) =>
            createAudioMixer(SampleMixer, !string.IsNullOrEmpty(identifier) ? identifier : $"user #{Interlocked.Increment(ref userMixerID)}");

        private AudioMixer createAudioMixer(AudioMixer globalMixer, string identifier)
        {
            var mixer = new BassAudioMixer(globalMixer, identifier);
            AddItem(mixer);
            return mixer;
        }

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

        /// <summary>
        /// Obtains the <see cref="TrackStore"/> corresponding to a given resource store.
        /// Returns the global <see cref="TrackStore"/> if no resource store is passed.
        /// </summary>
        /// <param name="store">The <see cref="IResourceStore{T}"/> of which to retrieve the <see cref="TrackStore"/>.</param>
        /// <param name="mixer">The <see cref="AudioMixer"/> to use for tracks created by this store. Defaults to the global <see cref="TrackMixer"/>.</param>
        public ITrackStore GetTrackStore(IResourceStore<byte[]> store = null, AudioMixer mixer = null)
        {
            if (store == null) return globalTrackStore.Value;

            TrackStore tm = new TrackStore(store, mixer ?? TrackMixer);
            globalTrackStore.Value.AddItem(tm);
            return tm;
        }

        /// <summary>
        /// Obtains the <see cref="SampleStore"/> corresponding to a given resource store.
        /// Returns the global <see cref="SampleStore"/> if no resource store is passed.
        /// </summary>
        /// <param name="store">The <see cref="IResourceStore{T}"/> of which to retrieve the <see cref="SampleStore"/>.</param>
        /// <param name="mixer">The <see cref="AudioMixer"/> to use for samples created by this store. Defaults to the global <see cref="SampleMixer"/>.</param>
        public ISampleStore GetSampleStore(IResourceStore<byte[]> store = null, AudioMixer mixer = null)
        {
            if (store == null) return globalSampleStore.Value;

            SampleStore sm = new SampleStore(store, mixer ?? SampleMixer);
            globalSampleStore.Value.AddItem(sm);
            return sm;
        }

        /// <summary>
        /// Sets the output audio device by its name.
        /// This will automatically fall back to the system default device on failure.
        /// </summary>
        /// <param name="deviceName">Name of the audio device, or null to use the configured device preference <see cref="AudioDevice"/>.</param>
        private bool setAudioDevice(string deviceName = null)
        {
            deviceName ??= AudioDevice.Value;

            // try using the specified device
            int deviceIndex = audioDeviceNames.FindIndex(d => d == deviceName);
            if (deviceIndex >= 0 && setAudioDevice(BASS_INTERNAL_DEVICE_COUNT + deviceIndex))
                return true;

            // try using the system default if there is any device present.
            if (audioDeviceNames.Count > 0 && setAudioDevice(bass_default_device))
                return true;

            // no audio devices can be used, so try using Bass-provided "No sound" device as last resort.
            if (setAudioDevice(Bass.NoSoundDevice))
                return true;

            //we're fucked. even "No sound" device won't initialise.
            return false;
        }

        private bool setAudioDevice(int deviceIndex)
        {
            var device = audioDevices.ElementAtOrDefault(deviceIndex);

            // device is invalid
            if (!device.IsEnabled)
                return false;

            // we don't want bass initializing with real audio device on headless test runs.
            if (deviceIndex != Bass.NoSoundDevice && DebugUtils.IsNUnitRunning)
                return false;

            // initialize new device
            bool initSuccess = InitBass(deviceIndex);
            if (Bass.LastError != Errors.Already && BassUtils.CheckFaulted(false))
                return false;

            if (!initSuccess)
            {
                Logger.Log("BASS failed to initialize but did not provide an error code", level: LogLevel.Error);
                return false;
            }

            Logger.Log($@"🔈 BASS initialised
                          BASS version:           {Bass.Version}
                          BASS FX version:        {BassFx.Version}
                          BASS MIX version:       {BassMix.Version}
                          Device:                 {device.Name}
                          Driver:                 {device.Driver}
                          Update period:          {Bass.UpdatePeriod} ms
                          Device buffer length:   {Bass.DeviceBufferLength} ms
                          Playback buffer length: {Bass.PlaybackBufferLength} ms");

            //we have successfully initialised a new device.
            UpdateDevice(deviceIndex);

            return true;
        }

        /// <summary>
        /// This method calls <see cref="Bass.Init(int, int, DeviceInitFlags, IntPtr, IntPtr)"/>.
        /// It can be overridden for unit testing.
        /// </summary>
        protected virtual bool InitBass(int device)
        {
            if (Bass.CurrentDevice == device)
                return true;

            // this likely doesn't help us but also doesn't seem to cause any issues or any cpu increase.
            Bass.UpdatePeriod = 5;

            // reduce latency to a known sane minimum.
            Bass.DeviceBufferLength = 10;
            Bass.PlaybackBufferLength = 100;

            // ensure there are no brief delays on audio operations (causing stream stalls etc.) after periods of silence.
            Bass.DeviceNonStop = true;

            // without this, if bass falls back to directsound legacy mode the audio playback offset will be way off.
            Bass.Configure(ManagedBass.Configuration.TruePlayPosition, 0);

            // For iOS devices, set the default audio policy to one that obeys the mute switch.
            Bass.Configure(ManagedBass.Configuration.IOSMixAudio, 5);

            // Always provide a default device. This should be a no-op, but we have asserts for this behaviour.
            Bass.Configure(ManagedBass.Configuration.IncludeDefaultDevice, true);

            // Enable custom BASS_CONFIG_MP3_OLDGAPS flag for backwards compatibility.
            Bass.Configure((ManagedBass.Configuration)68, 1);

            // Disable BASS_CONFIG_DEV_TIMEOUT flag to keep BASS audio output from pausing on device processing timeout.
            // See https://www.un4seen.com/forum/?topic=19601 for more information.
            Bass.Configure((ManagedBass.Configuration)70, false);

            return AudioThread.InitDevice(device);
        }

        private void syncAudioDevices()
        {
            // audioDevices are updated if:
            // - A new device is added
            // - An existing device is Enabled/Disabled or set as Default
            var updatedAudioDevices = EnumerateAllDevices().ToImmutableList();
            if (audioDevices.SequenceEqual(updatedAudioDevices, updateComparer))
                return;

            audioDevices = updatedAudioDevices;

            // Bass should always be providing "No sound" and "Default" device.
            Trace.Assert(audioDevices.Count >= BASS_INTERNAL_DEVICE_COUNT, "Bass did not provide any audio devices.");

            var oldDeviceNames = audioDeviceNames;
            var newDeviceNames = audioDeviceNames = audioDevices.Skip(BASS_INTERNAL_DEVICE_COUNT).Where(d => d.IsEnabled).Select(d => d.Name).ToImmutableList();

            onDevicesChanged();

            var newDevices = newDeviceNames.Except(oldDeviceNames).ToList();
            var lostDevices = oldDeviceNames.Except(newDeviceNames).ToList();

            if (newDevices.Count > 0 || lostDevices.Count > 0)
            {
                eventScheduler.Add(delegate
                {
                    foreach (string d in newDevices)
                        OnNewDevice?.Invoke(d);
                    foreach (string d in lostDevices)
                        OnLostDevice?.Invoke(d);
                });
            }
        }

        protected virtual IEnumerable<DeviceInfo> EnumerateAllDevices()
        {
            int deviceCount = Bass.DeviceCount;
            for (int i = 0; i < deviceCount; i++)
                yield return Bass.GetDeviceInfo(i);
        }

        // The current device is considered valid if it is enabled, initialized, and not a fallback device.
        protected virtual bool IsCurrentDeviceValid()
        {
            var device = audioDevices.ElementAtOrDefault(Bass.CurrentDevice);
            bool isFallback = string.IsNullOrEmpty(AudioDevice.Value) ? !device.IsDefault : device.Name != AudioDevice.Value;
            return device.IsEnabled && device.IsInitialized && !isFallback;
        }

        public override string ToString()
        {
            string deviceName = audioDevices.ElementAtOrDefault(Bass.CurrentDevice).Name;
            return $@"{GetType().ReadableName()} ({deviceName ?? "Unknown"})";
        }

        private class DeviceInfoUpdateComparer : IEqualityComparer<DeviceInfo>
        {
            public bool Equals(DeviceInfo x, DeviceInfo y) => x.IsEnabled == y.IsEnabled && x.IsDefault == y.IsDefault;

            public int GetHashCode(DeviceInfo obj) => obj.Name.GetHashCode();
        }
    }
}
