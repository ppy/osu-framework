// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    public class BindableBool : Bindable<bool>
    {
        public BindableBool(bool value = false)
            : base(value)
        {
        }

        public static implicit operator bool(BindableBool value) => value?.Value ?? throw new InvalidCastException($"Casting a null {nameof(BindableBool)} to a bool is likely a mistake");

        public override string ToString() => Value.ToString();

        public override void Parse(object s)
        {
            string str = s as string;
            if (str == null)
                throw new InvalidCastException($@"Input type {s.GetType()} could not be cast to a string for parsing");

            Value = str == @"1" || str.Equals(@"true", StringComparison.OrdinalIgnoreCase);
        }

        public void Toggle() => Value = !Value;
    }
}
