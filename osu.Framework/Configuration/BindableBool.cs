// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Configuration
{
    public class BindableBool : Bindable<bool>
    {
        public BindableBool(bool value = false)
            : base(value)
        {
        }

        public static implicit operator bool(BindableBool value) => value != null && value.Value;

        public override string ToString() => Value.ToString();

        public override bool Parse(object s)
        {
            string str = s as string;
            if (str == null) return false;

            Value = str == @"1" || str.Equals(@"true", System.StringComparison.OrdinalIgnoreCase);
            return true;
        }

        public void Toggle() => Value = !Value;
    }
}
