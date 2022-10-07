// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneCountingText : FrameworkTestScene
    {
        private readonly Bindable<CountType> countType = new Bindable<CountType>();

        public TestSceneCountingText()
        {
            Counter counter;

            BasicDropdown<CountType> typeDropdown;
            Children = new Drawable[]
            {
                typeDropdown = new BasicDropdown<CountType>
                {
                    Position = new Vector2(10),
                    Width = 150,
                },
                counter = new TestTextCounter(createResult)
                {
                    Position = new Vector2(180)
                }
            };

            typeDropdown.Items = (CountType[])Enum.GetValues(typeof(CountType));
            countType.BindTo(typeDropdown.Current);
            countType.ValueChanged += _ => beginStep(lastStep)();

            AddStep("1 -> 4 | 1 sec", beginStep(() => counter.CountTo(1).CountTo(4, 1000)));
            AddStep("1 -> 4 | 3 sec", beginStep(() => counter.CountTo(1).CountTo(4, 3000)));
            AddStep("4 -> 1 | 1 sec", beginStep(() => counter.CountTo(4).CountTo(1, 1000)));
            AddStep("4 -> 1 | 3 sec", beginStep(() => counter.CountTo(4).CountTo(1, 3000)));
            AddStep("1 -> 4 -> 1 | 6 sec", beginStep(() => counter.CountTo(1).CountTo(4, 3000).Then().CountTo(1, 3000)));
            AddStep("1 -> 4 -> 1 | 2 sec", beginStep(() => counter.CountTo(1).CountTo(4, 1000).Then().CountTo(1, 1000)));
            AddStep("1 -> 100 | 5 sec | OutQuint", beginStep(() => counter.CountTo(1).CountTo(100, 5000, Easing.OutQuint)));
        }

        private Action lastStep;

        private Action beginStep(Action stepAction) => () =>
        {
            lastStep = stepAction;
            stepAction?.Invoke();
        };

        private string createResult(double value)
        {
            switch (countType.Value)
            {
                default:
                case CountType.AsDouble:
                    return value.ToString(CultureInfo.InvariantCulture);

                case CountType.AsInteger:
                    return ((int)value).ToString();

                case CountType.AsIntegerCeiling:
                    return ((int)Math.Ceiling(value)).ToString();

                case CountType.AsDouble2:
                    return Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);

                case CountType.AsDouble4:
                    return Math.Round(value, 4).ToString(CultureInfo.InvariantCulture);
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
            AddInternal(text = new SpriteText { Font = new FontUsage(size: 24) });
        }

        protected override void OnCountChanged(double count) => text.Text = resultFunction(count);
    }
}
