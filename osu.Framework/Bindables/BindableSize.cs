// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Represents a <see cref="Size"/> bindable with defined component-wise constraints applied to it.
    /// </summary>
    public class BindableSize : RangeConstrainedBindable<Size>
    {
        protected override Size DefaultMinValue => new Size(int.MinValue, int.MinValue);
        protected override Size DefaultMaxValue => new Size(int.MaxValue, int.MaxValue);

        public BindableSize(Size defaultValue = default)
            : base(defaultValue)
        {
        }

        public override string ToString(string format, IFormatProvider formatProvider) => ((FormattableString)$"{Value.Width}x{Value.Height}").ToString(formatProvider);

        public override void Parse(object input)
        {
            switch (input)
            {
                case string str:
                    string[] split = str.Split('x');

                    if (split.Length != 2)
                        throw new ArgumentException($"Input string was in wrong format! (expected: '<width>x<height>', actual: '{str}')");

                    Value = new Size(int.Parse(split[0]), int.Parse(split[1]));
                    break;

                default:
                    base.Parse(input);
                    break;
            }
        }

        protected override Bindable<Size> CreateInstance() => new BindableSize();

        protected sealed override Size ClampValue(Size value, Size minValue, Size maxValue)
        {
            return new Size
            {
                Width = Math.Clamp(value.Width, minValue.Width, maxValue.Width),
                Height = Math.Clamp(value.Height, minValue.Height, maxValue.Height)
            };
        }

        protected sealed override bool IsValidRange(Size min, Size max) => min.Width <= max.Width && min.Height <= max.Height;
    }
}
