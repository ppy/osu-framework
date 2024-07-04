// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        /// This test exercises that when sitting a decimal <see cref="BindableNumber{T}.Precision"/> like 0.1 on a <c>BindableNumber&lt;float&gt;</c>,
        /// the resulting <see cref="BindableNumber{T}.Value"/> is the closest to the actual closest <c>float</c> number
        /// to the real-world decimal.
        /// This matters because certain methods of rounding to the precision specified on the <see cref="BindableNumber{T}"/>
        /// cause rounding errors that show up elsewhere in undesirable ways.
        /// </summary>
        [Test]
        public void TestDecimalPrecision()
        {
            var bindable = new BindableNumber<float>
            {
                Precision = 0.1f,
                Value = 5.2f
            };
            Assert.That(bindable.Value, Is.EqualTo(5.2f));

            bindable.Value = 4.3f;
            Assert.That(bindable.Value, Is.EqualTo(4.3f));

            bindable.Precision = 0.01f;
            Assert.That(bindable.Value, Is.EqualTo(4.3f));

            bindable.Value = 9.87f;
            Assert.That(bindable.Value, Is.EqualTo(9.87f));
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

        private object createBindable(Type type) => Activator.CreateInstance(typeof(BindableNumber<>).MakeGenericType(type), Convert.ChangeType(0, type));
    }
}
