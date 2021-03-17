// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot be run in headless mode (a window instance is required).")]
    public class TestSceneResizableWindow : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            AddToggleStep("toggle resizable", state => ((SDL2DesktopWindow)host.Window).Resizable = state);
        }
    }
}
