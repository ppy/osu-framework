// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseCountingText : TestCase
    {
        private CountType countType;

        public TestCaseCountingText()
        {
            Counter counter;
            Add(counter = new Counter(() => new SpriteText { TextSize = 36 }, createResult));

            AddStep("Integer", () => countType = CountType.AsInteger);
            AddStep("Ceiled-integer", () => countType = CountType.AsIntegerCeiling);
            AddStep("Unrounded", () => countType = CountType.AsDouble);
            AddStep("2 d.p. rounded", () => countType = CountType.AsDouble2);
            AddStep("4 d.p. rounded", () => countType = CountType.AsDouble4);

            AddStep("1 -> 4 | 1 sec", () => counter.CountTo(1).CountTo(4, 1000));
            AddStep("1 -> 4 | 3 sec", () => counter.CountTo(1).CountTo(4, 3000));
            AddStep("4 -> 1 | 1 sec", () => counter.CountTo(4).CountTo(1, 1000));
            AddStep("4 -> 1 | 3 sec", () => counter.CountTo(4).CountTo(1, 3000));
            AddStep("1 -> 4 -> 1 | 6 sec", () => counter.CountTo(1).CountTo(4, 3000).Then().CountTo(1, 3000));
            AddStep("1 -> 4 -> 1 | 2 sec", () => counter.CountTo(1).CountTo(4, 1000).Then().CountTo(1, 1000));
        }

        private string createResult(double value)
        {
            switch (countType)
            {
                default:
                case CountType.AsDouble:
                    return value.ToString();
                case CountType.AsInteger:
                    return ((int)value).ToString();
                case CountType.AsIntegerCeiling:
                    return ((int)Math.Ceiling(value)).ToString();
                case CountType.AsDouble2:
                    return Math.Round(value, 2).ToString();
                case CountType.AsDouble4:
                    return Math.Round(value, 4).ToString();
            }
        }

        private enum CountType
        {
            AsInteger,
            AsIntegerCeiling,
            AsDouble,
            AsDouble2,
            AsDouble4,
        }
    }

    public class Counter : CompositeDrawable, IHasCurrentValue<double>
    {
        public Bindable<double> Current { get; } = new Bindable<double>();

        private readonly Func<double, string> resultFunction;
        private readonly SpriteText text;

        public Counter(Func<SpriteText> creationFunction, Func<double, string> resultFunction = null)
        {
            this.resultFunction = resultFunction ?? new Func<double, string>(v => v.ToString("n2"));

            text = creationFunction?.Invoke() ?? new SpriteText();
            AddInternal(text);

            Current.ValueChanged += currentValueChanged;
        }

        private void currentValueChanged(double newValue) => text.Text = resultFunction(newValue);

        public TransformSequence<Counter> CountTo(double endCount, double duration = 0, Easing easing = Easing.None)
            => this.TransformTo(this.PopulateTransform(new TransformCount(), endCount, duration, easing));

        private class TransformCount : Transform<double, Counter>
        {
            public override string TargetMember => "Current.Value";
            protected override void Apply(Counter d, double time) => d.Current.Value = Interpolation.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
            protected override void ReadIntoStartValue(Counter d) => StartValue = d.Current;
        }
    }

    public static class CounterTransformSequenceExtensions
    {
        public static TransformSequence<Counter> CountTo(this TransformSequence<Counter> t, double endCount, double duration = 0, Easing easing = Easing.None)
            => t.Append(o => o.CountTo(endCount, duration, easing));
    }
}
