// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("for manual testing")]
    public class TestSceneBlockExiting : FrameworkTestScene
    {
        private Bindable<bool> blockingClose;

        [BackgroundDependencyLoader]
        private void load(TestGame game)
        {
            blockingClose = game.BlockExit.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddToggleStep("Block closing window", b => blockingClose.Value = b);
        }
    }
}
