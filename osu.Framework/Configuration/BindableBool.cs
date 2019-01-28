﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        public override void Parse(object input)
        {
            if (input.Equals("1"))
                Value = true;
            else if (input.Equals("0"))
                Value = false;
            else
                base.Parse(input);
        }

        public void Toggle() => Value = !Value;
    }
}
