// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Tests.Bindables;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneTestWithValues : FrameworkTestScene
    {
        [Test]
        public void TestValues([Values] BindableEnumTest.TestEnum vals1, [Values] BindableEnumTest.TestEnum vals2)
        {
        }
    }
}
