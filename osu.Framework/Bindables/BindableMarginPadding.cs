// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using osu.Framework.Graphics;

namespace osu.Framework.Bindables
{
    public class BindableMarginPadding : Bindable<MarginPadding>
    {
        public BindableMarginPadding(MarginPadding value = default)
            : base(value)
        {
            MinValue = DefaultMinValue;
            MaxValue = DefaultMaxValue;
        }

        public MarginPadding MinValue { get; set; }
        public MarginPadding MaxValue { get; set; }

        protected MarginPadding DefaultMinValue => new MarginPadding(float.MinValue);
        protected MarginPadding DefaultMaxValue => new MarginPadding(float.MaxValue);

        public override MarginPadding Value
        {
            get => base.Value;
            set => base.Value = clamp(value, MinValue, MaxValue);
        }

        public override void BindTo(Bindable<MarginPadding> them)
        {
            if (them is BindableMarginPadding other)
            {
                MinValue = new MarginPadding
                {
                    Top = Math.Max(MinValue.Top, other.MinValue.Top),
                    Left = Math.Max(MinValue.Left, other.MinValue.Left),
                    Bottom = Math.Max(MinValue.Bottom, other.MinValue.Bottom),
                    Right = Math.Max(MinValue.Right, other.MinValue.Right)
                };

                MaxValue = new MarginPadding
                {
                    Top = Math.Min(MaxValue.Top, other.MaxValue.Top),
                    Left = Math.Min(MaxValue.Left, other.MaxValue.Left),
                    Bottom = Math.Min(MaxValue.Bottom, other.MaxValue.Bottom),
                    Right = Math.Min(MaxValue.Right, other.MaxValue.Right)
                };

                if (MinValue.Top > MaxValue.Top || MinValue.Left > MaxValue.Left || MinValue.Bottom > MaxValue.Bottom || MinValue.Right > MaxValue.Right)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(them),
                        $"Can not weld BindableMarginPaddings with non-overlapping min/max-ranges. The ranges were [{MinValue} - {MaxValue}] and [{other.MinValue} - {other.MaxValue}]."
                    );
                }
            }

            base.BindTo(them);
        }

        public override string ToString() => Value.ToString();

        public override void Parse(object input)
        {
            switch (input)
            {
                case string str:
                    string[] split = str.Trim("() ".ToCharArray()).Split(',');

                    if (split.Length != 4)
                        throw new ArgumentException($"Input string was in wrong format! (expected: '(<top>, <left>, <bottom>, <right>)', actual: '{str}')");

                    Value = new MarginPadding
                    {
                        Top = float.Parse(split[0], CultureInfo.InvariantCulture),
                        Left = float.Parse(split[1], CultureInfo.InvariantCulture),
                        Bottom = float.Parse(split[2], CultureInfo.InvariantCulture),
                        Right = float.Parse(split[3], CultureInfo.InvariantCulture),
                    };
                    break;

                default:
                    base.Parse(input);
                    break;
            }
        }

        private static MarginPadding clamp(MarginPadding value, MarginPadding minValue, MarginPadding maxValue) =>
            new MarginPadding
            {
                Top = Math.Max(minValue.Top, Math.Min(maxValue.Top, value.Top)),
                Left = Math.Max(minValue.Left, Math.Min(maxValue.Left, value.Left)),
                Bottom = Math.Max(minValue.Bottom, Math.Min(maxValue.Bottom, value.Bottom)),
                Right = Math.Max(minValue.Right, Math.Min(maxValue.Right, value.Right))
            };
    }
}
