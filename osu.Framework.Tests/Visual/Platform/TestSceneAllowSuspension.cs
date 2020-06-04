// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneAllowSuspension : FrameworkTestScene
    {
        private Bindable<bool> allowSuspension;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            allowSuspension = host.AllowScreenSuspension.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddToggleStep("Toggle Suspension", b => allowSuspension.Value = b);
        }
    }
}
