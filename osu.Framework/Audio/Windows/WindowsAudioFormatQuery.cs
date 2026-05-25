// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using osu.Framework.Audio.Asio;
using osu.Framework.Audio.Host;
using osu.Framework.Logging;

namespace osu.Framework.Audio.Windows
{
    /// <summary>
    /// Read-only Windows Core Audio queries for device-reported formats (mix format, device format property).
    /// Does not probe candidate sample rates; mirrors what the OS exposes for the endpoint.
    /// </summary>
    public static class WindowsAudioFormatQuery
    {
        public static string? TryGetFriendlyPlaybackName(string bassDriverId)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows || string.IsNullOrEmpty(bassDriverId))
                return null;

            try
            {
                using var enumerator = new MMDeviceEnumerator();

                foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (string.Equals(device.ID, bassDriverId, StringComparison.OrdinalIgnoreCase))
                        return device.FriendlyName;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Windows audio friendly name lookup failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return null;
        }

        public static WaveFormat? TryGetMixFormatForDriver(string bassDriverId)
        {
            var device = tryGetPlaybackDevice(bassDriverId);
            return device?.AudioClient.MixFormat;
        }

        /// <summary>
        /// Returns formats reported by Windows for the playback endpoint (mix format + user device format property).
        /// </summary>
        public static IReadOnlyList<EzAsioFormatOption> GetReportedPlaybackFormats(string? bassDriverId)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return Array.Empty<EzAsioFormatOption>();

            var device = tryGetPlaybackDevice(bassDriverId);

            if (device == null)
                return Array.Empty<EzAsioFormatOption>();

            var results = new List<EzAsioFormatOption>();

            try
            {
                addWaveFormat(results, device.AudioClient.MixFormat);
            }
            catch (Exception ex)
            {
                Logger.Log($"Windows mix format lookup failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
            }

            try
            {
                device.GetPropertyInformation();

                if (device.Properties.TryGetValue(PropertyKeys.PKEY_AudioEngine_DeviceFormat, out byte[] blob))
                    addWaveFormat(results, waveFormatFromBlob(blob));
            }
            catch (Exception ex)
            {
                Logger.Log($"Windows device format property lookup failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return results;
        }

        /// <summary>
        /// Resolves an ASIO device to its host playback endpoint and returns Windows-reported formats.
        /// </summary>
        public static IReadOnlyList<EzAsioFormatOption> GetReportedFormatsForAsioDevice(string asioDeviceName, IEnumerable<string> bassPlaybackDeviceNames)
        {
            string? bassName = HostDeviceMatcher.FindBestBassDeviceName(asioDeviceName, bassPlaybackDeviceNames);

            if (string.IsNullOrEmpty(bassName))
            {
                Logger.Log($"No host playback device match for ASIO device '{asioDeviceName}'; using default Windows playback format.", LoggingTarget.Runtime, LogLevel.Debug);
                return GetReportedPlaybackFormats(null);
            }

            string? driverId = tryGetDriverIdForBassDeviceName(bassName);

            if (string.IsNullOrEmpty(driverId))
                return GetReportedPlaybackFormats(null);

            return GetReportedPlaybackFormats(driverId);
        }

        public static WaveFormat? TryGetDefaultPlaybackMixFormat()
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return null;

            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                return device.AudioClient.MixFormat;
            }
            catch (Exception ex)
            {
                Logger.Log($"Windows default playback mix format lookup failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
                return null;
            }
        }

        private static MMDevice? tryGetPlaybackDevice(string? bassDriverId)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return null;

            try
            {
                using var enumerator = new MMDeviceEnumerator();

                if (!string.IsNullOrEmpty(bassDriverId))
                {
                    foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                    {
                        if (string.Equals(device.ID, bassDriverId, StringComparison.OrdinalIgnoreCase))
                            return device;
                    }
                }

                return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch (Exception ex)
            {
                Logger.Log($"Windows playback device lookup failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
                return null;
            }
        }

        private static string? tryGetDriverIdForBassDeviceName(string bassDeviceName)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return null;

            try
            {
                using var enumerator = new MMDeviceEnumerator();

                foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (device.FriendlyName.Contains(bassDeviceName, StringComparison.OrdinalIgnoreCase)
                        || bassDeviceName.Contains(device.FriendlyName, StringComparison.OrdinalIgnoreCase))
                        return device.ID;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Windows driver id lookup failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return null;
        }

        private static void addWaveFormat(List<EzAsioFormatOption> results, WaveFormat? format)
        {
            if (format == null)
                return;

            int bits = normaliseBitsPerSample(format.BitsPerSample);

            if (bits == 0)
                return;

            var option = new EzAsioFormatOption(format.SampleRate, bits);

            if (!results.Contains(option))
                results.Add(option);
        }

        private static int normaliseBitsPerSample(int bitsPerSample)
        {
            // UI exposes 16/24-bit only; map common Windows containers accordingly.
            if (bitsPerSample <= 16)
                return 16;

            return 24;
        }

        private static WaveFormat? waveFormatFromBlob(byte[]? blob)
        {
            if (blob == null || blob.Length < 16)
                return null;

            GCHandle handle = GCHandle.Alloc(blob, GCHandleType.Pinned);

            try
            {
                return WaveFormat.MarshalFromPtr(handle.AddrOfPinnedObject());
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to parse WAVEFORMATEX from device property: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
                return null;
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
