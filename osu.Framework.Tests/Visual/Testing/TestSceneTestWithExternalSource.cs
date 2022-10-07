// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneTestWithExternalSource : FrameworkTestScene
    {
        [TestCaseSource(typeof(TestSceneTestWithSource), nameof(TestSceneTestWithSource.ExposedSourceField))]
        public void TestExposedSourceField(int a, int b)
        {
        }

        [TestCaseSource(typeof(TestSceneTestWithSource), nameof(TestSceneTestWithSource.ExposedSourceProperty))]
        public void TestExposedSourceProperty(int a, int b)
        {
        }

        [TestCaseSource(typeof(TestSceneTestWithSource), nameof(TestSceneTestWithSource.ExposedSourceMethod))]
        public void TestExposedSourceMethod(int a, int b)
        {
        }
    }
}
