// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Performance
{
    public class LifetimeEntry
    {
        private double lifetimeStart = double.MinValue;

        public double LifetimeStart
        {
            get => lifetimeStart;
            set
            {
                if (lifetimeStart == value)
                    return;

                if (RequestLifetimeUpdate != null)
                    RequestLifetimeUpdate.Invoke(this, value, lifetimeEnd);
                else
                    UpdateLifetime(value, lifetimeEnd);
            }
        }

        private double lifetimeEnd = double.MaxValue;

        public double LifetimeEnd
        {
            get => lifetimeEnd;
            set
            {
                if (lifetimeEnd == value)
                    return;

                if (RequestLifetimeUpdate != null)
                    RequestLifetimeUpdate.Invoke(this, lifetimeStart, value);
                else
                    UpdateLifetime(lifetimeStart, value);
            }
        }

        internal event RequestLifetimeUpdateDelegate RequestLifetimeUpdate;

        /// <summary>
        /// Updates the stored lifetimes of this <see cref="LifetimeEntry"/>.
        /// </summary>
        /// <param name="start">The new <see cref="lifetimeStart"/> value.</param>
        /// <param name="end">The new <see cref="lifetimeEnd"/> value.</param>
        internal void UpdateLifetime(double start, double end)
        {
            lifetimeStart = start;
            lifetimeEnd = Math.Max(start, end); // Negative intervals are undesired.
        }

        /// <summary>
        /// The current state of this <see cref="LifetimeEntry"/>.
        /// </summary>
        internal LifetimeEntryState State { get; set; }

        internal ulong ChildId { get; set; }
    }

    internal delegate void RequestLifetimeUpdateDelegate(LifetimeEntry entry, double lifetimeStart, double lifetimeEnd);
}
