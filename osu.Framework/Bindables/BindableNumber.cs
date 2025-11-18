// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Numerics;
using JetBrains.Annotations;
using osu.Framework.Utils;

namespace osu.Framework.Bindables
{
    public class BindableNumber<T> : RangeConstrainedBindable<T>, IBindableNumber<T>
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        [CanBeNull]
        public event Action<T> PrecisionChanged;

        public BindableNumber(T defaultValue = default)
            : base(defaultValue)
        {
            if (!Validation.IsSupportedBindableNumberType<T>())
            {
                throw new NotSupportedException(
                    $"{nameof(BindableNumber<T>)} only accepts the primitive numeric types (except for {typeof(decimal).FullName}) as type arguments. You provided {typeof(T).FullName}.");
            }

            precision = DefaultPrecision;

            // Re-apply the current value to apply the default precision value
            setValue(Value);
        }

        private T precision;

        public T Precision
        {
            get => precision;
            set
            {
                if (precision == value)
                    return;

                if (value <= T.Zero)
                    throw new ArgumentOutOfRangeException(nameof(Precision), value, "Must be greater than 0.");

                SetPrecision(value, true, this);
            }
        }

        /// <summary>
        /// Sets the precision. This method does no equality comparisons.
        /// </summary>
        /// <param name="precision">The new precision.</param>
        /// <param name="updateCurrentValue">Whether to update the current value after the precision is set.</param>
        /// <param name="source">The bindable that triggered this. A null value represents the current bindable instance.</param>
        internal void SetPrecision(T precision, bool updateCurrentValue, BindableNumber<T> source)
        {
            this.precision = precision;
            TriggerPrecisionChange(source);

            if (updateCurrentValue)
            {
                // Re-apply the current value to apply the new precision
                setValue(Value);
            }
        }

        public override T Value
        {
            get => base.Value;
            set => setValue(value);
        }

        private void setValue(T value)
        {
            decimal decPrecision = decimal.CreateTruncating(Precision);

            if (decPrecision > 0)
            {
                // this rounding is purposefully performed on `decimal` to ensure that the resulting value is the closest possible floating-point
                // number to actual real-world base-10 decimals, as that is the most common usage of precision.
                decimal accurateResult = decimal.CreateTruncating(T.Clamp(value, MinValue, MaxValue));
                accurateResult = Math.Round(accurateResult / decPrecision) * decPrecision;

                base.Value = T.CreateTruncating(accurateResult);
            }
            else
                base.Value = value;
        }

        protected override T DefaultMinValue => T.MinValue;

        protected override T DefaultMaxValue => T.MaxValue;

        /// <summary>
        /// The default <see cref="Precision"/>.
        /// </summary>
        protected virtual T DefaultPrecision
        {
            get
            {
                if (typeof(T) == typeof(float))
                    return (T)(object)float.Epsilon;
                if (typeof(T) == typeof(double))
                    return (T)(object)double.Epsilon;

                return T.One;
            }
        }

        public override void TriggerChange()
        {
            base.TriggerChange();

            TriggerPrecisionChange(this, false);
        }

        protected void TriggerPrecisionChange(BindableNumber<T> source = null, bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = precision;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    if (b is BindableNumber<T> bn)
                        bn.SetPrecision(precision, false, this);
                }
            }

            if (beforePropagation.Equals(precision))
                PrecisionChanged?.Invoke(precision);
        }

        public override void CopyTo(Bindable<T> them)
        {
            if (them is BindableNumber<T> other)
                other.Precision = Precision;

            base.CopyTo(them);
        }

        public override void UnbindEvents()
        {
            base.UnbindEvents();

            PrecisionChanged = null;
        }

        public bool IsInteger =>
            typeof(T) != typeof(float) &&
            typeof(T) != typeof(double); // Will be **constant** after JIT.

        public void Set<TNewValue>(TNewValue val) where TNewValue : struct, INumber<TNewValue>
            => Value = T.CreateTruncating(val);

        public void Add<TNewValue>(TNewValue val) where TNewValue : struct, INumber<TNewValue>
            => Value += T.CreateTruncating(val);

        /// <summary>
        /// Sets the value of the bindable to Min + (Max - Min) * amt
        /// <param name="amt">The proportional amount to set, ranging from 0 to 1.</param>
        /// <param name="snap">If greater than 0, snap the final value to the closest multiple of this number.</param>
        /// </summary>
        public void SetProportional(float amt, float snap = 0)
        {
            // TODO: Use IFloatingPointIeee754<T>.Lerp when applicable

            double min = double.CreateTruncating(MinValue);
            double max = double.CreateTruncating(MaxValue);
            double value = min + (max - min) * amt;
            if (snap > 0)
                value = Math.Round(value / snap) * snap;
            Set(value);
        }

        IBindableNumber<T> IBindableNumber<T>.GetBoundCopy() => GetBoundCopy();

        public new BindableNumber<T> GetBoundCopy() => (BindableNumber<T>)base.GetBoundCopy();

        public new BindableNumber<T> GetUnboundCopy() => (BindableNumber<T>)base.GetUnboundCopy();

        public override bool IsDefault
        {
            get
            {
                if (typeof(T) == typeof(double))
                {
                    // Take 50% of the precision to ensure the value doesn't underflow and return true for non-default values.
                    return Utils.Precision.AlmostEquals((double)(object)Value, (double)(object)Default, (double)(object)Precision / 2);
                }

                if (typeof(T) == typeof(float))
                {
                    // Take 50% of the precision to ensure the value doesn't underflow and return true for non-default values.
                    return Utils.Precision.AlmostEquals((float)(object)Value, (float)(object)Default, (float)(object)Precision / 2);
                }

                return base.IsDefault;
            }
        }

        protected override Bindable<T> CreateInstance() => new BindableNumber<T>();

        protected sealed override T ClampValue(T value, T minValue, T maxValue) => T.Clamp(value, minValue, maxValue);

        protected sealed override bool IsValidRange(T min, T max) => min <= max;
    }
}
