// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using ManagedBass;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;
using System.Linq;
using System.Diagnostics;
using osu.Framework.Bindables;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Logging;

namespace osu.Framework.Audio
{
    public class AudioManager : AudioCollectionManager<AdjustableAudioComponent>
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
        internal readonly AudioThread Thread;

        private List<DeviceInfo> audioDevices = new List<DeviceInfo>();
        private List<string> audioDeviceNames = new List<string>();

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

        private Scheduler scheduler => Thread.Scheduler;

        private Scheduler eventScheduler => EventScheduler ?? scheduler;

        /// <summary>
        /// The scheduler used for invoking publicly exposed delegate events.
        /// </summary>
        public Scheduler EventScheduler;

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
            Thread = audioThread;

            Thread.RegisterManager(this);

            AudioDevice.ValueChanged += onDeviceChanged;

            globalTrackStore = new Lazy<TrackStore>(() =>
            {
                var store = new TrackStore(trackStore);
                AddItem(store);
                store.AddAdjustment(AdjustableProperty.Volume, VolumeTrack);
                return store;
            });

            globalSampleStore = new Lazy<SampleStore>(() =>
            {
                var store = new SampleStore(sampleStore);
                AddItem(store);
                store.AddAdjustment(AdjustableProperty.Volume, VolumeSample);
                return store;
            });

            // check for device validity every 100ms
            scheduler.AddDelayed(() =>
            {
                try
                {
                    if (!IsCurrentDeviceValid())
                        setAudioDevice();
                }
                catch
                {
                }
            }, 100, true);

            // enumerate new list of devices every second
            scheduler.AddDelayed(() =>
            {
                try
                {
                    setAudioDevice(AudioDevice.Value);
                }
                catch
                {
                }
            }, 1000, true);
        }

        protected override void Dispose(bool disposing)
        {
            Thread.UnregisterManager(this);

            OnNewDevice = null;
            OnLostDevice = null;

            base.Dispose(disposing);
        }

        private void onDeviceChanged(ValueChangedEvent<string> args)
        {
            scheduler.Add(() => setAudioDevice(args.NewValue));
        }

        /// <summary>
        /// Obtains the <see cref="TrackStore"/> corresponding to a given resource store.
        /// Returns the global <see cref="TrackStore"/> if no resource store is passed.
        /// </summary>
        /// <param name="store">The <see cref="IResourceStore{T}"/> of which to retrieve the <see cref="TrackStore"/>.</param>
        public ITrackStore GetTrackStore(IResourceStore<byte[]> store = null)
        {
            if (store == null) return globalTrackStore.Value;

            TrackStore tm = new TrackStore(store);
            globalTrackStore.Value.AddItem(tm);
            return tm;
        }

        /// <summary>
        /// Obtains the <see cref="SampleStore"/> corresponding to a given resource store.
        /// Returns the global <see cref="SampleStore"/> if no resource store is passed.
        /// </summary>
        /// <param name="store">The <see cref="IResourceStore{T}"/> of which to retrieve the <see cref="SampleStore"/>.</param>
        public ISampleStore GetSampleStore(IResourceStore<byte[]> store = null)
        {
            if (store == null) return globalSampleStore.Value;

            SampleStore sm = new SampleStore(store);
            globalSampleStore.Value.AddItem(sm);
            return sm;
        }

        private DeviceInfo currentAudioDevice;

        /// <summary>
        /// Sets the output audio device by its name.
        /// This will automatically fall back to the system default device on failure.
        /// </summary>
        /// <param name="deviceName">Name of the audio device, or null to use the configured device preference <see cref="AudioDevice"/>.</param>
        private bool setAudioDevice(string deviceName = null)
        {
            updateAvailableAudioDevices();

            deviceName ??= AudioDevice.Value;

            // try using the specified device
            if (setAudioDevice(audioDevices.FindIndex(d => d.Name == deviceName)))
                return true;

            // try using the system default device
            if (setAudioDevice(audioDevices.FindIndex(d => d.Name != deviceName && d.IsDefault)))
                return true;

            // no audio devices can be used, so try using Bass-provided "No sound" device as last resort
            if (setAudioDevice(Bass.NoSoundDevice))
                return true;

            //we're fucked. even "No sound" device won't initialise.
            currentAudioDevice = default;
            return false;
        }

        private bool setAudioDevice(int deviceIndex)
        {
            var device = audioDevices.ElementAtOrDefault(deviceIndex);

            // device is invalid
            if (!device.IsEnabled)
                return false;

            // same device
            if (device.IsInitialized && device.Name == currentAudioDevice.Name)
                return true;

            // initialize new device
            if (!InitBass(deviceIndex) && Bass.LastError != Errors.Already)
                return false;

            if (Bass.LastError == Errors.Already)
            {
                // We check if the initialization error is that we already initialized the device
                // If it is, it means we can just tell Bass to use the already initialized device without much
                // other fuzz.
                Bass.CurrentDevice = deviceIndex;
                Bass.Free();
                InitBass(deviceIndex);
            }

            Trace.Assert(Bass.LastError == Errors.OK);

            Logger.Log($@"BASS Initialized
                          BASS Version:               {Bass.Version}
                          BASS FX Version:            {ManagedBass.Fx.BassFx.Version}
                          Device:                     {device.Name}
                          Drive:                      {device.Driver}");

            //we have successfully initialised a new device.
            currentAudioDevice = device;

            UpdateDevice(deviceIndex);

            Bass.PlaybackBufferLength = 100;
            Bass.UpdatePeriod = 5;

            return true;
        }

        /// <summary>
        /// This method calls <see cref="Bass.Init(int, int, DeviceInitFlags, IntPtr, IntPtr)"/>.
        /// It can be overridden for unit testing.
        /// </summary>
        protected virtual bool InitBass(int device) => Bass.Init(device);

        private void updateAvailableAudioDevices()
        {
            audioDevices = EnumerateAllDevices().ToList();

            // Bass should always be providing "No sound" device
            Trace.Assert(audioDevices.Count > 0, "Bass did not provide any audio devices.");

            var oldDeviceNames = audioDeviceNames;
            var newDeviceNames = audioDeviceNames = audioDevices.Skip(1).Where(d => d.IsEnabled).Select(d => d.Name).ToList();

            var newDevices = newDeviceNames.Except(oldDeviceNames).ToList();
            var lostDevices = oldDeviceNames.Except(newDeviceNames).ToList();

            if (newDevices.Count > 0 || lostDevices.Count > 0)
            {
                eventScheduler.Add(delegate
                {
                    foreach (var d in newDevices)
                        OnNewDevice?.Invoke(d);
                    foreach (var d in lostDevices)
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

        protected virtual bool IsCurrentDeviceValid()
        {
            var deviceIndex = Bass.CurrentDevice;
            var device = deviceIndex == Bass.DefaultDevice ? default : Bass.GetDeviceInfo(deviceIndex);

            return device.IsEnabled && device.IsInitialized;
        }

        public override string ToString() => $@"{GetType().ReadableName()} ({currentAudioDevice})";
    }
}
