// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Performance
{
    /// <summary>
    /// An object for a <see cref="LifetimeEntryManager"/> to consume, which provides a <see cref="LifetimeEntryBase{T}.LifetimeStart"/> and <see cref="LifetimeEntryBase{T}.LifetimeEnd"/>.
    /// </summary>
    /// <remarks>
    /// Management of the object which the <see cref="LifetimeEntry"/> refers to is left up to the consumer.
    /// </remarks>
    public class LifetimeEntry : LifetimeEntryBase<LifetimeEntry>
    {
    }

    /// <summary>
    /// The required base type for the <see cref="LifetimeEntryManager{T}"/> to consume, which provides a <see cref="LifetimeEntryBase{T}.LifetimeStart"/> and <see cref="LifetimeEntryBase{T}.LifetimeEnd"/>.
    /// </summary>
    /// <typeparam name="TDerived">The implemented class itself. Used to provide derived category information for base categories using the Curiously Recurring Template Pattern(CRTP).</typeparam>
    public abstract class LifetimeEntryBase<TDerived> where TDerived : LifetimeEntryBase<TDerived>
    {
        private double lifetimeStart = double.MinValue;

        /// <summary>
        /// The time at which this <see cref="LifetimeEntryBase{T}"/> becomes alive in a <see cref="LifetimeEntryManager{T}"/>.
        /// </summary>
        public double LifetimeStart
        {
            get => lifetimeStart;
            // A method is used as C# doesn't allow the combination of a non-virtual getter and a virtual setter.
            set => SetLifetimeStart(value);
        }

        private double lifetimeEnd = double.MaxValue;

        /// <summary>
        /// The time at which this <see cref="LifetimeEntryBase{T}"/> becomes dead in a <see cref="LifetimeEntryManager{T}"/>.
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
        internal event Action<TDerived>? RequestLifetimeUpdate;

        /// <summary>
        /// Invoked after <see cref="LifetimeStart"/> or <see cref="LifetimeEnd"/> changes.
        /// </summary>
        public event Action<TDerived>? LifetimeChanged;

        /// <summary>
        /// Update <see cref="LifetimeStart"/> of this <see cref="LifetimeEntryBase{T}"/>.
        /// </summary>
        protected virtual void SetLifetimeStart(double start)
        {
            if (start != lifetimeStart)
                SetLifetime(start, lifetimeEnd);
        }

        /// <summary>
        /// Update <see cref="LifetimeEnd"/> of this <see cref="LifetimeEntryBase{T}"/>.
        /// </summary>
        protected virtual void SetLifetimeEnd(double end)
        {
            if (end != lifetimeEnd)
                SetLifetime(lifetimeStart, end);
        }

        /// <summary>
        /// Updates the stored lifetimes of this <see cref="LifetimeEntryBase{T}"/>.
        /// </summary>
        /// <param name="start">The new <see cref="LifetimeStart"/> value.</param>
        /// <param name="end">The new <see cref="LifetimeEnd"/> value.</param>
        protected void SetLifetime(double start, double end)
        {
            // Due to the type constraints of C#, we cannot declare `LifetimeEntry<T> where T = LifetimeEntry<T>` to limit the type of `this` to be `T`. But when used correctly, this will always be the case.
            // aka. We can't stop anyone from writing code like this:
            // ```csharp
            // public class MyEntry : LifetimeEntry<MyEntry> { }
            // LifetimeEntry<MyEntry> foo = new();
            // ```
            // We prevent users from inadvertently writing such code by declaring `LifetimeEntry<T>` as `abstract`, but we cannot prevent it completely.
            // ```csharp
            // public class NewEntry<T> : LifetimeEntry<T> where T : LifetimeEntry<T> { }
            // NewEntry<MyEntry> a = new();
            // ```
            // Happily, however, the compiler correctly prevents code like this from compiling. This is also enough to deter users from writing incorrect code:
            // ```csharp
            // public class ErrorEntry : NewEntry<MyEntry> { }
            // LifetimeEntryManager<NewEntry<MyEntry>> error1 = new(); // Compiler error.
            // LifetimeEntryManager<ErrorEntry> error2 = new(); // Compiler error.
            // ```
            RequestLifetimeUpdate?.Invoke((TDerived)this);

            lifetimeStart = start;
            lifetimeEnd = Math.Max(start, end); // Negative intervals are undesired.

            LifetimeChanged?.Invoke((TDerived)this);
        }

        /// <summary>
        /// The current state of this <see cref="LifetimeEntryBase{T}"/>.
        /// </summary>
        internal LifetimeEntryState State { get; set; }

        /// <summary>
        /// Uniquely identifies this <see cref="LifetimeEntryBase{T}"/> in a <see cref="LifetimeEntryManager{T}"/>.
        /// </summary>
        internal ulong ChildId { get; set; }
    }
}
