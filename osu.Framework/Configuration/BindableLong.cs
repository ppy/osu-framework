﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;

namespace osu.Framework.Configuration
{
    public class BindableLong : BindableNumber<long>
    {
        public override long Value
        {
            get { return base.Value; }
            set { base.Value = Math.Max(MinValue, Math.Min(MaxValue, value)); }
        }

        public BindableLong(long value = 0) : base(value)
        {
            MinValue = long.MinValue;
            MaxValue = long.MaxValue;
        }

        public override void BindTo(Bindable<long> them)
        {
            var i = them as BindableLong;
            if (i != null)
            {
                MinValue = Math.Max(MinValue, i.MinValue);
                MaxValue = Math.Min(MaxValue, i.MaxValue);
                if (MinValue > MaxValue)
                    throw new ArgumentOutOfRangeException($"Can not weld bindable longs with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{i.MinValue} - {i.MaxValue}].", nameof(them));
            }

            base.BindTo(them);
        }

        public override bool Parse(object s)
        {
            string str = s as string;
            if (str == null) return false;

            Value = long.Parse(str, NumberFormatInfo.InvariantInfo);
            return true;
        }

        public override string ToString() => Value.ToString(NumberFormatInfo.InvariantInfo);
    }
}