// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;

namespace osu.Framework.Configuration
{
    public class BindableSize : Bindable<Size>
    {
        public BindableSize(Size value = default(Size))
            : base(value)
        {
            MinValue = DefaultMinValue;
            MaxValue = DefaultMaxValue;
        }

        public Size MinValue { get; set; }
        public Size MaxValue { get; set; }

        protected Size DefaultMinValue => new Size(int.MinValue, int.MinValue);
        protected Size DefaultMaxValue => new Size(int.MaxValue, int.MaxValue);

        public override Size Value
        {
            get => base.Value;
            set => base.Value = clamp(value, MinValue, MaxValue);
        }

        public override void BindTo(Bindable<Size> them)
        {
            if (them is BindableSize other)
            {
                MinValue = new Size(Math.Max(MinValue.Width, other.MinValue.Width), Math.Max(MinValue.Height, other.MinValue.Height));
                MaxValue = new Size(Math.Min(MaxValue.Width, other.MaxValue.Width), Math.Min(MaxValue.Height, other.MaxValue.Height));

                if (MinValue.Width > MaxValue.Width || MinValue.Height > MaxValue.Height)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(them),
                        $"Can not weld BindableSizes with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{other.MinValue} - {other.MaxValue}]."
                    );
                }
            }

            base.BindTo(them);
        }

        public static implicit operator Size(BindableSize value) => value?.Value ?? throw new InvalidCastException($"Casting a null {nameof(BindableSize)} to a {nameof(Size)} is likely a mistake");

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

        private static Size clamp(Size value, Size minValue, Size maxValue)
        {
            return new Size(
                Math.Max(minValue.Width, Math.Min(value.Width, maxValue.Width)),
                Math.Max(minValue.Height, Math.Min(value.Height, maxValue.Height))
            );
        }
    }
}
