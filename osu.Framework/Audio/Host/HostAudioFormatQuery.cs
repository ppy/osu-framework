// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Audio.Asio;
using osu.Framework.Audio.Windows;

namespace osu.Framework.Audio.Host
{
    /// <summary>
    /// Cross-platform read-only queries for device-reported playback formats.
    /// Uses OS APIs (Core Audio, PulseAudio, WASAPI) — does not probe candidate sample rates.
    /// </summary>
    public static class HostAudioFormatQuery
    {
        /// <summary>
        /// Returns formats reported by the OS for a playback endpoint identified by BASS driver id and/or device name.
        /// </summary>
        public static IReadOnlyList<EzAsioFormatOption> GetReportedPlaybackFormats(string? bassDriverId, string? bassDeviceName = null)
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    return WindowsAudioFormatQuery.GetReportedPlaybackFormats(bassDriverId);

                case RuntimeInfo.Platform.macOS:
                    return MacOSHostAudioFormatQuery.GetReportedPlaybackFormats(bassDeviceName);

                case RuntimeInfo.Platform.Linux:
                    return LinuxHostAudioFormatQuery.GetReportedPlaybackFormats(bassDeviceName);

                default:
                    return Array.Empty<EzAsioFormatOption>();
            }
        }

        /// <summary>
        /// Resolves a logical device (e.g. ASIO) to a host playback endpoint and returns OS-reported formats.
        /// </summary>
        public static IReadOnlyList<EzAsioFormatOption> GetReportedFormatsForMatchedDevice(string logicalDeviceName, IEnumerable<string> bassPlaybackDeviceNames)
        {
            string? bassName = HostDeviceMatcher.FindBestBassDeviceName(logicalDeviceName, bassPlaybackDeviceNames);

            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    return WindowsAudioFormatQuery.GetReportedFormatsForAsioDevice(logicalDeviceName, bassPlaybackDeviceNames);

                case RuntimeInfo.Platform.macOS:
                case RuntimeInfo.Platform.Linux:
                    if (string.IsNullOrEmpty(bassName))
                        return GetReportedPlaybackFormats(null, null);

                    return GetReportedPlaybackFormats(null, bassName);

                default:
                    return Array.Empty<EzAsioFormatOption>();
            }
        }

        /// <summary>
        /// Returns the default system playback mix format, when available.
        /// </summary>
        public static (int sampleRate, int bitDepth)? TryGetDefaultPlaybackFormat()
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    var waveFormat = WindowsAudioFormatQuery.TryGetDefaultPlaybackMixFormat();
                    return waveFormat == null ? null : (waveFormat.SampleRate, NormaliseBits(waveFormat.BitsPerSample));

                case RuntimeInfo.Platform.macOS:
                    return MacOSHostAudioFormatQuery.TryGetDefaultPlaybackFormat();

                case RuntimeInfo.Platform.Linux:
                    return LinuxHostAudioFormatQuery.TryGetDefaultPlaybackFormat();

                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns mix format for a BASS driver id (Windows) or device name (macOS/Linux).
        /// </summary>
        public static (int sampleRate, int channels)? TryGetMixFormat(string? bassDriverId, string? bassDeviceName = null)
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    var waveFormat = WindowsAudioFormatQuery.TryGetMixFormatForDriver(bassDriverId ?? string.Empty);
                    return waveFormat == null ? null : (waveFormat.SampleRate, waveFormat.Channels);

                case RuntimeInfo.Platform.macOS:
                    return MacOSHostAudioFormatQuery.TryGetMixFormat(bassDeviceName);

                case RuntimeInfo.Platform.Linux:
                    return LinuxHostAudioFormatQuery.TryGetMixFormat(bassDeviceName);

                default:
                    return null;
            }
        }

        internal static int NormaliseBits(int bitsPerSample)
        {
            if (bitsPerSample <= 16)
                return 16;

            return 24;
        }
    }
}
