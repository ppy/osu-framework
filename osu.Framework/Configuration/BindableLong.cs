// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;
using OpenTK;

namespace osu.Framework.Configuration
{
    public class BindableLong : BindableNumber<long>
    {
        public override long Value
        {
            get { return base.Value; }
            set
            {
                double doubleValue = MathHelper.Clamp(value, MinValue, MaxValue);
                base.Value = (long)Math.Round(doubleValue / Precision) * Precision;
            }
        }

        protected override long DefaultMinValue => long.MinValue;
        protected override long DefaultMaxValue => long.MaxValue;
        protected override long DefaultPrecision => 1;

        public BindableLong(long value = 0)
            : base(value)
        {
        }

        public override void Parse(object s)
        {
            string str = s as string;
            if (str == null)
                throw new InvalidCastException($@"Input type {s.GetType()} could not be cast to a string for parsing");

            var parsed = long.Parse(str, NumberFormatInfo.InvariantInfo);
            if (parsed < MinValue || parsed > MaxValue)
                throw new ArgumentOutOfRangeException($"Parsed number ({parsed}) is outside the valid range ({MinValue} - {MaxValue})");

            Value = parsed;
        }

        public override string ToString() => Value.ToString(NumberFormatInfo.InvariantInfo);
    }
}
