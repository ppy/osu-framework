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

        /// <summary>
        /// The precision up to which the value of this bindable should be rounded.
        /// </summary>
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

        protected override double DefaultMinValue => double.MinValue;
        protected override double DefaultMaxValue => double.MaxValue;

        public BindableDouble(double value = 0)
            : base(value)
        {
        }

        /// <summary>
        /// Binds outselves to another bindable such that they receive bi-directional updates.
        /// We will take on any value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
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
