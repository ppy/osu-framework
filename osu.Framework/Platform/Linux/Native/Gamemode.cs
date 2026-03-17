// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Logging;

namespace osu.Framework.Platform.Linux.Native
{
    internal class Gamemode
    {
        [DllImport("libgamemode.so.0", EntryPoint = "real_gamemode_request_start", ExactSpelling = true)]
        private static extern int gamemode_request_start();

        [DllImport("libgamemode.so.0", EntryPoint = "real_gamemode_request_end", ExactSpelling = true)]
        private static extern int gamemode_request_end();

        /// <summary>
        /// Requests gamemode activation. Silently does nothing if libgamemode is not installed.
        /// </summary>
        public static void RequestStart()
        {
            try
            {
                if (gamemode_request_start() < 0)
                {
                    Logger.Log($"gamemode_rewust_start: failed.", LoggingTarget.Runtime, LogLevel.Debug);
                }
            }
            catch (DllNotFoundException)
            {
                // libgamemode is not installed; this is not an error.
            }
            catch (Exception e)
            {
                Logger.Log($"gamemode_request_start threw an unexpected exception: {e}", LoggingTarget.Runtime, LogLevel.Debug);
            }
        }

        /// <summary>
        /// Requests gamemode deactivation. Silently does nothing if libgamemode is not installed.
        /// </summary>
        public static void RequestEnd()
        {
            try
            {
                if (gamemode_request_end() < 0)
                {
                    Logger.Log($"gamemode_rewust_end: failed.", LoggingTarget.Runtime, LogLevel.Debug);
                }
            }
            catch (DllNotFoundException)
            {
                // libgamemode is not installed; this is not an error.
            }
            catch (Exception e)
            {
                Logger.Log($"gamemode_request_end threw an unexpected exception: {e}", LoggingTarget.Runtime, LogLevel.Debug);
            }
        }
    }
}
