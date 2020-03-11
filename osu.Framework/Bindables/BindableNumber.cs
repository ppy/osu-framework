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

        protected readonly BindableProperty<T> PrecisionProperty, MinValueProperty, MaxValueProperty;

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

            PrecisionProperty = new BindableProperty<T>(DefaultPrecision, this, b => (b as BindableNumber<T>)?.PrecisionProperty)
            {
                OnValueChange = (_, precision) => PrecisionChanged?.Invoke(precision),
            };

            MinValueProperty = new BindableProperty<T>(DefaultMinValue, this, b => (b as BindableNumber<T>)?.MinValueProperty)
            {
                OnValueChange = (_, minValue) => MinValueChanged?.Invoke(minValue),
            };

            MaxValueProperty = new BindableProperty<T>(DefaultMaxValue, this, b => (b as BindableNumber<T>)?.MaxValueProperty)
            {
                OnValueChange = (_, maxValue) => MaxValueChanged?.Invoke(maxValue),
            };

            // Update the current (default) value to apply precision/min-value/max-value to it.
            updateValue(Value);
        }

        public T Precision
        {
            get => PrecisionProperty.Value;
            set
            {
                PrecisionProperty.Value = value;

                // Update value with precision changes.
                updateValue(Value);
            }
        }

        public T MinValue
        {
            get => MinValueProperty.Value;
            set
            {
                MinValueProperty.Value = value;

                // Update value with minimum-value changes.
                updateValue(Value);
            }
        }

        public T MaxValue
        {
            get => MaxValueProperty.Value;
            set
            {
                MaxValueProperty.Value = value;

                // Update value with maximum-value changes.
                updateValue(Value);
            }
        }

        public override T Value
        {
            get => base.Value;
            set => updateValue(value);
        }

        private void updateValue(T value)
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

        protected override void CheckPropertyValueChange<TValue>(IBindableProperty<TValue> property, TValue value)
        {
            base.CheckPropertyValueChange(property, value);

            if (value is T number && property == PrecisionProperty)
            {
                if (number.CompareTo(default) <= 0)
                    throw new InvalidOperationException($"Can not set {nameof(Precision)} to zero or negative value: {value}");
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

            PrecisionProperty.TriggerChange(Precision, false);
            MinValueProperty.TriggerChange(MinValue, false);
            MaxValueProperty.TriggerChange(MaxValue, false);
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
