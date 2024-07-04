// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
            // A method is used as C# doesn't allow the combination of a non-virtual getter and a virtual setter.
            set => SetLifetimeStart(value);
        }

        private double lifetimeEnd = double.MaxValue;

        /// <summary>
        /// The time at which this <see cref="LifetimeEntry"/> becomes dead in a <see cref="LifetimeEntryManager"/>.
        /// </summary>
        public double LifetimeEnd
        {
            get => lifetimeEnd;
            set => SetLifetimeEnd(value);
        }

        /// <summary>
        /// Invoked before <see cref="LifetimeStart"/> or <see cref="LifetimeEnd"/> changes.
        /// It is used because <see cref="LifetimeChanged"/> cannot be used to ensure comparator stability.
        /// </summary>
        internal event Action<LifetimeEntry> RequestLifetimeUpdate;

        /// <summary>
        /// Invoked after <see cref="LifetimeStart"/> or <see cref="LifetimeEnd"/> changes.
        /// </summary>
        public event Action<LifetimeEntry> LifetimeChanged;

        /// <summary>
        /// Update <see cref="LifetimeStart"/> of this <see cref="LifetimeEntry"/>.
        /// </summary>
        protected virtual void SetLifetimeStart(double start)
        {
            if (start != lifetimeStart)
                SetLifetime(start, lifetimeEnd);
        }

        /// <summary>
        /// Update <see cref="LifetimeEnd"/> of this <see cref="LifetimeEntry"/>.
        /// </summary>
        protected virtual void SetLifetimeEnd(double end)
        {
            if (end != lifetimeEnd)
                SetLifetime(lifetimeStart, end);
        }

        /// <summary>
        /// Updates the stored lifetimes of this <see cref="LifetimeEntry"/>.
        /// </summary>
        /// <param name="start">The new <see cref="LifetimeStart"/> value.</param>
        /// <param name="end">The new <see cref="LifetimeEnd"/> value.</param>
        protected void SetLifetime(double start, double end)
        {
            RequestLifetimeUpdate?.Invoke(this);

            lifetimeStart = start;
            lifetimeEnd = Math.Max(start, end); // Negative intervals are undesired.

            LifetimeChanged?.Invoke(this);
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
}
