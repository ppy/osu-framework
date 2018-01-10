// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseBindableNumbers : TestCase
    {
        private readonly BindableInt bindableInt = new BindableInt();
        private readonly BindableLong bindableLong = new BindableLong();
        private readonly BindableDouble bindableDouble = new BindableDouble();
        private readonly BindableFloat bindableFloat = new BindableFloat();

        public TestCaseBindableNumbers()
        {
            AddStep("Reset", () =>
            {
                setValue(0);
                setPrecision(1);
            });

            AddStep("Value = 10", () => setValue(10));
            AddAssert("Check = 10", () => checkExact(10));

            AddStep("Precision = 3", () => setPrecision(3));
            AddStep("Value = 4", () => setValue(3));
            AddAssert("Check = 3", () => checkExact(3));
            AddStep("Value = 5", () => setValue(5));
            AddAssert("Check = 6", () => checkExact(6));
            AddStep("Value = 59", () => setValue(59));
            AddAssert("Check 60", () => checkExact(60));

            AddStep("Precision = 10", () => setPrecision(10));
            AddStep("Value = 6", () => setValue(6));
            AddAssert("Check = 10", () => checkExact(10));

            AddSliderStep("Value", -1000.0, 1000.0, 0.0, setValue);
            AddSliderStep("Precision", 1, 10, 1, setPrecision);

            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                Content = new[]
                {
                    new Drawable[]
                    {
                        new BindableDisplayContainer<int>(bindableInt),
                        new BindableDisplayContainer<long>(bindableLong),
                    },
                    new Drawable[]
                    {
                        new BindableDisplayContainer<float>(bindableFloat),
                        new BindableDisplayContainer<double>(bindableDouble),
                    }
                }
            };
        }

        private bool checkExact(decimal value)
            => bindableInt.Value == value && bindableLong.Value == value
            && bindableFloat.Value.ToString(CultureInfo.InvariantCulture) == value.ToString(CultureInfo.InvariantCulture)
            && bindableDouble.Value.ToString(CultureInfo.InvariantCulture) == value.ToString(CultureInfo.InvariantCulture);

        private void setValue<T>(T value)
        {
            bindableInt.Value = Convert.ToInt32(value);
            bindableLong.Value = Convert.ToInt64(value);
            bindableDouble.Value = Convert.ToDouble(value);
            bindableFloat.Value = Convert.ToSingle(value);
        }

        private void setPrecision<T>(T precision)
        {
            bindableInt.Precision = Convert.ToInt32(precision);
            bindableLong.Precision = Convert.ToInt64(precision);
            bindableDouble.Precision = Convert.ToDouble(precision);
            bindableFloat.Precision = Convert.ToSingle(precision);
        }

        private class BindableDisplayContainer<T> : CompositeDrawable
            where T : struct
        {
            public BindableDisplayContainer(BindableNumber<T> bindable)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                SpriteText valueText;
                InternalChild = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new SpriteText { Text = $"{typeof(T).Name} value:" },
                        valueText = new SpriteText { Text = bindable.Value.ToString() }
                    }
                };

                bindable.ValueChanged += v => valueText.Text = v.ToString();
            }
        }
    }
}
