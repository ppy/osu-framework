// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Represents a <see cref="Size"/> bindable with defined component-wise constraints applied to it.
    /// </summary>
    public class BindableSize : ConstrainedBindable<Size>
    {
        protected override Size DefaultMinValue => new Size(int.MinValue, int.MinValue);
        protected override Size DefaultMaxValue => new Size(int.MaxValue, int.MaxValue);

        public BindableSize(Size defaultValue = default)
            : base(defaultValue)
        {
        }

        public override string ToString() => $"{Value.Width}x{Value.Height}";

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

        protected override Size ClampValue(Size value, Size minValue, Size maxValue)
        {
            return new Size
            (
                Math.Clamp(value.Width, minValue.Width, maxValue.Width),
                Math.Clamp(value.Height, minValue.Height, maxValue.Height)
            );
        }

        protected override int Compare(Size x, Size y)
        {
            if (x.Width == y.Width && x.Height == y.Height)
                return 0;

            return Math.Max(x.Width.CompareTo(y.Width), x.Height.CompareTo(y.Height));
        }
    }
}
