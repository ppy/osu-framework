// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;

namespace osu.Framework.Configuration
{
    public class BindableLong : BindableNumber<long>
    {
        public override long Value
        {
            get { return base.Value; }
            set { base.Value = Math.Max(MinValue, Math.Min(MaxValue, value)); }
        }

        protected override long DefaultMinValue => long.MinValue;
        protected override long DefaultMaxValue => long.MaxValue;
        protected override long DefaultPrecision => 1;

        public BindableLong(long value = 0)
            : base(value)
        {
        }

        public override void BindTo(Bindable<long> them)
        {
            var i = them as BindableLong;
            if (i != null)
            {
                MinValue = Math.Max(MinValue, i.MinValue);
                MaxValue = Math.Min(MaxValue, i.MaxValue);
                if (MinValue > MaxValue)
                    throw new ArgumentOutOfRangeException(
                        $"Can not weld bindable longs with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{i.MinValue} - {i.MaxValue}].", nameof(them));
            }

            base.BindTo(them);
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
