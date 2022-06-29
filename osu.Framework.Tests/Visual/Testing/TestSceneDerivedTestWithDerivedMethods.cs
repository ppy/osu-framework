// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneDerivedTestWithDerivedMethods : TestSceneTest
    {
        // ReSharper disable once RedundantOverriddenMember
        public override void SetUp() => base.SetUp();

        // ReSharper disable once RedundantOverriddenMember
        public override void SetUpSteps() => base.SetUpSteps();

        // ReSharper disable once RedundantOverriddenMember
        public override void TearDownSteps() => base.TearDownSteps();
    }
}
