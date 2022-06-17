// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public abstract class FrameworkTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new FrameworkTestSceneTestRunner();
    }
}
