// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Configuration
{
    public abstract class BindableNumber<T> : Bindable<T> where T : struct
    {
        static BindableNumber()
        {
            // check supported types against provided type argument.
            var allowedTypes = new HashSet<Type>()
            {
                typeof(sbyte), typeof(byte),
                typeof(short), typeof(ushort),
                typeof(int), typeof(uint),
                typeof(long), typeof(ulong),
                typeof(float), typeof(double)
            };
            if (!allowedTypes.Contains(typeof(T)))
                throw new ArgumentException($"{nameof(BindableNumber<T>)} only accepts the primitive numeric types (except for {typeof(decimal).FullName}) as type arguments. You provided {typeof(T).FullName}.");
        }

        protected BindableNumber(T value = default(T)) : base(value)
        {
        }

        public T MinValue { get; set; }
        public T MaxValue { get; set; }

        public static implicit operator T(BindableNumber<T> value) => value?.Value ?? default(T);

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
                    BindableNumber<byte> byteBindable = this as BindableNumber<byte>;
                    byteBindable.Value = Convert.ToByte(val);
                    break;
                case TypeCode.SByte:
                    BindableNumber<sbyte> sbyteBindable = this as BindableNumber<sbyte>;
                    sbyteBindable.Value = Convert.ToSByte(val);
                    break;
                case TypeCode.UInt16:
                    BindableNumber<ushort> ushortBindable = this as BindableNumber<ushort>;
                    ushortBindable.Value = Convert.ToUInt16(val);
                    break;
                case TypeCode.Int16:
                    BindableNumber<short> shortBindable = this as BindableNumber<short>;
                    shortBindable.Value = Convert.ToInt16(val);
                    break;
                case TypeCode.UInt32:
                    BindableNumber<uint> uintBindable = this as BindableNumber<uint>;
                    uintBindable.Value = Convert.ToUInt32(val);
                    break;
                case TypeCode.Int32:
                    BindableNumber<int> intBindable = this as BindableNumber<int>;
                    intBindable.Value = Convert.ToInt32(val);
                    break;
                case TypeCode.UInt64:
                    BindableNumber<ulong> ulongBindable = this as BindableNumber<ulong>;

                    ulongBindable.Value = Convert.ToUInt64(val);
                    break;
                case TypeCode.Int64:
                    BindableNumber<long> longBindable = this as BindableNumber<long>;
                    longBindable.Value = Convert.ToInt64(val);
                    break;
                case TypeCode.Single:
                    BindableNumber<float> floatBindable = this as BindableNumber<float>;

                    floatBindable.Value = Convert.ToSingle(val);
                    break;
                case TypeCode.Double:
                    BindableNumber<double> doubleBindable = this as BindableNumber<double>;
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
                    BindableNumber<byte> byteBindable = this as BindableNumber<byte>;
                    byteBindable.Value += Convert.ToByte(val);
                    break;
                case TypeCode.SByte:
                    BindableNumber<sbyte> sbyteBindable = this as BindableNumber<sbyte>;
                    sbyteBindable.Value += Convert.ToSByte(val);
                    break;
                case TypeCode.UInt16:
                    BindableNumber<ushort> ushortBindable = this as BindableNumber<ushort>;
                    ushortBindable.Value += Convert.ToUInt16(val);
                    break;
                case TypeCode.Int16:
                    BindableNumber<short> shortBindable = this as BindableNumber<short>;
                    shortBindable.Value += Convert.ToInt16(val);
                    break;
                case TypeCode.UInt32:
                    BindableNumber<uint> uintBindable = this as BindableNumber<uint>;
                    uintBindable.Value += Convert.ToUInt32(val);
                    break;
                case TypeCode.Int32:
                    BindableNumber<int> intBindable = this as BindableNumber<int>;
                    intBindable.Value += Convert.ToInt32(val);
                    break;
                case TypeCode.UInt64:
                    BindableNumber<ulong> ulongBindable = this as BindableNumber<ulong>;
                    ulongBindable.Value += Convert.ToUInt64(val);
                    break;
                case TypeCode.Int64:
                    BindableNumber<long> longBindable = this as BindableNumber<long>;
                    longBindable.Value += Convert.ToInt64(val);
                    break;
                case TypeCode.Single:
                    BindableNumber<float> floatBindable = this as BindableNumber<float>;
                    floatBindable.Value += Convert.ToSingle(val);
                    break;
                case TypeCode.Double:
                    BindableNumber<double> doubleBindable = this as BindableNumber<double>;
                    doubleBindable.Value += Convert.ToDouble(val);
                    break;
            }
        }

        /// <summary>
        /// Sets the value of the bindable to Min + (Max - Min) * amt
        /// </summary>
        public void SetProportional(float amt)
        {
            var min = Convert.ToDouble(MinValue);
            var max = Convert.ToDouble(MaxValue);
            Set(min + (max - min) * amt);
        }
    }
}