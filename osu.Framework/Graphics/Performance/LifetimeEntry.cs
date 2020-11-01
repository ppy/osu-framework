// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Performance
{
    /// <summary>
    /// An object for a <see cref="LifetimeEntryManager"/> to consume, which provides a <see cref="LifetimeStart"/> and <see cref="LifetimeEnd"/>.
    /// </summary>
    /// <remarks>
    /// Management of the object which the <see cref="LifetimeEntry"/> refers to is left up to the consumer.
    /// </remarks>
    public class LifetimeEntry
    {
        private double lifetimeStart = double.MinValue;

        /// <summary>
        /// The time at which this <see cref="LifetimeEntry"/> becomes alive in a <see cref="LifetimeEntryManager"/>.
        /// </summary>
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
                    SetLifetime(value, lifetimeEnd);
            }
        }

        private double lifetimeEnd = double.MaxValue;

        /// <summary>
        /// The time at which this <see cref="LifetimeEntry"/> becomes dead in a <see cref="LifetimeEntryManager"/>.
        /// </summary>
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
                    SetLifetime(lifetimeStart, value);
            }
        }

        /// <summary>
        /// Invoked when this <see cref="LifetimeEntry"/> is attached to a <see cref="LifetimeEntryManager"/> and either
        /// <see cref="LifetimeStart"/> or <see cref="LifetimeEnd"/> are changed.
        /// </summary>
        /// <remarks>
        /// If this is handled, make sure to call <see cref="SetLifetime"/> to continue with the lifetime update.
        /// </remarks>
        internal event RequestLifetimeUpdateDelegate RequestLifetimeUpdate;

        /// <summary>
        /// Updates the stored lifetimes of this <see cref="LifetimeEntry"/>.
        /// </summary>
        /// <param name="start">The new <see cref="LifetimeStart"/> value.</param>
        /// <param name="end">The new <see cref="LifetimeEnd"/> value.</param>
        internal void SetLifetime(double start, double end)
        {
            lifetimeStart = start;
            lifetimeEnd = Math.Max(start, end); // Negative intervals are undesired.
        }

        /// <summary>
        /// The current state of this <see cref="LifetimeEntry"/>.
        /// </summary>
        internal LifetimeEntryState State { get; set; }

        /// <summary>
        /// Uniquely identifies this <see cref="LifetimeEntry"/> in a <see cref="LifetimeEntryManager"/>.
        /// </summary>
        internal ulong ChildId { get; set; }
    }

    internal delegate void RequestLifetimeUpdateDelegate(LifetimeEntry entry, double lifetimeStart, double lifetimeEnd);
}
