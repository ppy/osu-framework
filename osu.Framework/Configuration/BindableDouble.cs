// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;
using OpenTK;

namespace osu.Framework.Configuration
{
    public class BindableDouble : BindableNumber<double>
    {
        public override bool IsDefault => Math.Abs(Value - Default) < Precision;

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

        protected override double DefaultMinValue => double.MinValue;
        protected override double DefaultMaxValue => double.MaxValue;
        protected override double DefaultPrecision => double.Epsilon;

        public BindableDouble(double value = 0)
            : base(value)
        {
        }

        public override string ToString() => Value.ToString("0.0###", NumberFormatInfo.InvariantInfo);

        /// <summary>
        /// Parse an input into this instance.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        public override void Parse(object input)
        {
            string str = input as string;
            if (str == null)
                throw new InvalidCastException($@"Input type {input.GetType()} could not be cast to a string for parsing");

            var parsed = double.Parse(str, NumberFormatInfo.InvariantInfo);
            if (parsed < MinValue || parsed > MaxValue)
                throw new ArgumentOutOfRangeException($"Parsed number ({parsed}) is outside the valid range ({MinValue} - {MaxValue})");

            Value = parsed;
        }
    }
}
