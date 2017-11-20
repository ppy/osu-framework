// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using ManagedBass;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;
using System.Linq;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Audio
{
    public class AudioManager : AudioCollectionManager<AdjustableAudioComponent>
    {
        /// <summary>
        /// The manager component responsible for audio tracks (e.g. songs).
        /// </summary>
        public TrackManager Track => GetTrackManager();

        /// <summary>
        /// The manager component responsible for audio samples (e.g. sound effects).
        /// </summary>
        public SampleManager Sample => GetSampleManager();

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
        private string currentDefaultAudioDevice;

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

        private TrackManager globalTrackManager;

        private SampleManager globalSampleManager;

        /// <summary>
        /// Constructs an AudioManager given a track resource store, and a sample resource store.
        /// </summary>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        public AudioManager(ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
        {
            AudioDevice.ValueChanged += onDeviceChanged;
            trackStore.AddExtension(@"mp3");

            sampleStore.AddExtension(@"wav");
            sampleStore.AddExtension(@"mp3");

            Thread = new AudioThread(Update, @"Audio");
            Thread.Start();

            scheduler.Add(() =>
            {
                globalTrackManager = GetTrackManager(trackStore);
                globalSampleManager = GetSampleManager(sampleStore);
                initDefaultAudioDevice();
            });

            scheduler.AddDelayed(delegate
            {
                AvailableDeviceChangeListener();
                DefaultAudioDeviceChangedListener();
            }, 1000, true);
        }

        /// <summary>
        /// Obtains the <see cref="TrackManager"/> corresponding to a given resource store.
        /// Returns the global <see cref="TrackManager"/> if no resource store is passed.
        /// </summary>
        /// <param name="store">The <see cref="T:ResourceStore"/> of which to retrieve the <see cref="TrackManager"/>.</param>
        public TrackManager GetTrackManager(ResourceStore<byte[]> store = null)
        {
            if (store == null) return globalTrackManager;

            TrackManager tm = new TrackManager(store);
            AddItem(tm);
            tm.AddAdjustment(AdjustableProperty.Volume, VolumeTrack);
            VolumeTrack.ValueChanged += tm.InvalidateState;

            return tm;
        }

        /// <summary>
        /// Obtains the <see cref="SampleManager"/> corresponding to a given resource store.
        /// Returns the global <see cref="SampleManager"/> if no resource store is passed.
        /// </summary>
        /// <param name="store">The <see cref="T:ResourceStore"/> of which to retrieve the <see cref="SampleManager"/>.</param>
        public SampleManager GetSampleManager(ResourceStore<byte[]> store = null)
        {
            if (store == null) return globalSampleManager;

            SampleManager sm = new SampleManager(store);
            AddItem(sm);
            sm.AddAdjustment(AdjustableProperty.Volume, VolumeSample);
            VolumeSample.ValueChanged += sm.InvalidateState;

            return sm;
        }

        /// <summary>
        /// Returns a list of the names of recognized audio devices.
        /// </summary>
        /// <remarks>
        /// The No Sound device that is in the list of Audio Devices that are stored internally is not returned.
        /// Regarding the .Skip(1) as implementation for removing "No Sound", see http://bass.radio42.com/help/html/e5a666b4-1bdd-d1cb-555e-ce041997d52f.htm.
        /// </remarks>
        /// <returns>A list of the names of recognized audio devices.</returns>
        private IEnumerable<string> getDeviceNames(List<DeviceInfo> devices) => devices.Skip(1).Select(d => d.Name);
        public override string ToString() => $@"{GetType().ReadableName()} ({currentAudioDevice})";

        protected override void Dispose(bool disposing)
        {
            OnNewDevice = null;
            OnLostDevice = null;

            base.Dispose(disposing);
        }

        private void onDeviceChanged(string newDevice)
        {
            if (!string.IsNullOrEmpty(newDevice))
                scheduler.Add(() => changeDevice(newDevice));
            else
            {
                scheduler.Add(() =>
                {
                    if (currentDefaultAudioDevice != currentAudioDevice)
                        initDevice(getDeviceIndexFromName(currentDefaultAudioDevice), currentDefaultAudioDevice);
                });
            }
        }

        private List<DeviceInfo> getAllDevices()
        {
            int deviceCount = Bass.DeviceCount;
            List<DeviceInfo> info = new List<DeviceInfo>();
            for (int i = 0; i < deviceCount; i++)
                info.Add(Bass.GetDeviceInfo(i));
            return info;
        }

        public override void UpdateDevice(int deviceIndex)
        {
            Sample.UpdateDevice(deviceIndex);
            Track.UpdateDevice(deviceIndex);
        }

        private void initDefaultAudioDevice()
        {
            AvailableDeviceChangeListener();
            DefaultAudioDeviceChangedListener();

            var deviceIndex = getDeviceIndexFromName(currentDefaultAudioDevice);
            Bass.Init(deviceIndex);
            Bass.CurrentDevice = deviceIndex;
            Bass.PlaybackBufferLength = 100;
            Bass.UpdatePeriod = 5;
            UpdateDevice(deviceIndex);
            currentAudioDevice = string.Empty;
        }

        private void changeDevice(string deviceName)
        {
            if (string.Equals(deviceName, currentAudioDevice) != true)
            {
                var deviceIndex = getDeviceIndexFromName(deviceName);
                if (deviceIndex != -1)
                {
                    var info = Bass.GetDeviceInfo(deviceIndex);
                    initDevice(deviceIndex, info.Name);
                }
            }
        }

        private void initDevice(int deviceIndex, string deviceName)
        {
            if (Bass.Init(deviceIndex) && Bass.LastError != Errors.OK && Bass.LastError != Errors.Already)
            {
                currentAudioDevice = null;
                initDefaultAudioDevice();
            }
            else if (Bass.LastError == Errors.Already)
            {
                UpdateDevice(deviceIndex);
                currentAudioDevice = deviceName;
                Bass.CurrentDevice = deviceIndex;
                Bass.PlaybackBufferLength = 100;
                Bass.UpdatePeriod = 5;
            }
            else
            {
                UpdateDevice(deviceIndex);
                currentAudioDevice = deviceName;
                Bass.CurrentDevice = deviceIndex;
                Bass.PlaybackBufferLength = 100;
                Bass.UpdatePeriod = 5;
            }

        }

        private int getDeviceIndex(DeviceInfo device)
        {
            for (var i = 0; i < audioDevices.Count; i++)
            {
                if (audioDevices[i].Name == device.Name)
                    return i;
            }
            return -1;
        }

        private int getDeviceIndexFromName(string deviceName)
        {
            var devices = getAllDevices();
            foreach (var device in devices)
            {
                if (device.Name == deviceName)
                    return getDeviceIndex(device);
            }
            return -1;
        }

        protected void AvailableDeviceChangeListener()
        {
            var currentDeviceList = getAllDevices().Where(d => d.IsEnabled).ToList();
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
        protected void DefaultAudioDeviceChangedListener()
        {
            foreach (var d in audioDevices)
            {
                if (d.IsDefault && d.Name != currentDefaultAudioDevice)
                {
                    currentDefaultAudioDevice = d.Name;
                    initDevice(getDeviceIndexFromName(d.Name), d.Name);
                    break;
                }
            }
        }
    }
}
