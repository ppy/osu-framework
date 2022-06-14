// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Tests.Visual.Bindables
{
    public class TestSceneBindableNumbers : FrameworkTestScene
    {
        private readonly BindableInt bindableInt = new BindableInt();
        private readonly BindableLong bindableLong = new BindableLong();
        private readonly BindableDouble bindableDouble = new BindableDouble();
        private readonly BindableFloat bindableFloat = new BindableFloat();

        public TestSceneBindableNumbers()
        {
            AddStep("Reset", () =>
            {
                setValue(0);
                setPrecision(1);
            });

            testBasic();
            testPrecision3();
            testPrecision10();
            testMinMaxWithoutPrecision();
            testMinMaxWithPrecision();
            testInvalidPrecision();
            testFractionalPrecision();

            AddSliderStep("Min value", -100, 100, -100, setMin);
            AddSliderStep("Max value", -100, 100, 100, setMax);
            AddSliderStep("Value", -100, 100, 0, setValue);
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

        /// <summary>
        /// Tests basic setting of values.
        /// </summary>
        private void testBasic()
        {
            AddStep("Value = 10", () => setValue(10));
            AddAssert("Check = 10", () => checkExact(10));
        }

        /// <summary>
        /// Tests that midpoint values are correctly rounded with a precision of 3.
        /// </summary>
        private void testPrecision3()
        {
            AddStep("Precision = 3", () => setPrecision(3));
            AddStep("Value = 4", () => setValue(3));
            AddAssert("Check = 3", () => checkExact(3));
            AddStep("Value = 5", () => setValue(5));
            AddAssert("Check = 6", () => checkExact(6));
            AddStep("Value = 59", () => setValue(59));
            AddAssert("Check = 60", () => checkExact(60));
        }

        /// <summary>
        /// Tests that midpoint values are correctly rounded with a precision of 10.
        /// </summary>
        private void testPrecision10()
        {
            AddStep("Precision = 10", () => setPrecision(10));
            AddStep("Value = 6", () => setValue(6));
            AddAssert("Check = 10", () => checkExact(10));
        }

        /// <summary>
        /// Tests that values are correctly clamped to min/max values.
        /// </summary>
        private void testMinMaxWithoutPrecision()
        {
            AddStep("Precision = 1", () => setPrecision(1));
            AddStep("Min = -30", () => setMin(-30));
            AddStep("Max = 30", () => setMax(30));
            AddStep("Value = -50", () => setValue(-50));
            AddAssert("Check = -30", () => checkExact(-30));
            AddStep("Value = 50", () => setValue(50));
            AddAssert("Check = 30", () => checkExact(30));
        }

        /// <summary>
        /// Tests that values are correctly clamped to min/max values when precision is involved.
        /// In this case, precision is preferred over min/max values.
        /// </summary>
        private void testMinMaxWithPrecision()
        {
            AddStep("Precision = 5", () => setPrecision(5));
            AddStep("Min = -27", () => setMin(-27));
            AddStep("Max = 27", () => setMax(27));
            AddStep("Value = -30", () => setValue(-30));
            AddAssert("Check = -25", () => checkExact(-25));
            AddStep("Value = 30", () => setValue(30));
            AddAssert("Check = 25", () => checkExact(25));
        }

        /// <summary>
        /// Tests that invalid precisions cause exceptions.
        /// </summary>
        private void testInvalidPrecision()
        {
            AddAssert("Precision = 0 throws", () =>
            {
                try
                {
                    setPrecision(0);
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            });

            AddAssert("Precision = -1 throws", () =>
            {
                try
                {
                    setPrecision(-1);
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            });
        }

        /// <summary>
        /// Tests that fractional precisions are obeyed.
        /// Note that int bindables are assigned int precisions/values, so their results will differ.
        /// </summary>
        private void testFractionalPrecision()
        {
            AddStep("Precision = 2.25/2", () => setPrecision(2.25));
            AddStep("Value = 3.3/3", () => setValue(3.3));
            AddAssert("Check = 2.25/4", () => checkExact(2.25m, 4));
            AddStep("Value = 4.17/4", () => setValue(4.17));
            AddAssert("Check = 4.5/4", () => checkExact(4.5m, 4));
        }

        private bool checkExact(decimal value) => checkExact(value, value);

        private bool checkExact(decimal floatValue, decimal intValue)
            => bindableInt.Value == Convert.ToInt32(intValue)
               && bindableLong.Value == Convert.ToInt64(intValue)
               && bindableFloat.Value == Convert.ToSingle(floatValue)
               && bindableDouble.Value == Convert.ToDouble(floatValue);

        private void setMin<T>(T value)
        {
            bindableInt.MinValue = Convert.ToInt32(value);
            bindableLong.MinValue = Convert.ToInt64(value);
            bindableFloat.MinValue = Convert.ToSingle(value);
            bindableDouble.MinValue = Convert.ToDouble(value);
        }

        private void setMax<T>(T value)
        {
            bindableInt.MaxValue = Convert.ToInt32(value);
            bindableLong.MaxValue = Convert.ToInt64(value);
            bindableFloat.MaxValue = Convert.ToSingle(value);
            bindableDouble.MaxValue = Convert.ToDouble(value);
        }

        private void setValue<T>(T value)
        {
            bindableInt.Value = Convert.ToInt32(value);
            bindableLong.Value = Convert.ToInt64(value);
            bindableFloat.Value = Convert.ToSingle(value);
            bindableDouble.Value = Convert.ToDouble(value);
        }

        private void setPrecision<T>(T precision)
        {
            bindableInt.Precision = Convert.ToInt32(precision);
            bindableLong.Precision = Convert.ToInt64(precision);
            bindableFloat.Precision = Convert.ToSingle(precision);
            bindableDouble.Precision = Convert.ToDouble(precision);
        }

        private class BindableDisplayContainer<T> : CompositeDrawable
            where T : struct, IComparable<T>, IConvertible, IEquatable<T>
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
                        valueText = new SpriteText { Text = bindable.Value.ToString(CultureInfo.InvariantCulture) }
                    }
                };

                bindable.ValueChanged += e => valueText.Text = e.NewValue.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
