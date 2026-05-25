// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Audio.Host
{
    /// <summary>
    /// Maps a logical device name (e.g. ASIO) to a host (BASS) playback device name.
    /// </summary>
    internal static class HostDeviceMatcher
    {
        public static string? FindBestBassDeviceName(string logicalDeviceName, IEnumerable<string> bassPlaybackDeviceNames)
        {
            string normalisedLogical = normaliseDeviceName(logicalDeviceName);
            string? bestMatch = null;
            int bestScore = 0;

            foreach (string bassName in bassPlaybackDeviceNames)
            {
                int score = scoreMatch(normalisedLogical, normaliseDeviceName(bassName));

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = bassName;
                }
            }

            return bestMatch;
        }

        private static int scoreMatch(string logicalName, string bassName)
        {
            if (string.IsNullOrEmpty(logicalName) || string.IsNullOrEmpty(bassName))
                return 0;

            if (logicalName.Equals(bassName, StringComparison.OrdinalIgnoreCase))
                return 100;

            if (logicalName.Contains(bassName, StringComparison.OrdinalIgnoreCase) || bassName.Contains(logicalName, StringComparison.OrdinalIgnoreCase))
                return 80;

            string logicalToken = firstToken(logicalName);
            string bassToken = firstToken(bassName);

            if (logicalToken.Equals(bassToken, StringComparison.OrdinalIgnoreCase))
                return 60;

            return 0;
        }

        private static string normaliseDeviceName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            return name.Replace("(ASIO)", string.Empty, StringComparison.OrdinalIgnoreCase)
                       .Replace("(WASAPI Exclusive)", string.Empty, StringComparison.OrdinalIgnoreCase)
                       .Trim();
        }

        private static string firstToken(string value)
            => value.Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? value;
    }
}
