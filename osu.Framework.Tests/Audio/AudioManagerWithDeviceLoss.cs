// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using ManagedBass;
using osu.Framework.Audio;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    /// <summary>
    /// <see cref="AudioManager"/> that can simulate the loss of a device.
    /// This will NOT work without a physical audio device!
    /// </summary>
    internal class AudioManagerWithDeviceLoss : AudioManager
    {
        public AudioManagerWithDeviceLoss(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
            : base(audioThread, trackStore, sampleStore)
        {
        }

        public volatile int CurrentDevice = Bass.DefaultDevice;

        private volatile bool simulateLoss;

        protected override bool InitBass(int device)
        {
            try
            {
                if (simulateLoss)
                    return device == Bass.NoSoundDevice && base.InitBass(device);

                return base.InitBass(device);
            }
            finally
            {
                CurrentDevice = Bass.CurrentDevice;
            }
        }

        protected override IEnumerable<DeviceInfo> EnumerateAllDevices()
        {
            var devices = base.EnumerateAllDevices();

            if (simulateLoss)
                devices = devices.Take(BASS_INTERNAL_DEVICE_COUNT);

            return devices;
        }

        protected override bool IsCurrentDeviceValid()
        {
            if (simulateLoss)
                return CurrentDevice == Bass.NoSoundDevice && base.IsCurrentDeviceValid();

            return CurrentDevice != Bass.NoSoundDevice && base.IsCurrentDeviceValid();
        }

        public void SimulateDeviceLoss()
        {
            int current = CurrentDevice;

            simulateLoss = true;

            if (current != Bass.NoSoundDevice)
                WaitForDeviceChange(current);
        }

        public void SimulateDeviceRestore()
        {
            int current = CurrentDevice;

            simulateLoss = false;

            if (current == Bass.NoSoundDevice)
                WaitForDeviceChange(current);
        }

        public void WaitForDeviceChange(int? current = null, int timeoutMs = 5000)
        {
            current ??= CurrentDevice;

            AudioThreadTest.WaitForOrAssert(() => CurrentDevice != current, $"Timed out while waiting for the device to change from {current}.", timeoutMs);
        }
    }
}
