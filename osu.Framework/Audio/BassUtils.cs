// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using ManagedBass;
using osu.Framework.Logging;

namespace osu.Framework.Audio
{
    internal static class BassUtils
    {
        private static string getFailString() => $@"BASS failed to initialize with error code {Bass.LastError:D}: {Bass.LastError}.";

        /// <summary>
        /// Checks whether <see cref="Bass"/> faulted by checking its <see cref="Bass.LastError"/>.
        /// </summary>
        /// <param name="throwException">Whether to throw an exception if it faulted.</param>
        /// <returns>Whether it faulted.</returns>
        internal static bool CheckFaulted(bool throwException)
        {
            if (Bass.LastError == Errors.OK)
                return false;

            if (throwException)
                throw new InvalidOperationException(getFailString());

            Logger.Log(getFailString(), LoggingTarget.Runtime, LogLevel.Important);
            return true;
        }
    }
}
