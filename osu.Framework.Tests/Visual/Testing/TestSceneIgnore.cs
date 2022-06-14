// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneIgnore : FrameworkTestScene
    {
        [Test]
        [Ignore("test")]
        public void IgnoredTest()
        {
            AddAssert("Test ignored", () => false);
        }

        [TestCase(1)]
        [TestCase(2)]
        [Ignore("test")]
        public void IgnoredParameterizedTest(int test)
        {
            AddAssert("Test ignored", () => false);
        }
    }
}
