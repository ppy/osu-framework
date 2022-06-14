// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class RangeConstrainedBindableTest
    {
        /// <summary>
        /// Tests that the value of a bindable is updated when the maximum value is changed.
        /// </summary>
        [Test]
        public void TestValueUpdatedOnMaxValueChange()
        {
            var bindable = new BindableInt(2)
            {
                MaxValue = 1
            };

            Assert.That(bindable.Value, Is.EqualTo(1));
        }

        /// <summary>
        /// Tests that the order of maximum value vs value changed events follows the order:
        /// MaxValue (bound) -> MaxValue -> Value (bound) -> Value
        /// </summary>
        [Test]
        public void TestValueChangeEventOccursAfterMaxValueChangeEvent()
        {
            var bindable1 = new BindableInt(2);
            var bindable2 = new BindableInt { BindTarget = bindable1 };
            int counter = 0, bindable1ValueChange = 0, bindable1MaxChange = 0, bindable2ValueChange = 0, bindable2MaxChange = 0;
            bindable1.ValueChanged += _ => bindable1ValueChange = ++counter;
            bindable1.MaxValueChanged += _ => bindable1MaxChange = ++counter;
            bindable2.ValueChanged += _ => bindable2ValueChange = ++counter;
            bindable2.MaxValueChanged += _ => bindable2MaxChange = ++counter;

            bindable1.MaxValue = 1;

            Assert.That(bindable2MaxChange, Is.EqualTo(1));
            Assert.That(bindable1MaxChange, Is.EqualTo(2));
            Assert.That(bindable2ValueChange, Is.EqualTo(3));
            Assert.That(bindable1ValueChange, Is.EqualTo(4));
        }

        [Test]
        public void TestDefaultMaxValueAppliedInConstructor()
        {
            var bindable = new BindableNumberWithDefaultMaxValue(2);

            Assert.That(bindable.Value, Is.EqualTo(1));
        }

        /// <summary>
        /// Tests that the value of a bindable is updated when the minimum value is changed.
        /// </summary>
        [Test]
        public void TestValueUpdatedOnMinValueChange()
        {
            var bindable = new BindableInt(2)
            {
                MinValue = 3
            };

            Assert.That(bindable.Value, Is.EqualTo(3));
        }

        /// <summary>
        /// Tests that the order of minimum value vs value changed events follows the order:
        /// MinValue (bound) -> MinValue -> Value (bound) -> Value
        /// </summary>
        [Test]
        public void TestValueChangeEventOccursAfterMinValueChangeEvent()
        {
            var bindable1 = new BindableInt(2);
            var bindable2 = new BindableInt { BindTarget = bindable1 };
            int counter = 0, bindable1ValueChange = 0, bindable1MinChange = 0, bindable2ValueChange = 0, bindable2MinChange = 0;
            bindable1.ValueChanged += _ => bindable1ValueChange = ++counter;
            bindable1.MinValueChanged += _ => bindable1MinChange = ++counter;
            bindable2.ValueChanged += _ => bindable2ValueChange = ++counter;
            bindable2.MinValueChanged += _ => bindable2MinChange = ++counter;

            bindable1.MinValue = 3;

            Assert.That(bindable2MinChange, Is.EqualTo(1));
            Assert.That(bindable1MinChange, Is.EqualTo(2));
            Assert.That(bindable2ValueChange, Is.EqualTo(3));
            Assert.That(bindable1ValueChange, Is.EqualTo(4));
        }

        [Test]
        public void TestDefaultMinValueAppliedInConstructor()
        {
            var bindable = new BindableNumberWithDefaultMinValue(2);

            Assert.That(bindable.Value, Is.EqualTo(3));
        }

        private class BindableNumberWithDefaultMaxValue : BindableInt
        {
            public BindableNumberWithDefaultMaxValue(int value = 0)
                : base(value)
            {
            }

            protected override int DefaultMaxValue => 1;

            protected override Bindable<int> CreateInstance() => new BindableNumberWithDefaultMaxValue();
        }

        private class BindableNumberWithDefaultMinValue : BindableInt
        {
            public BindableNumberWithDefaultMinValue(int value = 0)
                : base(value)
            {
            }

            protected override int DefaultMinValue => 3;

            protected override Bindable<int> CreateInstance() => new BindableNumberWithDefaultMinValue();
        }
    }
}
