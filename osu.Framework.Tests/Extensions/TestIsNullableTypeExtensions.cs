// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Extensions.TypeExtensions;

#pragma warning disable CS8618
#pragma warning disable CS0649

namespace osu.Framework.Tests.Extensions
{
    [TestFixture]
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    [SuppressMessage("ReSharper", "ValueParameterNotUsed")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public class TestIsNullableTypeExtensions
    {
        private const BindingFlags binding_flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private int nonNullValueField;
        private int? nullableValueField;

        private int nonNullValueGetSetProperty { get; set; }
        private int? nullableValueGetSetProperty { get; set; }

        private int nonNullValueGetProperty { get; }
        private int? nullableValueGetProperty { get; }

        private int nonNullValueSetProperty { set { } }
        private int? nullableValueSetProperty { set { } }

        private object nonNullReferenceField;
        private object? nullableReferenceField;

        private object nonNullReferenceGetSetProperty { get; set; }
        private object? nullableReferenceGetSetProperty { get; set; }

        private object nonNullReferenceGetProperty { get; }
        private object? nullableReferenceGetProperty { get; }

        private object nonNullReferenceSetProperty { set { } }
        private object? nullableReferenceSetProperty { set { } }

#nullable disable
        private object nonNullReferenceFieldWithoutNullableReferenceTypes;
#nullable enable

        private event Action nonNullEvent;
        private event Action? nullableEvent;

        private void testValueParamMethod(int param1, int? param2) { }
        private void testReferenceParamMethod(object param1, object? param2) { }

        [Test]
        public void TestNonNullValueField() => Assert.False(GetType().GetField(nameof(nonNullValueField), binding_flags).IsNullable());

        [Test]
        public void TestNullableValueField() => Assert.True(GetType().GetField(nameof(nullableValueField), binding_flags).IsNullable());

        [Test]
        public void TestNonNullValueGetSetProperty() => Assert.False(GetType().GetProperty(nameof(nonNullValueGetSetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNullableValueGetSetProperty() => Assert.True(GetType().GetProperty(nameof(nullableValueGetSetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNonNullValueGetProperty() => Assert.False(GetType().GetProperty(nameof(nonNullValueGetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNullableValueGetProperty() => Assert.True(GetType().GetProperty(nameof(nullableValueGetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNonNullValueSetProperty() => Assert.False(GetType().GetProperty(nameof(nonNullValueSetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNullableValueSetProperty() => Assert.True(GetType().GetProperty(nameof(nullableValueSetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNonNullReferenceField() => Assert.False(GetType().GetField(nameof(nonNullReferenceField), binding_flags).IsNullable());

        [Test]
        public void TestNullableReferenceField() => Assert.True(GetType().GetField(nameof(nullableReferenceField), binding_flags).IsNullable());

        [Test]
        public void TestNonNullReferenceGetSetProperty() => Assert.False(GetType().GetProperty(nameof(nonNullReferenceGetSetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNullableReferenceGetSetProperty() => Assert.True(GetType().GetProperty(nameof(nullableReferenceGetSetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNonNullReferenceGetProperty() => Assert.False(GetType().GetProperty(nameof(nonNullReferenceGetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNullableReferenceGetProperty() => Assert.True(GetType().GetProperty(nameof(nullableReferenceGetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNonNullReferenceSetProperty() => Assert.False(GetType().GetProperty(nameof(nonNullReferenceSetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNullableReferenceSetProperty() => Assert.True(GetType().GetProperty(nameof(nullableReferenceSetProperty), binding_flags).IsNullable());

        [Test]
        public void TestNonNullReferenceFieldWithoutNullableReferenceTypes()
            => Assert.False(GetType().GetField(nameof(nonNullReferenceFieldWithoutNullableReferenceTypes), binding_flags).IsNullable());

        [Test]
        public void TestNonNullEvent() => Assert.False(GetType().GetEvent(nameof(nonNullEvent), binding_flags).IsNullable());

        [Test]
        public void TestNullableEvent() => Assert.True(GetType().GetEvent(nameof(nullableEvent), binding_flags).IsNullable());

        [Test]
        public void TestValueParameters()
        {
            var parameters = GetType().GetMethod(nameof(testValueParamMethod), binding_flags)!.GetParameters();
            Assert.False(parameters[0].IsNullable());
            Assert.True(parameters[1].IsNullable());
        }

        [Test]
        public void TestReferenceParameters()
        {
            var parameters = GetType().GetMethod(nameof(testReferenceParamMethod), binding_flags)!.GetParameters();
            Assert.False(parameters[0].IsNullable());
            Assert.True(parameters[1].IsNullable());
        }

        [Test]
        public void TestNonNullValueType() => Assert.False(typeof(int).IsNullable());

        [Test]
        public void TestNullableValueType() => Assert.True(typeof(int?).IsNullable());

        [Test]
        public void TestNonNullReferenceType() => Assert.False(typeof(object).IsNullable());

        // typeof cannot be used on "object?".
    }
}
