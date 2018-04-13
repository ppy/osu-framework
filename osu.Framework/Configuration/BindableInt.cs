// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
