// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;

namespace osu.Framework.Bindables
{
    public class BindableDouble : BindableNumber<double>
    {
        // Take 50% of the precision to ensure the value doesn't underflow and return true for non-default values.
        public override bool IsDefault => MathUtils.Precision.AlmostEquals(Value, Default, Precision / 2);

        protected override double DefaultMinValue => double.MinValue;
        protected override double DefaultMaxValue => double.MaxValue;
        protected override double DefaultPrecision => double.Epsilon;

        public BindableDouble(double value = 0)
            : base(value)
        {
        }

        public override string ToString() => Value.ToString("0.0###", NumberFormatInfo.InvariantInfo);
    }
}
