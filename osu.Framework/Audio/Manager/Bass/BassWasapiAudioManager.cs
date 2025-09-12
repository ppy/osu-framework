// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using ManagedBass.Wasapi;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Audio.Manager.Bass
{
    using Bass = ManagedBass.Bass;

    /// <summary>
    /// An <see cref="AudioManager"/> implementation which uses BASS and WASAPI for audio output.
    /// </summary>
    public class BassWasapiAudioManager : BassAudioManager, IGlobalMixerProvider
    {
        // The device list will be like this:
        //
        // (-3: default loopback input device; just a specifier for BASS_WASAPI_Init, not included in the list, and never used in our case)
        // (-2: default input device; just a specifier for BASS_WASAPI_Init, not included in the list, and never used in our case)
        // (-1: default output device; just a specifier for BASS_WASAPI_Init, not included in the list)
        // 0: the output handle for the first real device
        // 1: the input handle for the first real device (if it has one)
        // 2...n: additional devices (if any)
        //
        // Note that the output handle and input handle for the same device has the same ID, so we can differentiate them by checking IsInput.

        public IBindable<int> GlobalMixerHandle => globalMixerHandle;

        private readonly Bindable<int> globalMixerHandle = new BindableInt();

        /// <inheritdoc />
        /// <remarks>
        /// The value is a device id. See <see cref="WasapiDeviceInfo.ID"/> for more information.
        /// </remarks>
        public override Bindable<string> AudioDevice { get; } = new Bindable<string>();

        public override ImmutableDictionary<string, string> AudioDevices { get; protected set; } = ImmutableDictionary<string, string>.Empty;

        public override string DefaultDevice => string.Empty;

        protected bool IsDefaultDevice => string.IsNullOrEmpty(AudioDevice.Value);

        private Scheduler eventScheduler => EventScheduler ?? Scheduler;

        internal Bindable<bool> Exclusive { get; } = new BindableBool();

        public override IBindable<bool> IsExclusive => Exclusive;

        // Mutated by multiple threads, must be thread safe.
        private ImmutableList<WasapiDeviceInfo> audioDevices = [];

        private readonly HashSet<string> initializedDevices = [];
        private readonly HashSet<int> initializedGlobalMixers = [];

        // This is intentionally stored to a field despite being never read.
        // If we don't do this, it gets GC'd away.
        [UsedImplicitly]
#pragma warning disable IDE0052 // Unread private member
        private WasapiNotifyProcedure? notifyProcedure;
#pragma warning restore IDE0052 // Unread private member

        public BassWasapiAudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
            : base(audioThread, trackStore, sampleStore)
        {
            AudioDevice.ValueChanged += _ => OnDeviceChanged();
            Exclusive.ValueChanged += _ => OnDeviceChanged();
            CancellationToken token = CancelSource.Token;

            syncAudioDevices();

            // This is intentionally initialised inline.
            // If we don't do this, it gets GC'd away.
            BassWasapi.SetNotify(notifyProcedure = (notify, device, _) =>
            {
                if (token.IsCancellationRequested)
                    return;

                syncAudioDevices();
            });
        }

        protected virtual void OnDeviceChanged()
        {
            Scheduler.Add(() =>
            {
                if (CancelSource.IsCancellationRequested)
                    return;

                setAudioDevice(AudioDevice.Value);
            });
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

        private bool setAudioDevice(string? deviceIdentifier = null)
        {
            deviceIdentifier ??= AudioDevice.Value;

            // Try using the specified device.
            int deviceIndex = audioDevices.FindIndex(d => !d.IsInput && !d.IsLoopback && d.ID == deviceIdentifier);
            if (setAudioDevice(deviceIndex))
                return true;

            // Try using the system default device if there is any device present.
            deviceIndex = audioDevices.FindIndex(d => !d.IsInput && !d.IsLoopback && d.IsDefault);
            if (setAudioDevice(deviceIndex))
                return true;

            // Neither the specified device nor the default device could be used.
            // There may still be candidate devices available, but we should not attempt to use them
            // without explicit user interaction because depending on the operating environment,
            // it could potentially cause nightmares like destroying the device.
            return false;
        }

        private bool setAudioDevice(int deviceIndex)
        {
            var device = audioDevices.ElementAtOrDefault(deviceIndex);

            // The device is invalid.
            if (!device.IsEnabled || device.IsInput || device.IsLoopback)
                return false;

            // Initialize new device.
            bool initSuccess = InitBass(deviceIndex);
            if (Bass.LastError != Errors.Already && BassUtils.CheckFaulted(false))
                return false;

            if (!initSuccess)
            {
                Logger.Log("BASSWASAPI failed to initialize but did not provide an error code", level: LogLevel.Error);
                return false;
            }

            Logger.Log($@"🔈 BASS initialised with WASAPI add-on
                          BASS version:        {Bass.Version}
                          BASS FX version:     {BassFx.Version}
                          BASS MIX version:    {BassMix.Version}
                          BASS WASAPI version: {BassWasapi.Version}
                          Device ID:           {device.ID}
                          Device name:         {device.Name}
                          Device mode:         {(BassWasapi.Info.IsExclusive ? "Exclusive" : "Shared")}
                          Frequency:           {BassWasapi.Info.Frequency}
                          Buffer length:       {BassWasapi.Info.BufferLength}");

            // We have successfully initialised a new device.
            UpdateDevice(Bass.NoSoundDevice);

            return true;
        }

        protected override bool IsDeviceChanged(int device) => BassWasapi.CurrentDevice != device || BassWasapi.Info.IsExclusive != Exclusive.Value;

        private void syncAudioDevices()
        {
            audioDevices = GetAllDevices();

            var oldDeviceNames = AudioDevices;
            var newDeviceNames = AudioDevices = audioDevices.Where(d => d.IsEnabled && !d.IsInput && !d.IsLoopback).ToImmutableDictionary(d => d.ID, d => d.Name);

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

        protected virtual ImmutableList<WasapiDeviceInfo> GetAllDevices()
        {
            var devices = ImmutableList.CreateBuilder<WasapiDeviceInfo>();
            for (int i = 0; BassWasapi.GetDeviceInfo(i, out var info); i++)
                devices.Add(info);

            return devices.ToImmutable();
        }

        // The current device is considered valid if it is enabled, initialized, and not a fallback device.Is
        protected virtual bool IsCurrentDeviceValid()
        {
            var device = audioDevices.ElementAtOrDefault(BassWasapi.CurrentDevice);
            bool isFallback = IsDefaultDevice ? !device.IsDefault : device.ID != AudioDevice.Value;
            return device.IsEnabled && device.IsInitialized && !isFallback;
        }

        internal override bool InitDevice(int device)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);
            // The real device ID should always be used, as the default device has special cases which are hard to work with,
            // such as the `BASS_WASAPI_DEVICE_REINIT` flag, which cannot be used with the default device.
            Trace.Assert(device != BassWasapi.DefaultDevice);

            // Try to initialise BASS first, or request a re-initialise.
            if (!Bass.Init(Bass.NoSoundDevice, Flags: DeviceInitFlags.Stereo | (DeviceInitFlags)128)) // 128 == BASS_DEVICE_REINIT
                return false;

            return InitDevice(device, false, false);
        }

        protected virtual bool InitDevice(int device, bool uncategorized, bool reinit)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);

            if (!BassWasapi.GetDeviceInfo(device, out var deviceInfo))
            {
                Logger.Log($"Failed to get WASAPI device info for {device} ({Bass.LastError})", level: LogLevel.Error);
                Bass.Free();

                return false;
            }

            int frequency = Exclusive.Value ? 44100 : deviceInfo.MixFrequency;
            float buffer = Exclusive.Value ? (float)deviceInfo.MinimumUpdatePeriod : 0;

            globalMixerHandle.Value = BassMix.CreateMixerStream(frequency, 2, BassFlags.MixerNonStop | BassFlags.Decode | BassFlags.Float);

            var flags = WasapiInitFlags.Async | WasapiInitFlags.AutoFormat | WasapiInitFlags.EventDriven;
            if (Exclusive.Value)
                flags |= WasapiInitFlags.Exclusive;
            else if (!uncategorized)
                flags |= WasapiInitFlags.Raw;

            if (reinit)
            {
                BassWasapi.CurrentDevice = device;
                BassWasapi.Stop(false);
                BassWasapi.Free();
            }

            if (!BassWasapi.InitEx(device, frequency, 2, flags, buffer, 0, BassWasapi.WasapiProc_Bass, globalMixerHandle.Value))
            {
                Errors initError = Bass.LastError;
                Bass.StreamFree(globalMixerHandle.Value);

                if (!uncategorized && initError == (Errors)5002) // 5002 == BASS_ERROR_WASAPI_CATEGORY
                {
                    // If the device not supports the requested category, we can try again without specifying it.
                    return InitDevice(device, true, reinit);
                }

                if (!reinit && initError == Errors.Already)
                {
                    // If the device is already initialised, we can try to free and reinitialise it.
                    return InitDevice(device, uncategorized, true);
                }

                Logger.Log($"Failed to initialize WASAPI device {deviceInfo.Name} ({initError})", level: LogLevel.Error);
                Bass.Stop();
                Bass.Free();

                return false;
            }

            initializedDevices.Add(deviceInfo.ID);
            initializedGlobalMixers.Add(globalMixerHandle.Value);

            return BassWasapi.Start();
        }

        internal override void FreeDevice(int device)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);

            int selectedDevice = BassWasapi.CurrentDevice;

            BassWasapi.Stop();
            Bass.StreamFree(globalMixerHandle.Value);
            Bass.Stop();

            if (canSelectDevice(device))
            {
                BassWasapi.CurrentDevice = device;
                BassWasapi.Free();
                Bass.Free();
            }

            if (selectedDevice != device && canSelectDevice(selectedDevice))
                BassWasapi.CurrentDevice = selectedDevice;

            static bool canSelectDevice(int deviceId) => BassWasapi.GetDeviceInfo(deviceId, out var deviceInfo) && deviceInfo.IsInitialized;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (int mixer in initializedGlobalMixers)
                    Bass.StreamFree(mixer);

                foreach (string deviceId in initializedDevices)
                {
                    if (audioDevices?.FindIndex(d => d.ID == deviceId) is int device and >= 0)
                        FreeDevice(device);
                }
            }

            BassWasapi.SetNotify(notifyProcedure = null);

            base.Dispose(disposing);
        }
    }
}
