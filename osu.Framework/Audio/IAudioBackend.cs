using System;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Abstraction for audio backend implementations (BASS, WASAPI, etc.).
    /// Implementations should expose a consistent device timebase and provide lifecycle management.
    /// </summary>
    public interface IAudioBackend : IDisposable
    {
        /// <summary>
        /// Initialize backend and open the device specified by <paramref name="deviceIndex"/>.
        /// </summary>
        void Initialize(int deviceIndex);

        /// <summary>
        /// Notify the backend that the device index or device state has changed.
        /// </summary>
        void UpdateDevice(int deviceIndex);

        /// <summary>
        /// Returns device output time in seconds. Should be monotonic and suitable as a shared timebase.
        /// </summary>
        double GetDeviceTimeSeconds();

        /// <summary>
        /// Try get a hardware timestamp in nanoseconds if the backend/device can provide it.
        /// Returns true when a valid timestamp is provided in <paramref name="deviceTimestampNs"/>.
        /// </summary>
        bool TryGetHardwareTimestamp(out long deviceTimestampNs);

        /// <summary>
        /// Backend-specific debug information for diagnostics.
        /// </summary>
        string DebugInfo { get; }
    }
}
