// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Globalization;

namespace osu.Framework.Bindables
{
    public class BindableDouble : BindableNumber<double>
    {
        public BindableDouble(double defaultValue = 0)
            : base(defaultValue)
        {
        }

        public override string ToString() => Value.ToString("0.0###", NumberFormatInfo.InvariantInfo);

        protected override Bindable<double> CreateInstance() => new BindableDouble();
    }
}
