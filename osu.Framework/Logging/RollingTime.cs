// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Logging
{
    /// <summary>
    /// Keep track of a finite list of timed events for the purpose of rate limiting.
    /// </summary>
    internal class RollingTime
    {
        private readonly int size;
        private readonly int span;
        private readonly long[] time;

        /// <summary>
        /// Make a new object that keeps track of time for a number of events.
        /// </summary>
        /// <param name="size">The number of events to track.</param>
        /// <param name="span">The time over which the number of events is governed.</param>
        internal RollingTime(int size, int span)
        {
            this.size = size;
            this.span = span;
            time = new long[size];
            RequestEntry();
        }

        private long now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public bool IsAtLimit => time[0] + span - now > 0;

        internal bool RequestEntry()
        {
            if (IsAtLimit) return false;

            for (int i = 0; i < size - 1; i++)
                time[i] = time[i + 1];
            time[size - 1] = now;
            return true;
        }
    }
}
