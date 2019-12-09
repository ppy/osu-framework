// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;

namespace osu.Framework.Bindables
{
    [Obsolete("Use BindableNumber<int> instead")] // can be removed 20200609
    public class BindableInt : BindableNumber<int>
    {
        public BindableInt(int value = 0)
            : base(value)
        {
        }

        public override string ToString() => Value.ToString(NumberFormatInfo.InvariantInfo);
    }
}
