// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;

namespace osu.Framework.Configuration
{
    public class BindableInt : BindableNumber<int>
    {
        protected override int DefaultMinValue => int.MinValue;
        protected override int DefaultMaxValue => int.MaxValue;
        protected override int DefaultPrecision => 1;

        public BindableInt(int value = 0)
            : base(value)
        {
        }

        public override string ToString() => Value.ToString(NumberFormatInfo.InvariantInfo);
    }
}
