// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
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
using System.Diagnostics;

namespace osu.Framework.Audio
{
    public class AudioManager : AudioCollectionManager<AdjustableAudioComponent>
    {
        public TrackManager Track => GetTrackManager();
        public SampleManager Sample => GetSampleManager();

        internal GameThread Thread;

        internal event Action AvailableDevicesChanged;

        internal List<DeviceInfo> AudioDevices = new List<DeviceInfo>();

        private List<string> audioDeviceNames = new List<string>();
        public IEnumerable<string> AudioDeviceNames => audioDeviceNames;

        public readonly Bindable<string> AudioDevice = new Bindable<string>();

        internal string CurrentAudioDevice;

        private string lastPreferredDevice;

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

        /// <summary>
        /// Constructs an AudioManager given an event scheduler, a track resource store, and a sample resource store.
        /// </summary>
        /// If <see cref="null"/> is passed, then the audio thread's own scheduler is used.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        public AudioManager(ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
        {
            AudioDevice.ValueChanged += onDeviceChanged;

            trackStore.AddExtension(@"mp3");

            sampleStore.AddExtension(@"wav");
            sampleStore.AddExtension(@"mp3");

            Thread = new GameThread(Update, @"Audio");
            Thread.Start();

            scheduler.Add(() =>
            {
                globalTrackManager = GetTrackManager(trackStore);
                globalSampleManager = GetSampleManager(sampleStore);

                try
                {
                    SetAudioDevice();
                }
                catch
                {
                }
            });

            scheduler.AddDelayed(delegate
            {
                updateAudioDevices();
                checkAudioDeviceChanged();
            }, 1000, true);
        }

        private void onDeviceChanged(object sender, EventArgs e)
        {
            scheduler.Add(() => SetAudioDevice(string.IsNullOrEmpty(AudioDevice.Value) ? null : AudioDevice.Value));
        }

        private TrackManager globalTrackManager;
        private SampleManager globalSampleManager;

        /// <summary>
        /// Returns a list of the names of recognized audio devices.
        /// </summary>
        /// <remarks>The No Sound device that is in the list of Audio Devices that are stored internally is not returned.</remarks>
        /// <returns>A list of the names of recognized audio devices.</returns>
        // Regarding the .Skip(1) as implementation for removing "No Sound", see http://bass.radio42.com/help/html/e5a666b4-1bdd-d1cb-555e-ce041997d52f.htm.
        private IEnumerable<string> getDeviceNames(List<DeviceInfo> devices) => devices.Skip(1).Select(d => d.Name);

        public TrackManager GetTrackManager(ResourceStore<byte[]> store = null)
        {
            if (store == null) return globalTrackManager;

            TrackManager tm = new TrackManager(store);
            AddItem(tm);
            tm.AddAdjustment(AdjustableProperty.Volume, VolumeTrack);

            return tm;
        }

        public SampleManager GetSampleManager(ResourceStore<byte[]> store = null)
        {
            if (store == null) return globalSampleManager;

            SampleManager sm = new SampleManager(store);
            AddItem(sm);
            sm.AddAdjustment(AdjustableProperty.Volume, VolumeSample);

            return sm;
        }

        internal bool CheckAudioDevice()
        {
            if (CurrentAudioDevice != null)
                return true;

            //NotificationManager.ShowMessage("No compatible audio device detected. You must plug in a valid audio device in order to play osu!", Color4.Red, 4000);
            return false;
        }

        private List<DeviceInfo> getAllDevices()
        {
            int deviceCount = Bass.DeviceCount;
            List<DeviceInfo> info = new List<DeviceInfo>();
            for (int i = 0; i < deviceCount; i++)
                info.Add(Bass.GetDeviceInfo(i));

            return info;
        }

