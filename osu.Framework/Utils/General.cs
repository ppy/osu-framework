// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Logging;

namespace osu.Framework.Utils
{
    public static class General
    {
        /// <summary>
        /// Attempt an operation and perform retries on a matching exception, up to a limit.
        /// Useful for IO operations which can fail for a short period due to an open file handle.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="attempts">The number of attempts (250ms wait between each).</param>
        /// <param name="throwOnFailure">Whether to throw an exception on failure. If <c>false</c>, will silently fail.</param>
        /// <typeparam name="TException">The type of exception which should trigger retries.</typeparam>
        /// <returns>Whether the operation succeeded.</returns>
        public static bool AttemptWithRetryOnException<TException>(this Action action, int attempts = 10, bool throwOnFailure = true)
            where TException : Exception
        {
            while (true)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception e)
                {
                    if (e is not TException)
                        throw;

                    if (attempts-- == 0)
                    {
                        if (throwOnFailure)
                            throw;

                        return false;
                    }

                    Logger.Log($"Operation failed ({e.Message}). Retrying {attempts} more times...");
                }

                Thread.Sleep(250);
            }
        }
    }
}
