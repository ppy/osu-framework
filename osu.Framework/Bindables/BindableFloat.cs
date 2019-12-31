﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;

namespace osu.Framework.Bindables
{
    public class BindableFloat : BindableNumber<float>
    {
        public BindableFloat(float defaultValue = 0)
            : base(defaultValue)
        {
        }

        public override string ToString() => Value.ToString("0.0###", NumberFormatInfo.InvariantInfo);
    }
}
