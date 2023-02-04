// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot be run in headless mode (a window instance is required).")]
    public partial class TestSceneRaiseWindow : FrameworkTestScene
    {
        private IWindow window = null!;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            window = host.Window!;
            AddStep("nothing", () => { }); // so the test doesn't switch to windowed on startup.
        }

        [TestCase(WindowMode.Windowed)]
        [TestCase(WindowMode.Borderless)]
        [TestCase(WindowMode.Fullscreen)]
        public void TestRaise(WindowMode windowMode)
        {
            AddStep($"set mode to {windowMode}", () => window.WindowMode.Value = windowMode);
            AddUntilStep("wait for window to lose focus", () => window.IsActive.Value, () => Is.False);
            AddWaitStep("do nothing to give the WM breathing room", 5);
            AddStep("raise window", () => window.Raise());
            AddAssert("window has focus", () => window.IsActive.Value, () => Is.True);
        }
    }
}
