// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;

namespace osu.Framework.Configuration
{
    public class BindableFloat : BindableNumber<float>
    {
        public override bool IsDefault => Math.Abs(Value - Default) < Precision;

        protected override float DefaultMinValue => float.MinValue;
        protected override float DefaultMaxValue => float.MaxValue;
        protected override float DefaultPrecision => float.Epsilon;

        public BindableFloat(float value = 0)
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

            var parsed = float.Parse(str, NumberFormatInfo.InvariantInfo);
            if (parsed < MinValue || parsed > MaxValue)
                throw new ArgumentOutOfRangeException($"Parsed number ({parsed}) is outside the valid range ({MinValue} - {MaxValue})");

            Value = parsed;
        }
    }
}
