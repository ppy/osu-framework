// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Globalization;
using OpenTK;

namespace osu.Framework.Configuration
{
    public class BindableInt : BindableNumber<int>
    {
        public override int Value
        {
            get { return base.Value; }
            set { base.Value = MathHelper.Clamp(value, MinValue, MaxValue); }
        }

        public BindableInt(int value = 0) : base(value)
        {
            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
        }
        
        public override void Weld(Bindable<int> v, bool transferValue = true)
        {
            var i = v as BindableInt;
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

            Value = int.Parse(str, NumberFormatInfo.InvariantInfo);
            return true;
        }

        public override string ToString() => Value.ToString(NumberFormatInfo.InvariantInfo);
    }
}
