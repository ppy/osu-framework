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
            Add(counter = new TestTextCounter(createResult));

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

            AddStep("1 -> 100 | 5 sec | OutQuint", () => counter.CountTo(1).CountTo(100, 5000, Easing.OutQuint));
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

    public class TestTextCounter : Counter
    {
        private readonly Func<double, string> resultFunction;
        private readonly SpriteText text;

        public TestTextCounter(Func<double, string> resultFunction)
        {
            this.resultFunction = resultFunction;
            AddInternal(text = new SpriteText { TextSize = 24 });
        }

        protected override void OnCountChanged() => text.Text = resultFunction(Count);
    }
}
