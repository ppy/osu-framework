// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;

namespace osu.Framework.Configuration
{
    public abstract class BindableNumber<T> : Bindable<T> where T : struct
    {
        protected BindableNumber(T value = default(T)) : base(value)
        {
            // Check that this is a numeric type
            // SURE WOULD BE NICE TO DO THIS AT COMPILE TIME, C#
            var code = Type.GetTypeCode(typeof(T));
            var invalid = new[]
            {
                TypeCode.Boolean,
                TypeCode.Char,
                TypeCode.Empty,
                TypeCode.Decimal, // TODO
                TypeCode.Object,
                TypeCode.String
            };
            if (Array.IndexOf(invalid, code) != -1)
                throw new InvalidOperationException("BindableNumber created with a non-numeric generic type argument");
        }

        public T MinValue { get; set; }
        public T MaxValue { get; set; }
        
        public static implicit operator T(BindableNumber<T> value) =>
            value == null ? default(T) : value.Value;
        
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
                    Debug.Assert(byteBindable != null);
                    byteBindable.Value = Convert.ToByte(val);
                    break;
                case TypeCode.SByte:
                    BindableNumber<sbyte> sbyteBindable = this as BindableNumber<sbyte>;
                    Debug.Assert(sbyteBindable != null);
                    sbyteBindable.Value = Convert.ToSByte(val);
                    break;
                case TypeCode.UInt16:
                    BindableNumber<ushort> ushortBindable = this as BindableNumber<ushort>;
                    Debug.Assert(ushortBindable != null);
                    ushortBindable.Value = Convert.ToUInt16(val);
                    break;
                case TypeCode.Int16:
                    BindableNumber<short> shortBindable = this as BindableNumber<short>;
                    Debug.Assert(shortBindable != null);
                    shortBindable.Value = Convert.ToInt16(val);
                    break;
                case TypeCode.UInt32:
                    BindableNumber<uint> uintBindable = this as BindableNumber<uint>;
                    Debug.Assert(uintBindable != null);
                    uintBindable.Value = Convert.ToUInt32(val);
                    break;
                case TypeCode.Int32:
                    BindableNumber<int> intBindable = this as BindableNumber<int>;
                    Debug.Assert(intBindable != null);
                    intBindable.Value = Convert.ToInt32(val);
                    break;
                case TypeCode.UInt64:
                    BindableNumber<ulong> ulongBindable = this as BindableNumber<ulong>;
                    Debug.Assert(ulongBindable != null);
                    ulongBindable.Value = Convert.ToUInt64(val);
                    break;
                case TypeCode.Int64:
                    BindableNumber<long> longBindable = this as BindableNumber<long>;
                    Debug.Assert(longBindable != null);
                    longBindable.Value = Convert.ToInt64(val);
                    break;
                case TypeCode.Single:
                    BindableNumber<float> floatBindable = this as BindableNumber<float>;
                    Debug.Assert(floatBindable != null);
                    floatBindable.Value = Convert.ToSingle(val);
                    break;
                case TypeCode.Double:
                    BindableNumber<double> doubleBindable = this as BindableNumber<double>;
                    Debug.Assert(doubleBindable != null);
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
                    Debug.Assert(byteBindable != null);
                    byteBindable.Value += Convert.ToByte(val);
                    break;
                case TypeCode.SByte:
                    BindableNumber<sbyte> sbyteBindable = this as BindableNumber<sbyte>;
                    Debug.Assert(sbyteBindable != null);
                    sbyteBindable.Value += Convert.ToSByte(val);
                    break;
                case TypeCode.UInt16:
                    BindableNumber<ushort> ushortBindable = this as BindableNumber<ushort>;
                    Debug.Assert(ushortBindable != null);
                    ushortBindable.Value += Convert.ToUInt16(val);
                    break;
                case TypeCode.Int16:
                    BindableNumber<short> shortBindable = this as BindableNumber<short>;
                    Debug.Assert(shortBindable != null);
                    shortBindable.Value += Convert.ToInt16(val);
                    break;
                case TypeCode.UInt32:
                    BindableNumber<uint> uintBindable = this as BindableNumber<uint>;
                    Debug.Assert(uintBindable != null);
                    uintBindable.Value += Convert.ToUInt32(val);
                    break;
                case TypeCode.Int32:
                    BindableNumber<int> intBindable = this as BindableNumber<int>;
                    Debug.Assert(intBindable != null);
                    intBindable.Value += Convert.ToInt32(val);
                    break;
                case TypeCode.UInt64:
                    BindableNumber<ulong> ulongBindable = this as BindableNumber<ulong>;
                    Debug.Assert(ulongBindable != null);
                    ulongBindable.Value += Convert.ToUInt64(val);
                    break;
                case TypeCode.Int64:
                    BindableNumber<long> longBindable = this as BindableNumber<long>;
                    Debug.Assert(longBindable != null);
                    longBindable.Value += Convert.ToInt64(val);
                    break;
                case TypeCode.Single:
                    BindableNumber<float> floatBindable = this as BindableNumber<float>;
                    Debug.Assert(floatBindable != null);
                    floatBindable.Value += Convert.ToSingle(val);
                    break;
                case TypeCode.Double:
                    BindableNumber<double> doubleBindable = this as BindableNumber<double>;
                    Debug.Assert(doubleBindable != null);
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