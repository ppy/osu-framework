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


        /// <summary>
        /// List of all audioDevices.
        /// </summary>
        protected List<DeviceInfo> AudioDevices = new List<DeviceInfo>();

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

        /// <summary>
        /// Name of the current audio device.
        /// </summary>
        private string currentAudioDevice;

        /// <summary>
        /// Name of the current default audio device.
        /// </summary>
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
                changeDevice(string.Empty);
                Bass.PlaybackBufferLength = 100;
                Bass.UpdatePeriod = 5;
            });

            scheduler.AddDelayed(delegate
            {
                CurrentDeviceStillWorkingListener();
                AvailableDeviceChangeListener();
                DefaultAudioDeviceChangedListener();
            }, 1000, true);
        }

        /// <summary>
        /// Event fired in case of a device change.
        /// </summary>
        /// <param name="newDevice">The name of the new device to be enabled.</param>
        private void onDeviceChanged(string newDevice)
        {
            scheduler.Add(() =>
            {
                changeDevice(newDevice);
            });
        }

        /// <summary>
        /// Method that handles the audio device change.
        /// </summary>
        /// <param name="newDeviceName">The name of the new device to be enabled.</param>
        private void changeDevice(string newDeviceName)
        {
            //check if we're targetting the default device.
            if (newDeviceName.Equals(string.Empty))
            {
                var newDeviceIndex = getDeviceIndexFromName(currentDefaultAudioDevice);
                Bass.Init(newDeviceIndex);
                //try to init the default device and if it's not available it throws a Device error.
                if (Bass.LastError != Errors.Device)
                {
                    //success, new device initialized. Now update the Track and Sample active audio device
                    UpdateDevice(newDeviceIndex);
                    currentAudioDevice = string.Empty;
                }
                else
                {
                    //fallback to no sound device.
                    Bass.Init(0);
                    Bass.Stop();
                    UpdateDevice(0);
                    currentAudioDevice = "No audio devices";
                }
            }
            else
            {
                //we're not targetting the default device.
                var newDeviceIndex = getDeviceIndexFromName(newDeviceName);

                //check if the device is on our available device list.
                if (newDeviceIndex != -1)
                {
                    //get devices info in order to process the changes.
                    var newInfo = Bass.GetDeviceInfo(newDeviceIndex);
                    var currentInfo = Bass.GetDeviceInfo(Bass.CurrentDevice);
                    //if the type is 0 it means that somehow the no sound device got in there or perhaps a virtual audio device.
                    if (newInfo.Type == 0)
                    {
                        Bass.Init(0);
                        Bass.Stop();
                        UpdateDevice(0);
                        currentAudioDevice = newInfo.Name;
                    }
                    else if (!newInfo.Name.Equals(currentInfo.Name))
                    {
                        //check if the new device is not the same as the active device.
                        if (newInfo.IsInitialized != true)
                        {
                            //init the device if is not initialized.
                            Bass.Init(newDeviceIndex);
                        }
                        UpdateDevice(newDeviceIndex);
                        currentAudioDevice = newDeviceName;
                    }
                }
                else
                {
                    //fallback to no sound device.
                    Bass.Init(0);
                    Bass.Stop();
                    UpdateDevice(0);
                    currentAudioDevice = string.Empty;
                }
            }

        }
        /// <summary>
        /// Event that updates the device of Track and Sample.
        /// </summary>
        /// <param name="deviceIndex">Index of the new device.</param>
        public override void UpdateDevice(int deviceIndex)
        {
            Sample.UpdateDevice(deviceIndex);
            Track.UpdateDevice(deviceIndex);
        }
        /// <summary>
        /// Get all device infos.
        /// </summary>
        private List<DeviceInfo> getAllDevices()
        {
            int deviceCount = Bass.DeviceCount;
            List<DeviceInfo> info = new List<DeviceInfo>();
            for (int i = 0; i < deviceCount; i++)
                info.Add(Bass.GetDeviceInfo(i));
            return info;
        }

        /// <summary>
        /// Get the device index by providing DeviceInfo object.
        /// </summary>
        /// <param name="device"> The DeviceInfo Object of the audio device.</param>
        private int getDeviceIndex(DeviceInfo device)
        {
            for (var i = 0; i < AudioDevices.Count; i++)
            {
                if (AudioDevices[i].Name == device.Name)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Get the device index by providing the device name.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
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

        /// <summary>
        /// Listen to the global audio devices and updates the lists of devices.
        /// </summary>
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

            AudioDevices = currentDeviceList;
            audioDeviceNames = currentDeviceNames;
        }

        /// <summary>
        /// When the default device is selected it checks if the user changed the system default device and targets it.
        /// </summary>
        protected void DefaultAudioDeviceChangedListener()
        {
            if (currentAudioDevice.Equals(string.Empty))
            {
                var old = currentDefaultAudioDevice;
                foreach (var d in AudioDevices)
                {
                    if (d.IsDefault && d.Name != currentDefaultAudioDevice)
                    {
                        currentDefaultAudioDevice = d.Name;
                    }
                }
                if (old != null && !old.Equals(currentDefaultAudioDevice) && currentAudioDevice.Equals(string.Empty))
                {
                    //it means that the default device changed, let's trigger change device
                    scheduler.Add(() =>
                    {
                        changeDevice(string.Empty);
                    });
                }
                else if (currentAudioDevice.Equals(string.Empty) && !Bass.GetDeviceInfo(Bass.CurrentDevice).Name.Equals(currentDefaultAudioDevice))
                {
                    var newDeviceIndex = getDeviceIndexFromName(currentDefaultAudioDevice);
                    Bass.CurrentDevice = newDeviceIndex;
                    Bass.Free();
                    Bass.Init(newDeviceIndex);
                    UpdateDevice(newDeviceIndex);
                    currentAudioDevice = string.Empty;
                }
            }

        }

        /// <summary>
        /// Check if the active audio device is not broken or force disabled by the user. It falls back to the default device or to the no sound device if there is no other audio device available.
        /// </summary>
        protected void CurrentDeviceStillWorkingListener()
        {
            if (!currentAudioDevice.Equals(string.Empty))
            {
                var active = Bass.GetDeviceInfo(Bass.CurrentDevice);
                if (active.IsEnabled == false && Bass.CurrentDevice == getDeviceIndexFromName(currentAudioDevice))
                {
                    Bass.Free();
                    //fallback to default device
                    changeDevice(string.Empty);
                }
            }
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
        public override string ToString() => $@"{GetType().ReadableName()} ({currentAudioDevice})";
        protected override void Dispose(bool disposing)
        {
            OnNewDevice = null;
            OnLostDevice = null;

            base.Dispose(disposing);
        }
    }
}
