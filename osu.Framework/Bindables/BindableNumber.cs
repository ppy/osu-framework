// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace osu.Framework.Bindables
{
    public class BindableNumber<T> : Bindable<T>, IBindableNumber<T>
        where T : struct, IComparable<T>, IConvertible, IEquatable<T>
    {
        public event Action<T> PrecisionChanged;

        public event Action<T> MinValueChanged;

        public event Action<T> MaxValueChanged;

        public BindableNumber(T defaultValue = default)
            : base(defaultValue)
        {
            // Directly comparing typeof(T) to type literal is recognized pattern of JIT and very fast.
            // Just a pointer comparison for reference types, or constant for value types.
            // The check will become NOP after optimization.
            if (!isSupportedType())
            {
                throw new NotSupportedException(
                    $"{nameof(BindableNumber<T>)} only accepts the primitive numeric types (except for {typeof(decimal).FullName}) as type arguments. You provided {typeof(T).FullName}.");
            }

            minValue = DefaultMinValue;
            maxValue = DefaultMaxValue;
            precision = DefaultPrecision;

            // Re-apply the current value to apply the default min/max/precision values
            SetValue(Value);
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
                SetValue(Value);
            }
        }

        public override T Value
        {
            get => base.Value;
            set => SetValue(value);
        }

        internal void SetValue(T value)
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

        private T minValue;

        public T MinValue
        {
            get => minValue;
            set
            {
                if (minValue.Equals(value))
                    return;

                SetMinValue(value, true, this);
            }
        }

        /// <summary>
        /// Sets the minimum value. This method does no equality comparisons.
        /// </summary>
        /// <param name="minValue">The new minimum value.</param>
        /// <param name="updateCurrentValue">Whether to update the current value after the minimum value is set.</param>
        /// <param name="source">The bindable that triggered this. A null value represents the current bindable instance.</param>
        internal void SetMinValue(T minValue, bool updateCurrentValue, BindableNumber<T> source)
        {
            this.minValue = minValue;
            TriggerMinValueChange(source);

            if (updateCurrentValue)
            {
                // Re-apply the current value to apply the new minimum value
                SetValue(Value);
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

                SetMaxValue(value, true, this);
            }
        }

        /// <summary>
        /// Sets the maximum value. This method does no equality comparisons.
        /// </summary>
        /// <param name="maxValue">The new maximum value.</param>
        /// <param name="updateCurrentValue">Whether to update the current value after the maximum value is set.</param>
        /// <param name="source">The bindable that triggered this. A null value represents the current bindable instance.</param>
        internal void SetMaxValue(T maxValue, bool updateCurrentValue, BindableNumber<T> source)
        {
            this.maxValue = maxValue;
            TriggerMaxValueChange(source);

            if (updateCurrentValue)
            {
                // Re-apply the current value to apply the new maximum value
                SetValue(Value);
            }
        }

        /// <summary>
        /// The default <see cref="MinValue"/>. This should be equal to the minimum value of type <typeparamref name="T"/>.
        /// </summary>
        protected virtual T DefaultMinValue
        {
            get
            {
                Debug.Assert(isSupportedType());

                if (typeof(T) == typeof(sbyte))
                    return (T)(object)sbyte.MinValue;
                if (typeof(T) == typeof(byte))
                    return (T)(object)byte.MinValue;
                if (typeof(T) == typeof(short))
                    return (T)(object)short.MinValue;
                if (typeof(T) == typeof(ushort))
                    return (T)(object)ushort.MinValue;
                if (typeof(T) == typeof(int))
                    return (T)(object)int.MinValue;
                if (typeof(T) == typeof(uint))
                    return (T)(object)uint.MinValue;
                if (typeof(T) == typeof(long))
                    return (T)(object)long.MinValue;
                if (typeof(T) == typeof(ulong))
                    return (T)(object)ulong.MinValue;
                if (typeof(T) == typeof(float))
                    return (T)(object)float.MinValue;

                return (T)(object)double.MinValue;
            }
        }

        /// <summary>
        /// The default <see cref="MaxValue"/>. This should be equal to the maximum value of type <typeparamref name="T"/>.
        /// </summary>
        protected virtual T DefaultMaxValue
        {
            get
            {
                Debug.Assert(isSupportedType());

                if (typeof(T) == typeof(sbyte))
                    return (T)(object)sbyte.MaxValue;
                if (typeof(T) == typeof(byte))
                    return (T)(object)byte.MaxValue;
                if (typeof(T) == typeof(short))
                    return (T)(object)short.MaxValue;
                if (typeof(T) == typeof(ushort))
                    return (T)(object)ushort.MaxValue;
                if (typeof(T) == typeof(int))
                    return (T)(object)int.MaxValue;
                if (typeof(T) == typeof(uint))
                    return (T)(object)uint.MaxValue;
                if (typeof(T) == typeof(long))
                    return (T)(object)long.MaxValue;
                if (typeof(T) == typeof(ulong))
                    return (T)(object)ulong.MaxValue;
                if (typeof(T) == typeof(float))
                    return (T)(object)float.MaxValue;

                return (T)(object)double.MaxValue;
            }
        }

        /// <summary>
        /// The default <see cref="Precision"/>.
        /// </summary>
        protected virtual T DefaultPrecision
        {
            get
            {
                if (typeof(T) == typeof(sbyte))
                    return (T)(object)(sbyte)1;
                if (typeof(T) == typeof(byte))
                    return (T)(object)(byte)1;
                if (typeof(T) == typeof(short))
                    return (T)(object)(short)1;
                if (typeof(T) == typeof(ushort))
                    return (T)(object)(ushort)1;
                if (typeof(T) == typeof(int))
                    return (T)(object)1;
                if (typeof(T) == typeof(uint))
                    return (T)(object)1U;
                if (typeof(T) == typeof(long))
                    return (T)(object)1L;
                if (typeof(T) == typeof(ulong))
                    return (T)(object)1UL;
                if (typeof(T) == typeof(float))
                    return (T)(object)float.Epsilon;

                return (T)(object)double.Epsilon;
            }
        }

        public override void TriggerChange()
        {
            base.TriggerChange();

            TriggerPrecisionChange(this, false);
            TriggerMinValueChange(this, false);
            TriggerMaxValueChange(this, false);
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

        protected void TriggerMinValueChange(BindableNumber<T> source = null, bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = minValue;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    if (b is BindableNumber<T> bn)
                        bn.SetMinValue(minValue, false, this);
                }
            }

            if (beforePropagation.Equals(minValue))
                MinValueChanged?.Invoke(minValue);
        }

        protected void TriggerMaxValueChange(BindableNumber<T> source = null, bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = maxValue;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    if (b is BindableNumber<T> bn)
                        bn.SetMaxValue(maxValue, false, this);
                }
            }

            if (beforePropagation.Equals(maxValue))
                MaxValueChanged?.Invoke(maxValue);
        }

        public override void BindTo(Bindable<T> them)
        {
            if (them is BindableNumber<T> other)
            {
                Precision = other.Precision;
                MinValue = other.MinValue;
                MaxValue = other.MaxValue;

                if (MinValue.CompareTo(MaxValue) > 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(them), $"Can not weld bindable longs with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{other.MinValue} - {other.MaxValue}].");
                }
            }

            base.BindTo(them);
        }

        /// <summary>
        /// Whether this bindable has a user-defined range that is not the full range of the <typeparamref name="T"/> type.
        /// </summary>
        public bool HasDefinedRange => !MinValue.Equals(DefaultMinValue) || !MaxValue.Equals(DefaultMaxValue);

        public bool IsInteger =>
            typeof(T) != typeof(float) &&
            typeof(T) != typeof(double); // Will be **constant** after JIT.

        public void Set<TNewValue>(TNewValue val) where TNewValue : struct,
            IFormattable, IConvertible, IComparable<TNewValue>, IEquatable<TNewValue>
        {
            Debug.Assert(isSupportedType());

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
        }

        public void Add<TNewValue>(TNewValue val) where TNewValue : struct,
            IFormattable, IConvertible, IComparable<TNewValue>, IEquatable<TNewValue>
        {
            Debug.Assert(isSupportedType());

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

        public override string ToString() => Value.ToString(NumberFormatInfo.InvariantInfo);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool isSupportedType() =>
            typeof(T) == typeof(sbyte)
            || typeof(T) == typeof(byte)
            || typeof(T) == typeof(short)
            || typeof(T) == typeof(ushort)
            || typeof(T) == typeof(int)
            || typeof(T) == typeof(uint)
            || typeof(T) == typeof(long)
            || typeof(T) == typeof(ulong)
            || typeof(T) == typeof(float)
            || typeof(T) == typeof(double);
    }
}
