﻿using System;
using System.Diagnostics;
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

        public BindableLong(int value = 0) : base(value)
        {
            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
        }

        public override void Weld(Bindable<long> v, bool transferValue = true)
        {
            var i = v as BindableLong;
            if (i != null)
            {
                MinValue = Math.Max(MinValue, i.MinValue);
                MaxValue = Math.Min(MaxValue, i.MaxValue);
                Debug.Assert(MinValue <= MaxValue);
            }
            base.Weld(v, transferValue);
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