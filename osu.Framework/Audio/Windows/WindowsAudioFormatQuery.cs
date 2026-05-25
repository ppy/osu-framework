// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using osu.Framework.Logging;

namespace osu.Framework.Audio.Windows
{
    /// <summary>
    /// Read-only Windows Core Audio queries for device names and mix formats.
    /// Playback remains on BASS; this only informs device/format selection.
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
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows || string.IsNullOrEmpty(bassDriverId))
                return null;

            try
            {
                using var enumerator = new MMDeviceEnumerator();

                foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (!string.Equals(device.ID, bassDriverId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return device.AudioClient.MixFormat;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Windows audio mix format lookup failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return null;
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
    }
}
