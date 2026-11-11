// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using ManagedBass;
using osu.Framework.Audio.Asio;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Logging;

namespace osu.Framework.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// Whether to use native ASIO output on Windows. Falls back to the normal output device if the driver cannot start.
        /// </summary>
        public readonly BindableBool UseAsio = new BindableBool();

        /// <summary>
        /// The preferred ASIO driver name. An empty string selects the first installed driver.
        /// </summary>
        public readonly Bindable<string> AsioDevice = new Bindable<string>(string.Empty);

        /// <summary>
        /// The requested ASIO sample rate, in Hz.
        /// </summary>
        public readonly BindableInt AsioSampleRate = new BindableInt(48000) { MinValue = 8000, MaxValue = 384000 };

        /// <summary>
        /// The requested ASIO buffer length in samples, or zero for the driver's default.
        /// </summary>
        public readonly BindableInt AsioBufferSize = new BindableInt { MinValue = 0, MaxValue = 8192 };

        /// <summary>
        /// Diagnostic information about the current ASIO output or the reason for fallback.
        /// Changes are dispatched using <see cref="EventScheduler"/>.
        /// </summary>
        public IBindable<string> AsioStatus => asioStatus;

        private readonly Bindable<string> asioStatus = new Bindable<string>("ASIO is disabled.");

        /// <summary>
        /// The names of installed ASIO drivers. Driver presence does not imply the interface is connected.
        /// </summary>
        public IEnumerable<string> AsioDeviceNames => AsioOutput.GetDeviceNames();

        private void initialiseAsioBindings()
        {
            UseAsio.ValueChanged += _ => RetryAsio();
            AsioDevice.ValueChanged += _ => RetryAsio();
            AsioSampleRate.ValueChanged += _ => RetryAsio();
            AsioBufferSize.ValueChanged += _ => RetryAsio();
        }

        /// <summary>
        /// Retries the selected driver, including after reconnecting an interface.
        /// </summary>
        public void RetryAsio() => scheduler.AddOnce(initCurrentDevice);

        private bool tryInitAsio()
        {
            if (!UseAsio.Value || RuntimeInfo.OS != RuntimeInfo.Platform.Windows || DebugUtils.IsNUnitRunning)
            {
                eventScheduler.Add(() => asioStatus.Value = "ASIO is disabled.");
                return false;
            }

            try
            {
                // Configure BASS normally, but only initialise its silent decoding device.
                // The ASIO driver is the only component which opens a hardware output.
                if (!InitBass(Bass.NoSoundDevice))
                    throw new InvalidOperationException($"BASS decoder initialisation failed: {Bass.LastError}");

                string status = thread.InitAsio(AsioDevice.Value, AsioSampleRate.Value, AsioBufferSize.Value);
                UpdateDevice(Bass.NoSoundDevice);
                eventScheduler.Add(() => asioStatus.Value = status);
                Logger.Log(status);
                return true;
            }
            catch (Exception exception) when (exception is InvalidOperationException or DllNotFoundException or BadImageFormatException or NotSupportedException or EntryPointNotFoundException)
            {
                thread.FreeAsio();
                string status = $"ASIO unavailable: {exception.Message} Using the normal audio output.";
                eventScheduler.Add(() => asioStatus.Value = status);
                Logger.Error(exception, "ASIO initialisation failed; falling back to Windows audio");
                return false;
            }
        }
    }
}
