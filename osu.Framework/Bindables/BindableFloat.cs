// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;

namespace osu.Framework.Bindables
{
    public class BindableFloat : BindableNumber<float>
    {
        // Take 50% of the precision to ensure the value doesn't underflow and return true for non-default values.
        public override bool IsDefault => MathUtils.Precision.AlmostEquals(Value, Default, Precision / 2);

        protected override float DefaultMinValue => float.MinValue;
        protected override float DefaultMaxValue => float.MaxValue;
        protected override float DefaultPrecision => float.Epsilon;

        public BindableFloat(float value = 0)
            : base(value)
        {
        }

        public override string ToString() => Value.ToString("0.0###", NumberFormatInfo.InvariantInfo);
    }
}
