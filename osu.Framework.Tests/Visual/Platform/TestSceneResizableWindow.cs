// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneResizableWindow : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            if (host.Window == null)
            {
                Assert.Ignore("This test cannot be run in headless mode (a window instance is required).");
                return;
            }

            AddToggleStep("toggle resizable", state => ((SDL2DesktopWindow)host.Window).Resizable = state);
        }
    }
}
