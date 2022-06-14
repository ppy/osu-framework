// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneDerivedTestWithDerivedMethodsWithAttributes : TestSceneTest
    {
        [SetUp]
        public override void SetUp() => base.SetUp();

        [SetUpSteps]
        public override void SetUpSteps() => base.SetUpSteps();

        [TearDownSteps]
        public override void TearDownSteps() => base.TearDownSteps();
    }
}
