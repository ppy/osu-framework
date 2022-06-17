// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Statistics;
using System;

namespace osu.Framework.Caching
{
    public class Cached<T>
    {
        private T value;

        public T Value
        {
            get
            {
                if (!IsValid)
                    throw new InvalidOperationException($"May not query {nameof(Value)} of an invalid {nameof(Cached<T>)}.");

                return value;
            }

            set
            {
                this.value = value;
                IsValid = true;
                FrameStatistics.Increment(StatisticsCounterType.Refreshes);
            }
        }

        public bool IsValid { get; private set; }

        public static implicit operator T(Cached<T> value) => value.Value;

        /// <summary>
        /// Invalidate the cache of this object.
        /// </summary>
        /// <returns>True if we invalidated from a valid state.</returns>
        public bool Invalidate()
        {
            if (IsValid)
            {
                IsValid = false;
                FrameStatistics.Increment(StatisticsCounterType.Invalidations);
                return true;
            }

            return false;
        }
    }

    public class Cached
    {
        public bool IsValid { get; private set; }

        /// <summary>
        /// Invalidate the cache of this object.
        /// </summary>
        /// <returns>True if we invalidated from a valid state.</returns>
        public bool Invalidate()
        {
            if (IsValid)
            {
                IsValid = false;
                FrameStatistics.Increment(StatisticsCounterType.Invalidations);
                return true;
            }

            return false;
        }

        public void Validate()
        {
            IsValid = true;
            FrameStatistics.Increment(StatisticsCounterType.Refreshes);
        }
    }
}
