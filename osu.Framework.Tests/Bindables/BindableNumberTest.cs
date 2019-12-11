// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableNumberTest
    {
        private static IEnumerable<Type> typeSource = new[]
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double)
        };

        /// <summary>
        /// Tests that all supported <see cref="BindableNumber{T}"/> types are constructible.
        /// </summary>
        [TestCaseSource(nameof(typeSource))]
        public void TestConstruction(Type type)
        {
            Assert.That(createBindable(type), Is.Not.Null);
        }

        /// <summary>
        /// Tests that value can be set on all supported <see cref="BindableNumber{T}"/> types.
        /// </summary>
        [TestCaseSource(nameof(typeSource))]
        public void TestSetValue(Type type)
        {
            object expectedValue = Convert.ChangeType(1, type);

            object bindable = createBindable(type);

            MethodInfo setMethod = bindable.GetType().GetMethod(nameof(BindableNumber<int>.Set), BindingFlags.Public | BindingFlags.Instance)?.MakeGenericMethod(type);
            setMethod?.Invoke(bindable, new[] { expectedValue });

            PropertyInfo valueProperty = bindable.GetType().GetProperty(nameof(BindableNumber<int>.Value), BindingFlags.Public | BindingFlags.Instance);
            object value = valueProperty?.GetValue(bindable);

            Assert.That(Convert.ChangeType(value, typeof(int)), Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Tests that value can be added to on all supported <see cref="BindableNumber{T}"/> types.
        /// </summary>
        [TestCaseSource(nameof(typeSource))]
        public void TestAddValue(Type type)
        {
            object expectedValue = Convert.ChangeType(1, type);

            object bindable = createBindable(type);

            MethodInfo addMethod = bindable.GetType().GetMethod(nameof(BindableNumber<int>.Add), BindingFlags.Public | BindingFlags.Instance)?.MakeGenericMethod(type);
            addMethod?.Invoke(bindable, new[] { expectedValue });

            PropertyInfo valueProperty = bindable.GetType().GetProperty(nameof(BindableNumber<int>.Value), BindingFlags.Public | BindingFlags.Instance);
            object value = valueProperty?.GetValue(bindable);

            Assert.That(Convert.ChangeType(value, typeof(int)), Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Tests that the value of a bindable is updated when the precision is changed.
        /// </summary>
        [Test]
        public void TestValueUpdatedOnPrecisionChange()
        {
            var bindable = new BindableInt(2)
            {
                Precision = 3
            };

            Assert.That(bindable.Value, Is.EqualTo(3));
        }

        /// <summary>
        /// Tests that the order of precision vs value changed events follows the order:
        /// Precision (bound) -> Precision -> Value (bound) -> Value
        /// </summary>
        [Test]
        public void TestValueChangeEventOccursAfterPrecisionChangeEvent()
        {
            var bindable1 = new BindableInt(2);
            var bindable2 = new BindableInt { BindTarget = bindable1 };
            int counter = 0, bindable1ValueChange = 0, bindable1PrecisionChange = 0, bindable2ValueChange = 0, bindable2PrecisionChange = 0;
            bindable1.ValueChanged += _ => bindable1ValueChange = ++counter;
            bindable1.PrecisionChanged += _ => bindable1PrecisionChange = ++counter;
            bindable2.ValueChanged += _ => bindable2ValueChange = ++counter;
            bindable2.PrecisionChanged += _ => bindable2PrecisionChange = ++counter;

            bindable1.Precision = 3;

            Assert.That(bindable2PrecisionChange, Is.EqualTo(1));
            Assert.That(bindable1PrecisionChange, Is.EqualTo(2));
            Assert.That(bindable2ValueChange, Is.EqualTo(3));
            Assert.That(bindable1ValueChange, Is.EqualTo(4));
        }

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

        private object createBindable(Type type) => Activator.CreateInstance(typeof(BindableNumber<>).MakeGenericType(type), Convert.ChangeType(0, type));

        private class BindableNumberWithDefaultMaxValue : BindableInt
        {
            public BindableNumberWithDefaultMaxValue(int value = 0)
                : base(value)
            {
            }

            protected override int DefaultMaxValue => 1;
        }

        private class BindableNumberWithDefaultMinValue : BindableInt
        {
            public BindableNumberWithDefaultMinValue(int value = 0)
                : base(value)
            {
            }

            protected override int DefaultMinValue => 3;
        }
    }
}