        public bool SetAudioDevice(string preferredDevice = null)
        {
            lastPreferredDevice = preferredDevice;

            updateAudioDevices();
            AvailableDevicesChanged?.Invoke();

            string oldDevice = CurrentAudioDevice;
            string newDevice = preferredDevice;

            if (string.IsNullOrEmpty(newDevice))
                newDevice = AudioDevices.Find(df => df.IsDefault).Name;

            bool oldDeviceValid = Bass.CurrentDevice >= 0;
            if (oldDeviceValid)
            {
                DeviceInfo oldDeviceInfo = Bass.GetDeviceInfo(Bass.CurrentDevice);
                oldDeviceValid &= oldDeviceInfo.IsEnabled && oldDeviceInfo.IsInitialized;
            }

            if (newDevice == oldDevice)
            {
                //check the old device is still valid
                if (oldDeviceValid)
                    return true;
            }

            if (string.IsNullOrEmpty(newDevice))
                return false;

            int newDeviceIndex = AudioDevices.FindIndex(df => df.Name == newDevice);

            DeviceInfo newDeviceInfo = new DeviceInfo();

            try
            {
                if (newDeviceIndex >= 0)
                    newDeviceInfo = Bass.GetDeviceInfo(newDeviceIndex);
                //we may have previously initialised this device.
            }
            catch
            {
            }

            if (oldDeviceValid && (newDeviceInfo.Driver == null || !newDeviceInfo.IsEnabled))
            {
                //handles the case we are trying to load a user setting which is currently unavailable,
                //and we have already fallen back to a sane default.
                return true;
            }

            if (!Bass.Init(newDeviceIndex) && Bass.LastError != Errors.Already)
            {
                //the new device didn't go as planned. we need another option.

                if (preferredDevice == null)
                {
                    //we're fucked. the default device won't initialise.
                    CurrentAudioDevice = null;
                    return false;
                }

                //let's try again using the default device.
                return SetAudioDevice();
            }
            else if(Bass.LastError == Errors.Already)
            {
                // We check if the initialization error is that we already initialized the device
                // If it is, it means we can just tell Bass to use the already initialized device without much
                // other fuzz.
                Bass.CurrentDevice = newDeviceIndex;
                Bass.Free();
                Bass.Init(newDeviceIndex);
            }

            Debug.Assert(Bass.LastError == Errors.OK);

            //we have successfully initialised a new device.
            CurrentAudioDevice = newDevice;

            UpdateDevice(newDeviceIndex);

            Bass.PlaybackBufferLength = 100;
            Bass.UpdatePeriod = 5;

            return true;
        }

        public override void UpdateDevice(int newDeviceIndex)
        {
            Sample.UpdateDevice(newDeviceIndex);
            Track.UpdateDevice(newDeviceIndex);
        }

        /// <summary>
        /// Is fired whenever a new audio device is discovered and provides its name.
        /// </summary>
        public event Action<string> OnNewDevice;

        /// <summary>
        /// Is fired whenever an audio device is lost and provides its name.
        /// </summary>
        public event Action<string> OnLostDevice;

        private void updateAudioDevices()
        {
            var newDeviceList = new List<DeviceInfo>(getAllDevices());
            var newDeviceNames = getDeviceNames(newDeviceList).ToList();

            var newDevices = newDeviceNames.Except(audioDeviceNames).ToList();
            var lostDevices = audioDeviceNames.Except(newDeviceNames).ToList();

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

            AudioDevices = newDeviceList;
            audioDeviceNames = newDeviceNames;
        }

        private void checkAudioDeviceChanged()
        {
            if (AudioDevice.Value == string.Empty)
            {
                // use default device
                var device = Bass.GetDeviceInfo(Bass.CurrentDevice);
                if (!device.IsDefault && !SetAudioDevice())
                {
                    if (!device.IsEnabled || !SetAudioDevice(device.Name))
                    {
                        foreach (var d in getAllDevices())
                        {
                            if (d.Name == device.Name || !d.IsEnabled)
                                continue;

                            if (SetAudioDevice(d.Name))
                                break;
                        }
                    }
                }
            }
            else
            {
                // use whatever is the preferred device
                var device = Bass.GetDeviceInfo(Bass.CurrentDevice);
                if (device.Name == AudioDevice.Value)
                {
                    if (!device.IsEnabled && !SetAudioDevice())
                    {
                        foreach (var d in getAllDevices())
                        {
                            if (d.Name == device.Name || !d.IsEnabled)
                                continue;

                            if (SetAudioDevice(d.Name))
                                break;
                        }
                    }
                }
                else
                {
                    var preferredDevice = getAllDevices().SingleOrDefault<DeviceInfo>(d => d.Name == AudioDevice.Value);
                    if (preferredDevice.Name == AudioDevice.Value && preferredDevice.IsEnabled)
                        SetAudioDevice(preferredDevice.Name);
                    else if (!device.IsEnabled && !SetAudioDevice())
                    {
                        foreach (var d in getAllDevices())
                        {
                            if (d.Name == device.Name || !d.IsEnabled)
                                continue;

                            if (SetAudioDevice(d.Name))
                                break;
                        }
                    }
                }
            }
        }
    }
}
