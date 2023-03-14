// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public abstract partial class FrameworkGridTestScene : GridTestScene
    {
        protected FrameworkGridTestScene(int rows, int cols)
            : base(rows, cols)
        {
        }

        protected override ITestSceneTestRunner CreateRunner() => new FrameworkTestSceneTestRunner();
    }
}
