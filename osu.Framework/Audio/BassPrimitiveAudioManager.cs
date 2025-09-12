// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Audio
{
    using Bass = ManagedBass.Bass;

    /// <summary>
    /// An <see cref="AudioManager"/> implementation which uses BASS for audio playback without any additional add-ons (so we call it "primitive").
    /// </summary>
    public class BassPrimitiveAudioManager : BassAudioManager
    {
        // The device list will be like this:
        //
        // (-1: default device; just a specifier for BASS_Init, not included in the list)
        // 0: "No sound"; no sound device
        // 1: "Default"; default device (not a real output device in actual, regardless of what is written in the document for `BASS_Init` because of `BASS_CONFIG_DEV_DEFAULT` which is enabled by default and also assumed to be enabled)
        // 2: first real output device
        // 3...n: additional output devices (if any)
        //
        // so the first real output device will always be at index 2.

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

        /// <inheritdoc />
        /// <remarks>
        /// The value is a driver identifier. See <see cref="DeviceInfo.Driver"/> for more information.
        /// </remarks>
        public override Bindable<string> AudioDevice { get; } = new Bindable<string>();

        public override ImmutableDictionary<string, string> AudioDevices { get; protected set; } = ImmutableDictionary<string, string>.Empty;

        public override string DefaultDevice => string.Empty;

        /// <inheritdoc />
        /// <remarks>
        /// The value is always false as primitive BASS does not support exclusive mode.
        /// </remarks>
        public override IBindable<bool> IsExclusive => new BindableBool();

        protected bool IsDefaultDevice => string.IsNullOrEmpty(AudioDevice.Value);

        public override bool IsLoaded => base.IsLoaded &&
                                         // bass default device is a null device (-1), not the actual system default.
                                         Bass.CurrentDevice != Bass.DefaultDevice;

        private Scheduler eventScheduler => EventScheduler ?? Scheduler;

        // Mutated by multiple threads, must be thread safe.
        private ImmutableList<DeviceInfo> audioDevices = [];

        private readonly HashSet<string> initializedDevices = [];

        public BassPrimitiveAudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
            : base(audioThread, trackStore, sampleStore)
        {
            AudioDevice.ValueChanged += _ => OnDeviceChanged();

            CancellationToken token = CancelSource.Token;

            syncAudioDevices();
            // Primitive BASS does not provide device change notifications, so we have to poll for changes.
            eventScheduler.AddDelayed(() =>
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

        protected virtual void OnDeviceChanged()
        {
            Scheduler.Add(() => setAudioDevice(AudioDevice.Value));
        }

        protected virtual void OnDevicesChanged()
        {
            Scheduler.Add(() =>
            {
                if (CancelSource.IsCancellationRequested)
                    return;

                if (!IsCurrentDeviceValid())
                    setAudioDevice();
            });
        }

        /// <summary>
        /// Sets the output audio device by its name.
        /// This will automatically fall back to the system default device on failure.
        /// </summary>
        /// <param name="deviceIdentifier">Identifier of the audio device, or null to use the configured device preference <see cref="AudioDevice"/>.</param>
        private bool setAudioDevice(string? deviceIdentifier = null)
        {
            deviceIdentifier ??= AudioDevice.Value;

            // Try using the specified device
            int deviceIndex = audioDevices.FindIndex(d => d.Driver == deviceIdentifier);
            if (deviceIndex >= BASS_INTERNAL_DEVICE_COUNT && setAudioDevice(deviceIndex))
                return true;

            // Try using the system default if there is any device present.
            // Mobiles are an exception as the built-in speakers may not be provided as an audio device name,
            // but they are still provided by BASS under the internal device name "Default".
            if ((AudioDevices.Count > 0 || RuntimeInfo.IsMobile) && setAudioDevice(bass_default_device))
                return true;

            // No audio devices can be used, so try using Bass-provided "No sound" device as last resort.
            if (setAudioDevice(Bass.NoSoundDevice))
                return true;

            // We're boned. Even "No sound" device won't initialise.
            return false;
        }

        private bool setAudioDevice(int deviceIndex)
        {
            var device = audioDevices.ElementAtOrDefault(deviceIndex);

            // The device is invalid.
            if (!device.IsEnabled)
                return false;

            // We don't want bass initializing with real audio device on headless test runs.
            if (deviceIndex != Bass.NoSoundDevice && DebugUtils.IsNUnitRunning)
                return false;

            // Initialize new device.
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

            // We have successfully initialised a new device.
            UpdateDevice(deviceIndex);

            return true;
        }

        protected override bool IsDeviceChanged(int device) => Bass.CurrentDevice != device;

        private void syncAudioDevices()
        {
            audioDevices = GetAllDevices();

            // Bass should always be providing "No sound" and "Default" device.
            Trace.Assert(audioDevices.Count >= BASS_INTERNAL_DEVICE_COUNT, "BASS did not provide any audio devices.");

            var oldDeviceNames = AudioDevices;
            var newDeviceNames = AudioDevices = audioDevices.Skip(BASS_INTERNAL_DEVICE_COUNT).Where(d => d.IsEnabled && d.Driver != null).ToImmutableDictionary(d => d.Driver, d => d.Name);

            OnDevicesChanged();

            IReadOnlyCollection<KeyValuePair<string, string>> newDevices = [.. newDeviceNames.ExceptBy(oldDeviceNames.Keys, d => d.Key)];
            IReadOnlyCollection<KeyValuePair<string, string>> lostDevices = [.. oldDeviceNames.ExceptBy(newDeviceNames.Keys, d => d.Key)];

            if (newDevices.Count > 0 || lostDevices.Count > 0)
            {
                eventScheduler.Add(() =>
                {
                    foreach (KeyValuePair<string, string> d in newDevices)
                        InvokeOnNewDevice(d);
                    foreach (KeyValuePair<string, string> d in lostDevices)
                        InvokeOnNewDevice(d);
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
        protected virtual bool CheckForDeviceChanges(ImmutableList<DeviceInfo> previousDevices)
        {
            int deviceCount = Bass.DeviceCount;

            if (previousDevices.Count != deviceCount)
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

        protected virtual ImmutableList<DeviceInfo> GetAllDevices()
        {
            var devices = ImmutableList.CreateBuilder<DeviceInfo>();
            for (int i = 0; Bass.GetDeviceInfo(i, out var info); i++)
                devices.Add(info);

            return devices.ToImmutable();
        }

        // The current device is considered valid if it is enabled, initialized, and not a fallback device.
        protected virtual bool IsCurrentDeviceValid()
        {
            var device = audioDevices.ElementAtOrDefault(Bass.CurrentDevice);
            bool isFallback = IsDefaultDevice ? !device.IsDefault : device.Driver != AudioDevice.Value;
            return device.IsEnabled && device.IsInitialized && !isFallback;
        }

        internal override bool InitDevice(int device)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);
            // The real device ID should always be used, as the default device has special cases which are hard to work with,
            // such as the `BASS_DEVICE_REINIT` flag, which cannot be used with the default device.
            Trace.Assert(device != Bass.DefaultDevice);

            // Try to initialise the device, or request a re-initialise.
            if (!Bass.Init(device, Flags: DeviceInitFlags.Stereo | (DeviceInitFlags)128)) // 128 == BASS_DEVICE_REINIT
                return false;

            initializedDevices.Add(Bass.GetDeviceInfo(device).Driver);

            return true;
        }

        internal override void FreeDevice(int device)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);

            int selectedDevice = Bass.CurrentDevice;

            if (canSelectDevice(device))
            {
                Bass.CurrentDevice = device;
                Bass.Free();
            }

            if (selectedDevice != device && canSelectDevice(selectedDevice))
                Bass.CurrentDevice = selectedDevice;

            static bool canSelectDevice(int device) => Bass.GetDeviceInfo(device, out var deviceInfo) && deviceInfo.IsInitialized;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (string driver in initializedDevices)
                {
                    if (audioDevices?.FindIndex(d => d.Driver == driver) is int device and >= 0)
                        FreeDevice(device);
                }
            }

            base.Dispose(disposing);
        }

        public override string ToString()
        {
            string deviceName = audioDevices.ElementAtOrDefault(Bass.CurrentDevice).Name;
            return $@"{GetType().ReadableName()} ({deviceName ?? "Unknown"})";
        }
    }
}
