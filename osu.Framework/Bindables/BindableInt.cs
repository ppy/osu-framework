// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Globalization;

namespace osu.Framework.Bindables
{
    public class BindableInt : BindableNumber<int>
    {
        public BindableInt(int defaultValue = 0)
            : base(defaultValue)
        {
        }

        public override string ToString() => Value.ToString(NumberFormatInfo.InvariantInfo);

        protected override Bindable<int> CreateInstance() => new BindableInt();
    }
}
