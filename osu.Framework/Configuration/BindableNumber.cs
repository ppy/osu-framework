﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace osu.Framework.Configuration
{
    public abstract class BindableNumber<T> : Bindable<T>, IBindableNumber<T>
        where T : struct, IComparable, IConvertible
    {
        static BindableNumber()
        {
            // check supported types against provided type argument.
            var allowedTypes = new HashSet<Type>
            {
                typeof(sbyte),
                typeof(byte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double)
            };

            if (!allowedTypes.Contains(typeof(T)))
                throw new ArgumentException(
                    $"{nameof(BindableNumber<T>)} only accepts the primitive numeric types (except for {typeof(decimal).FullName}) as type arguments. You provided {typeof(T).FullName}.");
        }

        public event Action<T> PrecisionChanged;

        public event Action<T> MinValueChanged;

        public event Action<T> MaxValueChanged;

        protected BindableNumber(T value = default)
            : base(value)
        {
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

                if (Convert.ToDouble(value) <= 0)
                    throw new ArgumentOutOfRangeException(nameof(Precision), "Must be greater than 0.");

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
                    double doubleValue = Convert.ToDouble(clamp(value, MinValue, MaxValue));
                    doubleValue = Math.Round(doubleValue / Convert.ToDouble(Precision)) * Convert.ToDouble(Precision);

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

            if (propagateToBindings)
            {
                Bindings?.ForEachAlive(b =>
                {
                    if (b is BindableNumber<T> bn)
                        bn.Precision = precision;
                });
            }

            if (Equals(beforePropagation, precision))
                PrecisionChanged?.Invoke(precision);
        }

        protected void TriggerMinValueChange(bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = minValue;

            if (propagateToBindings)
            {
                Bindings?.ForEachAlive(b =>
                {
                    if (b is BindableNumber<T> bn)
                        bn.MinValue = minValue;
                });
            }

            if (Equals(beforePropagation, minValue))
                MinValueChanged?.Invoke(minValue);
        }

        protected void TriggerMaxValueChange(bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = maxValue;

            if (propagateToBindings)
            {
                Bindings?.ForEachAlive(b =>
                {
                    if (b is BindableNumber<T> bn)
                        bn.MaxValue = maxValue;
                });
            }

            if (Equals(beforePropagation, maxValue))
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

        public static implicit operator T(BindableNumber<T> value) =>
            value?.Value ?? throw new InvalidCastException($"Casting a null {nameof(BindableNumber<T>)} to a {nameof(T)} is likely a mistake");

        public bool IsInteger
        {
            get
            {
                switch (Type.GetTypeCode(typeof(T)))
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.UInt16:
                    case TypeCode.Int16:
                    case TypeCode.UInt32:
                    case TypeCode.Int32:
                    case TypeCode.UInt64:
                    case TypeCode.Int64:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public void Set<U>(U val) where U : struct,
            IComparable, IFormattable, IConvertible, IComparable<U>, IEquatable<U>
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Byte:
                    var byteBindable = this as BindableNumber<byte>;
                    if (byteBindable == null) throw new ArgumentNullException(nameof(byteBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    byteBindable.Value = Convert.ToByte(val);
                    break;
                case TypeCode.SByte:
                    var sbyteBindable = this as BindableNumber<sbyte>;
                    if (sbyteBindable == null) throw new ArgumentNullException(nameof(sbyteBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    sbyteBindable.Value = Convert.ToSByte(val);
                    break;
                case TypeCode.UInt16:
                    var ushortBindable = this as BindableNumber<ushort>;
                    if (ushortBindable == null) throw new ArgumentNullException(nameof(ushortBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    ushortBindable.Value = Convert.ToUInt16(val);
                    break;
                case TypeCode.Int16:
                    var shortBindable = this as BindableNumber<short>;
                    if (shortBindable == null) throw new ArgumentNullException(nameof(shortBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    shortBindable.Value = Convert.ToInt16(val);
                    break;
                case TypeCode.UInt32:
                    var uintBindable = this as BindableNumber<uint>;
                    if (uintBindable == null) throw new ArgumentNullException(nameof(uintBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    uintBindable.Value = Convert.ToUInt32(val);
                    break;
                case TypeCode.Int32:
                    var intBindable = this as BindableNumber<int>;
                    if (intBindable == null) throw new ArgumentNullException(nameof(intBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    intBindable.Value = Convert.ToInt32(val);
                    break;
                case TypeCode.UInt64:
                    var ulongBindable = this as BindableNumber<ulong>;
                    if (ulongBindable == null) throw new ArgumentNullException(nameof(ulongBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    ulongBindable.Value = Convert.ToUInt64(val);
                    break;
                case TypeCode.Int64:
                    var longBindable = this as BindableNumber<long>;
                    if (longBindable == null) throw new ArgumentNullException(nameof(longBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    longBindable.Value = Convert.ToInt64(val);
                    break;
                case TypeCode.Single:
                    var floatBindable = this as BindableNumber<float>;
                    if (floatBindable == null) throw new ArgumentNullException(nameof(floatBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    floatBindable.Value = Convert.ToSingle(val);
                    break;
                case TypeCode.Double:
                    var doubleBindable = this as BindableNumber<double>;
                    if (doubleBindable == null) throw new ArgumentNullException(nameof(doubleBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    doubleBindable.Value = Convert.ToDouble(val);
                    break;
            }
        }

        public void Add<U>(U val) where U : struct,
            IComparable, IFormattable, IConvertible, IComparable<U>, IEquatable<U>
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Byte:
                    var byteBindable = this as BindableNumber<byte>;
                    if (byteBindable == null) throw new ArgumentNullException(nameof(byteBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    byteBindable.Value += Convert.ToByte(val);
                    break;
                case TypeCode.SByte:
                    var sbyteBindable = this as BindableNumber<sbyte>;
                    if (sbyteBindable == null) throw new ArgumentNullException(nameof(sbyteBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    sbyteBindable.Value += Convert.ToSByte(val);
                    break;
                case TypeCode.UInt16:
                    var ushortBindable = this as BindableNumber<ushort>;
                    if (ushortBindable == null) throw new ArgumentNullException(nameof(ushortBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    ushortBindable.Value += Convert.ToUInt16(val);
                    break;
                case TypeCode.Int16:
                    var shortBindable = this as BindableNumber<short>;
                    if (shortBindable == null) throw new ArgumentNullException(nameof(shortBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    shortBindable.Value += Convert.ToInt16(val);
                    break;
                case TypeCode.UInt32:
                    var uintBindable = this as BindableNumber<uint>;
                    if (uintBindable == null) throw new ArgumentNullException(nameof(uintBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    uintBindable.Value += Convert.ToUInt32(val);
                    break;
                case TypeCode.Int32:
                    var intBindable = this as BindableNumber<int>;
                    if (intBindable == null) throw new ArgumentNullException(nameof(intBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    intBindable.Value += Convert.ToInt32(val);
                    break;
                case TypeCode.UInt64:
                    var ulongBindable = this as BindableNumber<ulong>;
                    if (ulongBindable == null) throw new ArgumentNullException(nameof(ulongBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    ulongBindable.Value += Convert.ToUInt64(val);
                    break;
                case TypeCode.Int64:
                    var longBindable = this as BindableNumber<long>;
                    if (longBindable == null) throw new ArgumentNullException(nameof(longBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    longBindable.Value += Convert.ToInt64(val);
                    break;
                case TypeCode.Single:
                    var floatBindable = this as BindableNumber<float>;
                    if (floatBindable == null) throw new ArgumentNullException(nameof(floatBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    floatBindable.Value += Convert.ToSingle(val);
                    break;
                case TypeCode.Double:
                    var doubleBindable = this as BindableNumber<double>;
                    if (doubleBindable == null) throw new ArgumentNullException(nameof(doubleBindable), $"Generic type {typeof(T)} does not match actual bindable type {GetType()}.");
                    doubleBindable.Value += Convert.ToDouble(val);
                    break;
            }
        }

        /// <summary>
        /// Sets the value of the bindable to Min + (Max - Min) * amt
        /// <param name="amt">The proportional amount to set, ranging from 0 to 1.</param>
        /// <param name="snap">If greater than 0, snap the final value to the closest multiple of this number.</param>
        /// </summary>
        public void SetProportional(float amt, float snap = 0)
        {
            var min = Convert.ToDouble(MinValue);
            var max = Convert.ToDouble(MaxValue);
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
