// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;
using OpenTK;

namespace osu.Framework.Configuration
{
    public class BindableInt : BindableNumber<int>
    {
        public override int Value
        {
            get { return base.Value; }
            set { base.Value = MathHelper.Clamp(value, MinValue, MaxValue); }
        }

        protected override int DefaultMinValue => int.MinValue;
        protected override int DefaultMaxValue => int.MaxValue;
        protected override int DefaultPrecision => 1;

        public BindableInt(int value = 0)
            : base(value)
        {
        }

        public override void BindTo(Bindable<int> them)
        {
            var i = them as BindableInt;
            if (i != null)
            {
                MinValue = Math.Max(MinValue, i.MinValue);
                MaxValue = Math.Min(MaxValue, i.MaxValue);
                if (MinValue > MaxValue)
                    throw new ArgumentOutOfRangeException(
                        $"Can not weld bindable ints with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{i.MinValue} - {i.MaxValue}].", nameof(them));
            }

            base.BindTo(them);
        }

        public override void Parse(object s)
        {
            string str = s as string;
            if (str == null)
                throw new InvalidCastException($@"Input type {s.GetType()} could not be cast to a string for parsing");

            var parsed = int.Parse(str, NumberFormatInfo.InvariantInfo);
            if (parsed < MinValue || parsed > MaxValue)
                throw new ArgumentOutOfRangeException($"Parsed number ({parsed}) is outside the valid range ({MinValue} - {MaxValue})");

            Value = parsed;
        }

        public override string ToString() => Value.ToString(NumberFormatInfo.InvariantInfo);
    }
}
