// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;

namespace osu.Framework.Bindables
{
    public abstract class BindableNumber<T> : Bindable<T>, IBindableNumber<T>
        where T : struct, IComparable<T>, IConvertible, IEquatable<T>
    {
        public event Action<T> PrecisionChanged;

        public event Action<T> MinValueChanged;

        public event Action<T> MaxValueChanged;

        private protected BindableNumber(T value = default)
            : base(value)
        {
            // Directly comparing typeof(T) to type literal is recognized pattern of JIT and very fast.
            // Just a pointer comparison for reference types, or constant for value types.
            // The check will become NOP after optimization.
            if (typeof(T) != typeof(sbyte) &&
                typeof(T) != typeof(byte) &&
                typeof(T) != typeof(short) &&
                typeof(T) != typeof(ushort) &&
                typeof(T) != typeof(int) &&
                typeof(T) != typeof(uint) &&
                typeof(T) != typeof(long) &&
                typeof(T) != typeof(ulong) &&
                typeof(T) != typeof(float) &&
                typeof(T) != typeof(double))
                throw new NotSupportedException(
                    $"{nameof(BindableNumber<T>)} only accepts the primitive numeric types (except for {typeof(decimal).FullName}) as type arguments. You provided {typeof(T).FullName}.");

            MinValue = DefaultMinValue;
            MaxValue = DefaultMaxValue;
            precision = DefaultPrecision;
        }

        private T precision;

        public T Precision
        {
            get => precision;
            set
            {
                if (precision.Equals(value))
                    return;

                if (value.CompareTo(default) <= 0)
                    throw new ArgumentOutOfRangeException(nameof(Precision), value, "Must be greater than 0.");

                precision = value;

                TriggerPrecisionChange();
            }
        }

        public override T Value
        {
            get => base.Value;
            set
            {
                if (Precision.CompareTo(DefaultPrecision) > 0)
                {
                    double doubleValue = clamp(value, MinValue, MaxValue).ToDouble(NumberFormatInfo.InvariantInfo);
                    doubleValue = Math.Round(doubleValue / Precision.ToDouble(NumberFormatInfo.InvariantInfo)) * Precision.ToDouble(NumberFormatInfo.InvariantInfo);

                    base.Value = (T)Convert.ChangeType(doubleValue, typeof(T), CultureInfo.InvariantCulture);
                }
                else
                    base.Value = clamp(value, MinValue, MaxValue);
            }
        }

        private T minValue;

        public T MinValue
        {
            get => minValue;
            set
            {
                if (minValue.Equals(value))
                    return;

                minValue = value;

                TriggerMinValueChange();
            }
        }

        private T maxValue;

        public T MaxValue
        {
            get => maxValue;
            set
            {
                if (maxValue.Equals(value))
                    return;

                maxValue = value;

                TriggerMaxValueChange();
            }
        }

        /// <summary>
        /// The default <see cref="MinValue"/>. This should be equal to the minimum value of type <see cref="T"/>.
        /// </summary>
        protected abstract T DefaultMinValue { get; }

        /// <summary>
        /// The default <see cref="MaxValue"/>. This should be equal to the maximum value of type <see cref="T"/>.
        /// </summary>
        protected abstract T DefaultMaxValue { get; }

        /// <summary>
        /// The default <see cref="Precision"/>.
        /// </summary>
        protected abstract T DefaultPrecision { get; }

        public override void TriggerChange()
        {
            base.TriggerChange();

            TriggerPrecisionChange(false);
            TriggerMinValueChange(false);
            TriggerMaxValueChange(false);
        }

        protected void TriggerPrecisionChange(bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = precision;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b is BindableNumber<T> bn)
                        bn.Precision = precision;
                }
            }

            if (beforePropagation.Equals(precision))
                PrecisionChanged?.Invoke(precision);
        }

        protected void TriggerMinValueChange(bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = minValue;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b is BindableNumber<T> bn)
                        bn.MinValue = minValue;
                }
            }

            if (beforePropagation.Equals(minValue))
                MinValueChanged?.Invoke(minValue);
        }

        protected void TriggerMaxValueChange(bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = maxValue;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b is BindableNumber<T> bn)
                        bn.MaxValue = maxValue;
                }
            }

            if (beforePropagation.Equals(maxValue))
                MaxValueChanged?.Invoke(maxValue);
        }

        public override void BindTo(Bindable<T> them)
        {
            if (them is BindableNumber<T> other)
            {
                Precision = max(Precision, other.Precision);
                MinValue = max(MinValue, other.MinValue);
                MaxValue = min(MaxValue, other.MaxValue);

                if (MinValue.CompareTo(MaxValue) > 0)
                    throw new ArgumentOutOfRangeException(
                        $"Can not weld bindable longs with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{other.MinValue} - {other.MaxValue}].", nameof(them));
            }

            base.BindTo(them);
        }

        /// <summary>
        /// Whether this bindable has a user-defined range that is not the full range of the <see cref="T"/> type.
        /// </summary>
        public bool HasDefinedRange => !MinValue.Equals(DefaultMinValue) || !MaxValue.Equals(DefaultMaxValue);

        public bool IsInteger =>
            typeof(T) != typeof(float) &&
            typeof(T) != typeof(double); // Will be **constant** after JIT.

        public void Set<U>(U val) where U : struct,
            IComparable, IFormattable, IConvertible, IComparable<U>, IEquatable<U>
        {
            // Comparison between typeof(T) and type literals are treated as **constant** on value types.
            // Code pathes for other types will be eliminated.
            if (typeof(T) == typeof(byte))
                ((BindableNumber<byte>)(object)this).Value = val.ToByte(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(sbyte))
                ((BindableNumber<sbyte>)(object)this).Value = val.ToSByte(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(ushort))
                ((BindableNumber<ushort>)(object)this).Value = val.ToUInt16(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(short))
                ((BindableNumber<short>)(object)this).Value = val.ToInt16(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(uint))
                ((BindableNumber<uint>)(object)this).Value = val.ToUInt32(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(int))
                ((BindableNumber<int>)(object)this).Value = val.ToInt32(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(ulong))
                ((BindableNumber<ulong>)(object)this).Value = val.ToUInt64(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(long))
                ((BindableNumber<long>)(object)this).Value = val.ToInt64(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(float))
                ((BindableNumber<float>)(object)this).Value = val.ToSingle(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(double))
                ((BindableNumber<double>)(object)this).Value = val.ToDouble(NumberFormatInfo.InvariantInfo);
            else
                throw new NotSupportedException("How do you get here?");
        }

        public void Add<U>(U val) where U : struct,
            IComparable, IFormattable, IConvertible, IComparable<U>, IEquatable<U>
        {
            // Comparison between typeof(T) and type literals are treated as **constant** on value types.
            // Code pathes for other types will be eliminated.
            if (typeof(T) == typeof(byte))
                ((BindableNumber<byte>)(object)this).Value += val.ToByte(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(sbyte))
                ((BindableNumber<sbyte>)(object)this).Value += val.ToSByte(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(ushort))
                ((BindableNumber<ushort>)(object)this).Value += val.ToUInt16(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(short))
                ((BindableNumber<short>)(object)this).Value += val.ToInt16(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(uint))
                ((BindableNumber<uint>)(object)this).Value += val.ToUInt32(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(int))
                ((BindableNumber<int>)(object)this).Value += val.ToInt32(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(ulong))
                ((BindableNumber<ulong>)(object)this).Value += val.ToUInt64(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(long))
                ((BindableNumber<long>)(object)this).Value += val.ToInt64(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(float))
                ((BindableNumber<float>)(object)this).Value += val.ToSingle(NumberFormatInfo.InvariantInfo);
            else if (typeof(T) == typeof(double))
                ((BindableNumber<double>)(object)this).Value += val.ToDouble(NumberFormatInfo.InvariantInfo);
            else
                throw new NotSupportedException("How do you get here?");
        }

        /// <summary>
        /// Sets the value of the bindable to Min + (Max - Min) * amt
        /// <param name="amt">The proportional amount to set, ranging from 0 to 1.</param>
        /// <param name="snap">If greater than 0, snap the final value to the closest multiple of this number.</param>
        /// </summary>
        public void SetProportional(float amt, float snap = 0)
        {
            var min = MinValue.ToDouble(NumberFormatInfo.InvariantInfo);
            var max = MaxValue.ToDouble(NumberFormatInfo.InvariantInfo);
            var value = min + (max - min) * amt;
            if (snap > 0)
                value = Math.Round(value / snap) * snap;
            Set(value);
        }

        IBindableNumber<T> IBindableNumber<T>.GetBoundCopy() => GetBoundCopy();

        public new BindableNumber<T> GetBoundCopy() => (BindableNumber<T>)base.GetBoundCopy();

        public new BindableNumber<T> GetUnboundCopy() => (BindableNumber<T>)base.GetUnboundCopy();

        private static T max(T value1, T value2)
        {
            var comparison = value1.CompareTo(value2);
            return comparison > 0 ? value1 : value2;
        }

        private static T min(T value1, T value2)
        {
            var comparison = value1.CompareTo(value2);
            return comparison > 0 ? value2 : value1;
        }

        private static T clamp(T value, T minValue, T maxValue)
            => max(minValue, min(maxValue, value));
    }
}
