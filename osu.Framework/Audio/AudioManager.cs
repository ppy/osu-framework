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

        private string currentAudioDevice;

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

            scheduler.Add(() =>
            {
                try
                {
                    setAudioDevice();
                }
                catch
                {
                }
            });

            scheduler.AddDelayed(checkAudioDeviceChanged, 100, true);
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
            scheduler.Add(() => setAudioDevice(string.IsNullOrEmpty(args.NewValue) ? null : args.NewValue));
        }

        /// <summary>
        /// Returns a list of the names of recognized audio devices.
        /// </summary>
        /// <remarks>
        /// The No Sound device that is in the list of Audio Devices that are stored internally is not returned.
        /// Regarding the .Skip(1) as implementation for removing "No Sound", see http://bass.radio42.com/help/html/e5a666b4-1bdd-d1cb-555e-ce041997d52f.htm.
        /// </remarks>
        /// <returns>A list of the names of recognized audio devices.</returns>
        private IEnumerable<string> getDeviceNames(IEnumerable<DeviceInfo> devices) => devices.Skip(1).Select(d => d.Name);

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

        protected virtual IEnumerable<DeviceInfo> EnumerateAllDevices()
        {
            int deviceCount = Bass.DeviceCount;
            for (int i = 0; i < deviceCount; i++)
                yield return Bass.GetDeviceInfo(i);
        }

        private string noSoundDeviceNameCache;

        /// <summary>
        /// Gets the name of Bass "No sound" device.
        /// </summary>
        /// <remarks>
        /// Although we can refer to this device by the string "No sound", this property can be used to avoid hardcoding that string.
        /// </remarks>
        private string noSoundDevice => noSoundDeviceNameCache ??= Bass.GetDeviceInfo(Bass.NoSoundDevice).Name;

        private bool setAudioDevice(string preferredDevice = null)
        {
            updateAvailableAudioDevices();

            string oldDevice = currentAudioDevice;
            string newDevice = preferredDevice;

            // use in the order: preferred device, default device, fallback "No sound" device (Bass ID: 0)
            // this allows us to initialize and continue using Bass without failing every subsequent Bass calls
            newDevice ??= audioDevices.Find(df => df.IsDefault).Name ?? noSoundDevice;

            bool oldDeviceValid = Bass.CurrentDevice >= 0;

            if (oldDeviceValid)
            {
                var oldDeviceInfo = Bass.GetDeviceInfo(Bass.CurrentDevice);
                oldDeviceValid &= oldDeviceInfo.IsEnabled && oldDeviceInfo.IsInitialized;
            }

            // same device
            if (newDevice == oldDevice && oldDeviceValid)
                return true;

            int newDeviceIndex = audioDevices.FindIndex(df => df.Name == newDevice);

            // preferred device might be unavailable
            // in that case, continue using the old device if it is working, or fall back to default device
            if (newDeviceIndex == -1)
                return oldDeviceValid || setAudioDevice();

            // initialize new device
            if (!InitBass(newDeviceIndex) && Bass.LastError != Errors.Already)
            {
                //the new device didn't go as planned. we need another option.

                if (preferredDevice == noSoundDevice)
                {
                    //we're fucked. even "No sound" device won't initialise.
                    currentAudioDevice = null;
                    return false;
                }

                // preferred device failed: let's try using the default device.
                if (preferredDevice != null)
                    return setAudioDevice();

                // default device failed: let's try using "No sound" device.
                return setAudioDevice(noSoundDevice);
            }

            if (Bass.LastError == Errors.Already)
            {
                // We check if the initialization error is that we already initialized the device
                // If it is, it means we can just tell Bass to use the already initialized device without much
                // other fuzz.
                Bass.CurrentDevice = newDeviceIndex;
                Bass.Free();
                InitBass(newDeviceIndex);
            }

            Trace.Assert(Bass.LastError == Errors.OK);

            var newDeviceInfo = Bass.GetDeviceInfo(newDeviceIndex);

            Logger.Log($@"BASS Initialized
                          BASS Version:               {Bass.Version}
                          BASS FX Version:            {ManagedBass.Fx.BassFx.Version}
                          Device:                     {newDeviceInfo.Name}
                          Drive:                      {newDeviceInfo.Driver}");

            //we have successfully initialised a new device.
            currentAudioDevice = newDevice;

            UpdateDevice(newDeviceIndex);

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
            var currentDeviceList = EnumerateAllDevices().Where(d => d.IsEnabled).ToList();
            var currentDeviceNames = getDeviceNames(currentDeviceList).ToList();

            var newDevices = currentDeviceNames.Except(audioDeviceNames).ToList();
            var lostDevices = audioDeviceNames.Except(currentDeviceNames).ToList();

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

            audioDevices = currentDeviceList;
            audioDeviceNames = currentDeviceNames;
        }

        private void checkAudioDeviceChanged()
        {
            try
            {
                updateAvailableAudioDevices();

                var preferred = string.IsNullOrEmpty(AudioDevice.Value) ? null : AudioDevice.Value;

                var currentIndex = audioDevices.FindIndex(d => d.IsEnabled && d.Name == currentAudioDevice);

                // current audio device became unavailable
                if (currentIndex == -1)
                {
                    setAudioDevice(preferred);
                    return;
                }

                // preferred audio device, or a default device, became available
                var preferredIndex = preferred == null
                    ? audioDevices.FindIndex(d => d.IsDefault)
                    : audioDevices.FindIndex(d => d.IsEnabled && d.Name == preferred);

                if (preferredIndex != -1 && currentIndex != preferredIndex)
                    setAudioDevice(preferred);
            }
            catch
            {
            }
        }

        public override string ToString() => $@"{GetType().ReadableName()} ({currentAudioDevice})";
    }
}
