// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
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
using osu.Framework.Extensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Audio
{
    public class BassAudioManager : AudioManager
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
        /// If a global mixer is being used, this will be the BASS handle for it.
        /// If non-null, all game mixers should be added to this mixer.
        /// </summary>
        /// <remarks>
        /// When this is non-null, all mixers created via <see cref="AudioCreateAudioMixer"/>
        /// will themselves be added to the global mixer, which will handle playback itself.
        ///
        /// In this mode of operation, nested mixers will be created with the <see cref="BassFlags.Decode"/>
        /// flag, meaning they no longer handle playback directly.
        ///
        /// An eventual goal would be to use a global mixer across all platforms as it can result
        /// in more control and better playback performance.
        /// </remarks>
        internal readonly IBindable<int?> GlobalMixerHandle = new Bindable<int?>();

        public override bool IsLoaded => base.IsLoaded &&
                                         // bass default device is a null device (-1), not the actual system default.
                                         Bass.CurrentDevice != Bass.DefaultDevice;

        // Mutated by multiple threads, must be thread safe.
        private ImmutableArray<DeviceInfo> audioDevices = ImmutableArray<DeviceInfo>.Empty;

        private Scheduler eventScheduler => EventScheduler ?? CurrentAudioThread.Scheduler;

        /// <summary>
        /// Constructs an AudioStore given a track resource store, and a sample resource store.
        /// </summary>
        /// <param name="audioThread">The host's audio thread.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        public BassAudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
            : base(audioThread, trackStore, sampleStore)
        {
            GlobalMixerHandle.ValueChanged += handle =>
            {
                OnDeviceChanged();
                UsingGlobalMixer.Value = handle.NewValue.HasValue;
            };

            CancellationToken token = CancelSource.Token;

            syncAudioDevices();
            AudioScheduler.AddDelayed(() =>
            {
                // sync audioDevices every 1000ms
                new Thread(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            if (CheckForDeviceChanges(audioDevices))
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
            }, 1000);
        }

        protected void OnDevicesChanged()
        {
            AudioScheduler.Add(() =>
            {
                if (CancelSource.IsCancellationRequested)
                    return;

                if (!IsCurrentDeviceValid())
                    SetAudioDevice();
            });
        }

        internal override Track.Track GetNewTrack(Stream data, string name) => new TrackBass(data, name);

        internal override SampleFactory GetSampleFactory(Stream stream, string name, AudioMixer mixer, int playbackConcurrency)
        {
            byte[] data;

            using (stream)
                data = stream.ReadAllBytesToArray();

            return new SampleBassFactory(data, name, (BassAudioMixer)mixer, playbackConcurrency);
        }

        protected override AudioMixer AudioCreateAudioMixer(AudioMixer fallbackMixer, string identifier)
        {
            var mixer = new BassAudioMixer(this, fallbackMixer, identifier);
            AddItem(mixer);
            return mixer;
        }

        /// <summary>
        /// Sets the output audio device by its name.
        /// This will automatically fall back to the system default device on failure.
        /// </summary>
        /// <param name="deviceName">Name of the audio device, or null to use the configured device preference.</param>
        protected override bool SetAudioDevice(string deviceName = null)
        {
            deviceName ??= AudioDevice.Value;

            // try using the specified device
            int deviceIndex = DeviceNames.FindIndex(d => d == deviceName);
            if (deviceIndex >= 0 && SetAudioDevice(BASS_INTERNAL_DEVICE_COUNT + deviceIndex))
                return true;

            // try using the system default if there is any device present.
            if (DeviceNames.Count > 0 && SetAudioDevice(bass_default_device))
                return true;

            // no audio devices can be used, so try using Bass-provided "No sound" device as last resort.
            if (SetAudioDevice(Bass.NoSoundDevice))
                return true;

            // we're boned. even "No sound" device won't initialise.
            return false;
        }

        protected override bool SetAudioDevice(int deviceIndex)
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

            // Set BASS_IOS_SESSION_DISABLE here to leave session configuration in our hands (see iOS project).
            Bass.Configure(ManagedBass.Configuration.IOSSession, 16);

            // Always provide a default device. This should be a no-op, but we have asserts for this behaviour.
            Bass.Configure(ManagedBass.Configuration.IncludeDefaultDevice, true);

            // Enable custom BASS_CONFIG_MP3_OLDGAPS flag for backwards compatibility.
            // - This disables support for ItunSMPB tag parsing to match previous expectations.
            // - This also disables a change which assumes a 529 sample (2116 byte in stereo 16-bit) delay if the MP3 file doesn't specify one.
            //   (That was added in Bass for more consistent results across platforms and standard/mp3-free BASS versions, because OSX/iOS's MP3 decoder always removes 529 samples)
            Bass.Configure((ManagedBass.Configuration)68, 1);

            // Disable BASS_CONFIG_DEV_TIMEOUT flag to keep BASS audio output from pausing on device processing timeout.
            // See https://www.un4seen.com/forum/?topic=19601 for more information.
            Bass.Configure((ManagedBass.Configuration)70, false);

            if (!CurrentAudioThread.InitDevice(device))
                return false;

            return true;
        }

        private void syncAudioDevices()
        {
            audioDevices = GetAllDevices();

            // Bass should always be providing "No sound" and "Default" device.
            Trace.Assert(audioDevices.Length >= BASS_INTERNAL_DEVICE_COUNT, "Bass did not provide any audio devices.");

            var oldDeviceNames = DeviceNames;
            var newDeviceNames = DeviceNames = audioDevices.Skip(BASS_INTERNAL_DEVICE_COUNT).Where(d => d.IsEnabled).Select(d => d.Name).ToImmutableList();

            OnDevicesChanged();

            var newDevices = newDeviceNames.Except(oldDeviceNames).ToList();
            var lostDevices = oldDeviceNames.Except(newDeviceNames).ToList();

            if (newDevices.Count > 0 || lostDevices.Count > 0)
            {
                eventScheduler.Add(delegate
                {
                    foreach (string d in newDevices)
                        InvokeOnNewDevice(d);
                    foreach (string d in lostDevices)
                        InvokeOnLostDevice(d);
                });
            }
        }

        /// <summary>
        /// Check whether any audio device changes have occurred.
        ///
        /// Changes supported are:
        /// - A new device is added
        /// - An existing device is Enabled/Disabled or set as Default
        /// </summary>
        /// <remarks>
        /// This method is optimised to incur the lowest overhead possible.
        /// </remarks>
        /// <param name="previousDevices">The previous audio devices array.</param>
        /// <returns>Whether a change was detected.</returns>
        protected virtual bool CheckForDeviceChanges(ImmutableArray<DeviceInfo> previousDevices)
        {
            int deviceCount = Bass.DeviceCount;

            if (previousDevices.Length != deviceCount)
                return true;

            for (int i = 0; i < deviceCount; i++)
            {
                var prevInfo = previousDevices[i];

                Bass.GetDeviceInfo(i, out var info);

                if (info.IsEnabled != prevInfo.IsEnabled)
                    return true;

                if (info.IsDefault != prevInfo.IsDefault)
                    return true;
            }

            return false;
        }

        protected virtual ImmutableArray<DeviceInfo> GetAllDevices()
        {
            int deviceCount = Bass.DeviceCount;

            var devices = ImmutableArray.CreateBuilder<DeviceInfo>(deviceCount);
            for (int i = 0; i < deviceCount; i++)
                devices.Add(Bass.GetDeviceInfo(i));

            return devices.MoveToImmutable();
        }

        // The current device is considered valid if it is enabled, initialized, and not a fallback device.
        protected override bool IsCurrentDeviceValid()
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
    }
}
