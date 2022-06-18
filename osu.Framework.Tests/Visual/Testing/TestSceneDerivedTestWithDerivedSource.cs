// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneDerivedTestWithDerivedSource : TestSceneTestWithSource
    {
        [TestCaseSource(nameof(SourceField))]
        public void TestDerivedSourceField(int a, int b)
        {
        }

        [TestCaseSource(nameof(SourceProperty))]
        public void TestDerivedSourceProperty(int a, int b)
        {
        }

        [TestCaseSource(nameof(SourceMethod))]
        public void TestDerivedSourceMethod(int a, int b)
        {
        }
    }
}
