// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using ManagedBass;
using osu.Framework.Audio;
using osu.Framework.Extensions.IEnumerableExtensions;
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

        public volatile int CurrentDevice = -1;
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

        protected override IEnumerable<DeviceInfo> EnumerateAllDevices() => simulateLoss ? Bass.GetDeviceInfo(Bass.NoSoundDevice).Yield() : base.EnumerateAllDevices();

        protected override bool IsCurrentDeviceValid()
        {
            bool hasCorrectDevice = simulateLoss
                ? Bass.CurrentDevice == Bass.NoSoundDevice
                : Bass.CurrentDevice != Bass.NoSoundDevice;

            return hasCorrectDevice && base.IsCurrentDeviceValid();
        }

        public void SimulateDeviceLoss()
        {
            var current = CurrentDevice;

            simulateLoss = true;

            if (current != Bass.NoSoundDevice)
                WaitForDeviceChange(current);
        }

        public void SimulateDeviceRestore()
        {
            var current = CurrentDevice;

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
