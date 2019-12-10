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

        [TestCaseSource(nameof(typeSource))]
        public void TestConstruction(Type type)
        {
            Assert.That(createBindable(type), Is.Not.Null);
        }

        [TestCaseSource(nameof(typeSource))]
        public void TestSetValue(Type type)
        {
            object expectedValue = Convert.ChangeType(1, type);

            object bindable = createBindable(type);

            MethodInfo setMethod = bindable.GetType().GetMethod(nameof(BindableNumber<int>.Set), BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(type);
            setMethod.Invoke(bindable, new[] { expectedValue });

            PropertyInfo valueProperty = bindable.GetType().GetProperty(nameof(BindableNumber<int>.Value), BindingFlags.Public | BindingFlags.Instance);
            object value = valueProperty.GetValue(bindable);

            Assert.That(Convert.ChangeType(value, typeof(int)), Is.EqualTo(expectedValue));
        }

        [TestCaseSource(nameof(typeSource))]
        public void TestAddValue(Type type)
        {
            object expectedValue = Convert.ChangeType(1, type);

            object bindable = createBindable(type);

            MethodInfo addMethod = bindable.GetType().GetMethod(nameof(BindableNumber<int>.Add), BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(type);
            addMethod.Invoke(bindable, new[] { expectedValue });

            PropertyInfo valueProperty = bindable.GetType().GetProperty(nameof(BindableNumber<int>.Value), BindingFlags.Public | BindingFlags.Instance);
            object value = valueProperty.GetValue(bindable);

            Assert.That(Convert.ChangeType(value, typeof(int)), Is.EqualTo(expectedValue));
        }

        private object createBindable(Type type) => Activator.CreateInstance(typeof(BindableNumber<>).MakeGenericType(type), new[] { Convert.ChangeType(0, type) });
    }
}
