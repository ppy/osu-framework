// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Bindables
{
    public abstract class ConstraintedBindable<T> : Bindable<T>
    {
        public event Action<T> MinValueChanged;

        public event Action<T> MaxValueChanged;

        public override T Value
        {
            get => base.Value;
            set => base.Value = ClampValue(value, minValue, maxValue);
        }

        private T minValue;

        public T MinValue
        {
            get => minValue;
            set
            {
                if (EqualityComparer<T>.Default.Equals(value, minValue))
                    return;

                SetMinValue(value, true, this);
            }
        }

        private T maxValue;

        public T MaxValue
        {
            get => maxValue;
            set
            {
                if (EqualityComparer<T>.Default.Equals(value, maxValue))
                    return;

                SetMaxValue(value, true, this);
            }
        }

        /// <summary>
        /// Whether this bindable has a user-defined range that is not the full range of the <typeparamref name="T"/> type.
        /// </summary>
        public bool HasDefinedRange => !EqualityComparer<T>.Default.Equals(MinValue, DefaultMinValue) ||
                                       !EqualityComparer<T>.Default.Equals(MaxValue, DefaultMaxValue);

        protected ConstraintedBindable(T defaultValue = default)
            : base(defaultValue)
        {
            minValue = DefaultMinValue;
            maxValue = DefaultMaxValue;

            // Re-apply the current value to apply the default min/max values.
            setValue(Value);
        }

        /// <summary>
        /// Sets the minimum value. This method does no equality comparisons.
        /// </summary>
        /// <param name="minValue">The new minimum value.</param>
        /// <param name="updateCurrentValue">Whether to update the current value after the minimum value is set.</param>
        /// <param name="source">The bindable that triggered this. A null value represents the current bindable instance.</param>
        internal void SetMinValue(T minValue, bool updateCurrentValue, ConstraintedBindable<T> source)
        {
            this.minValue = minValue;
            TriggerMinValueChange(source);

            if (updateCurrentValue)
            {
                // Re-apply the current value to apply the new minimum value
                setValue(Value);
            }
        }

        /// <summary>
        /// Sets the maximum value. This method does no equality comparisons.
        /// </summary>
        /// <param name="maxValue">The new maximum value.</param>
        /// <param name="updateCurrentValue">Whether to update the current value after the maximum value is set.</param>
        /// <param name="source">The bindable that triggered this. A null value represents the current bindable instance.</param>
        internal void SetMaxValue(T maxValue, bool updateCurrentValue, ConstraintedBindable<T> source)
        {
            this.maxValue = maxValue;
            TriggerMaxValueChange(source);

            if (updateCurrentValue)
            {
                // Re-apply the current value to apply the new maximum value
                setValue(Value);
            }
        }

        public override void TriggerChange()
        {
            base.TriggerChange();

            TriggerMinValueChange(this, false);
            TriggerMaxValueChange(this, false);
        }

        protected void TriggerMinValueChange(ConstraintedBindable<T> source = null, bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = minValue;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    if (b is ConstraintedBindable<T> bn)
                        bn.SetMinValue(minValue, false, this);
                }
            }

            if (EqualityComparer<T>.Default.Equals(beforePropagation, minValue))
                MinValueChanged?.Invoke(minValue);
        }

        protected void TriggerMaxValueChange(ConstraintedBindable<T> source = null, bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = maxValue;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    if (b is ConstraintedBindable<T> bn)
                        bn.SetMaxValue(maxValue, false, this);
                }
            }

            if (EqualityComparer<T>.Default.Equals(beforePropagation, maxValue))
                MaxValueChanged?.Invoke(maxValue);
        }

        public override void BindTo(Bindable<T> them)
        {
            if (them is ConstraintedBindable<T> other)
            {
                MinValue = other.MinValue;
                MaxValue = other.MaxValue;

                if (Compare(MinValue, MaxValue) > 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(them), $"Can not weld bindable longs with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{other.MinValue} - {other.MaxValue}].");
                }
            }

            base.BindTo(them);
        }

        public new ConstraintedBindable<T> GetBoundCopy() => (ConstraintedBindable<T>)base.GetBoundCopy();

        public new ConstraintedBindable<T> GetUnboundCopy() => (ConstraintedBindable<T>)base.GetUnboundCopy();

        /// <summary>
        /// The default <see cref="MinValue"/>. This should be equal to the minimum value of type <typeparamref name="T"/>.
        /// </summary>
        protected abstract T DefaultMinValue { get; }

        /// <summary>
        /// The default <see cref="MaxValue"/>. This should be equal to the maximum value of type <typeparamref name="T"/>.
        /// </summary>
        protected abstract T DefaultMaxValue { get; }

        /// <summary>
        /// Clamps the given <paramref name="value"/>.
        /// </summary>
        protected abstract T ClampValue(T value, T minValue, T maxValue);

        /// <summary>
        /// Compares two values.
        /// </summary>
        /// <param name="x">The first value to compare.</param>
        /// <param name="y">The second value to compare.</param>
        /// <returns>-1 if <paramref name="x"/> is less than <paramref name="y"/>, 0 if they're both equal, 1 if <paramref name="x"/> is greater than <paramref name="y"/>.</returns>
        protected abstract int Compare(T x, T y);

        private void setValue(T value) => base.Value = ClampValue(value, minValue, maxValue);
    }
}
