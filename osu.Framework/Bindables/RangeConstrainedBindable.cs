// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;

namespace osu.Framework.Bindables
{
    public abstract class RangeConstrainedBindable<T> : Bindable<T>
    {
        public event Action<T> MinValueChanged;

        public event Action<T> MaxValueChanged;

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

        public override T Value
        {
            get => base.Value;
            set => setValue(value);
        }

        /// <summary>
        /// The default <see cref="MinValue"/>. This should be equal to the minimum value of type <typeparamref name="T"/>.
        /// </summary>
        protected abstract T DefaultMinValue { get; }

        /// <summary>
        /// The default <see cref="MaxValue"/>. This should be equal to the maximum value of type <typeparamref name="T"/>.
        /// </summary>
        protected abstract T DefaultMaxValue { get; }

        /// <summary>
        /// Whether this bindable has a user-defined range that is not the full range of the <typeparamref name="T"/> type.
        /// </summary>
        public bool HasDefinedRange => !EqualityComparer<T>.Default.Equals(MinValue, DefaultMinValue) ||
                                       !EqualityComparer<T>.Default.Equals(MaxValue, DefaultMaxValue);

        protected RangeConstrainedBindable(T defaultValue = default)
            : base(defaultValue)
        {
            minValue = DefaultMinValue;
            maxValue = DefaultMaxValue;

            // Reapply the default value here for respecting the defined default min/max values.
            setValue(defaultValue);
        }

        /// <summary>
        /// Sets the minimum value. This method does no equality comparisons.
        /// </summary>
        /// <param name="minValue">The new minimum value.</param>
        /// <param name="updateCurrentValue">Whether to update the current value after the minimum value is set.</param>
        /// <param name="source">The bindable that triggered this. A null value represents the current bindable instance.</param>
        internal void SetMinValue(T minValue, bool updateCurrentValue, RangeConstrainedBindable<T> source)
        {
            this.minValue = minValue;
            TriggerMinValueChange(source);

            if (updateCurrentValue)
            {
                // Reapply the current value to respect the new minimum value.
                setValue(Value);
            }
        }

        /// <summary>
        /// Sets the maximum value. This method does no equality comparisons.
        /// </summary>
        /// <param name="maxValue">The new maximum value.</param>
        /// <param name="updateCurrentValue">Whether to update the current value after the maximum value is set.</param>
        /// <param name="source">The bindable that triggered this. A null value represents the current bindable instance.</param>
        internal void SetMaxValue(T maxValue, bool updateCurrentValue, RangeConstrainedBindable<T> source)
        {
            this.maxValue = maxValue;
            TriggerMaxValueChange(source);

            if (updateCurrentValue)
            {
                // Reapply the current value to respect the new maximum value.
                setValue(Value);
            }
        }

        public override void TriggerChange()
        {
            base.TriggerChange();

            TriggerMinValueChange(this, false);
            TriggerMaxValueChange(this, false);
        }

        protected void TriggerMinValueChange(RangeConstrainedBindable<T> source = null, bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = minValue;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    if (b is RangeConstrainedBindable<T> cb)
                        cb.SetMinValue(minValue, false, this);
                }
            }

            if (EqualityComparer<T>.Default.Equals(beforePropagation, minValue))
                MinValueChanged?.Invoke(minValue);
        }

        protected void TriggerMaxValueChange(RangeConstrainedBindable<T> source = null, bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = maxValue;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    if (b is RangeConstrainedBindable<T> cb)
                        cb.SetMaxValue(maxValue, false, this);
                }
            }

            if (EqualityComparer<T>.Default.Equals(beforePropagation, maxValue))
                MaxValueChanged?.Invoke(maxValue);
        }

        public override void BindTo(Bindable<T> them)
        {
            if (them is RangeConstrainedBindable<T> other)
            {
                if (!IsValidRange(other.MinValue, other.MaxValue))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(them), $"The target bindable has specified an invalid range of [{other.MinValue} - {other.MaxValue}].");
                }

                MinValue = other.MinValue;
                MaxValue = other.MaxValue;
            }

            base.BindTo(them);
        }

        public override void UnbindEvents()
        {
            base.UnbindEvents();

            MinValueChanged = null;
            MaxValueChanged = null;
        }

        public new RangeConstrainedBindable<T> GetBoundCopy() => (RangeConstrainedBindable<T>)base.GetBoundCopy();

        public new RangeConstrainedBindable<T> GetUnboundCopy() => (RangeConstrainedBindable<T>)base.GetUnboundCopy();

        /// <summary>
        /// Clamps <paramref name="value"/> to the range defined by <paramref name="minValue"/> and <paramref name="maxValue"/>.
        /// </summary>
        protected abstract T ClampValue(T value, T minValue, T maxValue);

        /// <summary>
        /// Whether <paramref name="min"/> and <paramref name="max"/> constitute a valid range
        /// (usually used to check that <paramref name="min"/> is indeed lesser than or equal to <paramref name="max"/>).
        /// </summary>
        /// <param name="min">The range's minimum value.</param>
        /// <param name="max">The range's maximum value.</param>
        protected abstract bool IsValidRange(T min, T max);

        private void setValue(T value) => base.Value = ClampValue(value, minValue, maxValue);
    }
}
