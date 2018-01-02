// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;
using System.Globalization;

namespace osu.Framework.Configuration
{
    public class BindableFloat : BindableNumberWithPrecision<float>
    {
        public override bool IsDefault => Math.Abs(Value - Default) < Precision;

        public override float Value
        {
            get { return base.Value; }
            set
            {
                float boundValue = MathHelper.Clamp(value, MinValue, MaxValue);

                if (Precision > float.Epsilon)
                    boundValue = (float)Math.Round(boundValue / Precision) * Precision;

                base.Value = boundValue;
            }
        }

        protected override float DefaultMinValue => float.MinValue;
        protected override float DefaultMaxValue => float.MaxValue;
        protected override float DefaultPrecision => float.Epsilon;

        public BindableFloat(float value = 0)
            : base(value)
        {
        }

        /// <summary>
        /// Binds outselves to another bindable such that they receive bi-directional updates.
        /// We will take on any value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        public override void BindTo(Bindable<float> them)
        {
            if (them is BindableFloat other)
            {
                Precision = Math.Max(Precision, other.Precision);
                MinValue = Math.Max(MinValue, other.MinValue);
                MaxValue = Math.Min(MaxValue, other.MaxValue);
                if (MinValue > MaxValue)
                    throw new ArgumentOutOfRangeException(
                        $"Can not weld bindable singles with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{other.MinValue} - {other.MaxValue}].", nameof(them));
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

            var parsed = float.Parse(str, NumberFormatInfo.InvariantInfo);
            if (parsed < MinValue || parsed > MaxValue)
                throw new ArgumentOutOfRangeException($"Parsed number ({parsed}) is outside the valid range ({MinValue} - {MaxValue})");

            Value = parsed;
        }
    }
}
