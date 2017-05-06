// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;
using OpenTK;

namespace osu.Framework.Configuration
{
    public class BindableDouble : BindableNumber<double>
    {
        public override bool IsDefault => Math.Abs(Value - Default) < Precision;

        public double Precision = double.Epsilon;

        public override double Value
        {
            get { return base.Value; }
            set
            {
                double boundValue = MathHelper.Clamp(value, MinValue, MaxValue);

                if (Precision > double.Epsilon)
                    boundValue = Math.Round(boundValue / Precision) * Precision;

                base.Value = boundValue;
            }
        }

        public BindableDouble(double value = 0)
            : base(value)
        {
            MinValue = double.MinValue;
            MaxValue = double.MaxValue;
        }

        public override void BindTo(Bindable<double> them)
        {
            var dbl = them as BindableDouble;
            if (dbl != null)
            {
                MinValue = Math.Max(MinValue, dbl.MinValue);
                MaxValue = Math.Min(MaxValue, dbl.MaxValue);
                if (MinValue > MaxValue)
                    throw new ArgumentOutOfRangeException(
                        $"Can not weld bindable doubles with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{dbl.MinValue} - {dbl.MaxValue}].", nameof(them));
            }

            base.BindTo(them);
        }

        public override string ToString() => Value.ToString("0.0###", NumberFormatInfo.InvariantInfo);

        public override void Parse(object s)
        {
            string str = s as string;
            if (str == null)
                throw new InvalidCastException($@"Input type {s.GetType()} could not be cast to a string for parsing");

            var parsed = double.Parse(str, NumberFormatInfo.InvariantInfo);
            if (parsed < MinValue || parsed > MaxValue)
                throw new ArgumentOutOfRangeException($"Parsed number ({parsed}) is outside the valid range ({MinValue} - {MaxValue})");

            Value = parsed;
        }
    }
}
