using System;
using System.Diagnostics;

namespace osu.Framework.Audio.Wasapi
{
    using osu.Framework.Audio;

    /// <summary>
    /// Minimal WASAPI backend prototype.
    /// This skeleton intentionally avoids a hard dependency on NAudio/CSCore so it compiles without adding packages.
    /// Replace the internal simulation with real NAudio/CSCore calls in subsequent PRs.
    /// </summary>
    public class WasapiAudioBackend : IAudioBackend
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private int deviceIndex = -1;
        private bool initialized;

        public string DebugInfo => $"WasapiAudioBackend (initialized={initialized}, deviceIndex={deviceIndex})";

        public void Initialize(int deviceIndex)
        {
            this.deviceIndex = deviceIndex;
            // TODO: integrate NAudio or CSCore here. For prototype we start a stopwatch to simulate device time.
            stopwatch.Restart();
            initialized = true;
        }

        public void UpdateDevice(int deviceIndex)
        {
            // TODO: switch device or reinitialize WASAPI client with the new device index.
            this.deviceIndex = deviceIndex;
            stopwatch.Restart();
        }

        public double GetDeviceTimeSeconds()
        {
            // In real backend, return device's precise output position time.
            return stopwatch.Elapsed.TotalSeconds;
        }

        public bool TryGetHardwareTimestamp(out long deviceTimestampNs)
        {
            // Real implementation should query WASAPI / audio driver for hardware timestamp.
            deviceTimestampNs = 0;
            return false;
        }

        public void Dispose()
        {
            stopwatch.Stop();
            initialized = false;
        }
    }
}
