// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneTestWithSource : FrameworkTestScene
    {
        protected static object[][] SourceField =
        {
            new object[] { 1, 2 },
            new object[] { 3, 4 },
            new object[] { 5, 6 },
            new object[] { 7, 8 },
        };

        protected static object[][] SourceProperty => SourceField;

        protected static object[][] SourceMethod() => SourceField;

        protected static object[][] SourceMethodWithParameters(int x, int y) => new[]
        {
            new object[] { x + 1, y + 2 },
            new object[] { x + 3, y + 4 },
            new object[] { x + 5, y + 6 },
            new object[] { x + 7, y + 8 },
        };

        protected static object[] SingleParameterSource = { 1, 2, 3, 4 };

        protected static object[][] DifferentTypesSource =
        {
            new object[] { Visibility.Visible, FillDirection.Horizontal, false },
            new object[] { Visibility.Hidden, FillDirection.Vertical, true },
            new object[] { Visibility.Hidden, FillDirection.Full, false },
        };

        public static object[][] ExposedSourceField = SourceField;

        public static object[][] ExposedSourceProperty => SourceProperty;

        public static object[][] ExposedSourceMethod() => SourceMethod();

        [TestCaseSource(nameof(SourceField))]
        public void TestSourceField(int a, int b)
        {
        }

        [TestCaseSource(nameof(SourceProperty))]
        public void TestSourceProperty(int a, int b)
        {
        }

        [TestCaseSource(nameof(SourceMethod))]
        public void TestSourceMethod(int a, int b)
        {
        }

        [TestCaseSource(nameof(SourceMethodWithParameters), new object[] { 10, 20 })]
        public void TestSourceParamsMethod(int a, int b)
        {
        }

        [TestCaseSource(nameof(SourceField))]
        [TestCaseSource(nameof(SourceProperty))]
        [TestCaseSource(nameof(SourceMethod))]
        [TestCaseSource(nameof(SourceMethodWithParameters), new object[] { 10, 20 })]
        public void TestMultipleSources(int a, int b)
        {
        }

        [TestCaseSource(nameof(SingleParameterSource))]
        public void TestSingleParameterSource(int x)
        {
        }

        [TestCaseSource(nameof(DifferentTypesSource))]
        public void TestDifferentTypesSource(Visibility a, FillDirection b, bool c)
        {
        }

        [TestCaseSource(typeof(TestEnumerable))]
        public void TestCustomEnumerableSource(int a, int b)
        {
        }

        private class TestEnumerable : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                yield return new[] { 1, 2 };
                yield return new[] { 3, 4 };
                yield return new[] { 5, 6 };
                yield return new[] { 7, 8 };
            }
        }
    }
}
