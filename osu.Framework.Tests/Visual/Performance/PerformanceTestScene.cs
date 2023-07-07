// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Tests.Visual.Performance
{
    public abstract partial class PerformanceTestScene : FrameworkTestScene
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("General");
            AddToggleStep("hide content", v => Content.Alpha = v ? 0 : 1);
        }
    }
}
