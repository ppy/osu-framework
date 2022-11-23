// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot be run in headless mode (a window instance is required).")]
    public partial class TestSceneCurrentDisplay : FrameworkTestScene
    {
        private IWindow window = null!;

        public TestSceneCurrentDisplay()
        {
            Children = new Drawable[]
            {
                new WindowDisplaysPreview
                {
                    RelativeSizeAxes = Axes.Both
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            window = host.Window.AsNonNull();
        }

        [TestCase(WindowState.Normal)]
        [TestCase(WindowState.Maximised)]
        [TestCase(WindowState.Minimised)]
        [TestCase(WindowState.FullscreenBorderless)]
        [TestCase(WindowState.Fullscreen)]
        public void TestChangeCurrentDisplay(WindowState startingState)
        {
            Display? display = null;

            WindowMode startingMode = getWindowModeForState(startingState);

            // this shouldn't be necessary, but SDL2DesktopWindow doesn't set the config WindowMode when changing the WindowState only.
            AddStep($"switch to {startingMode}", () => window.WindowMode.Value = startingMode);

            AddStep($"switch to {startingState}", () => window.WindowState = startingState);
            AddStep("fetch a different display", () =>
            {
                int current = window.CurrentDisplayBindable.Value.Index;
                display = window.Displays.First(d => d.Index != current);
            });
            AddStep("change to that display", () => window.CurrentDisplayBindable.Value = display);
            AddAssert("display changed to requested", () => window.CurrentDisplayBindable.Value, () => Is.EqualTo(display));

            for (int i = 0; i < 3; i++)
            {
                AddStep("cycle mode", () => window.CycleMode());
                AddAssert("display hasn't changed", () => window.CurrentDisplayBindable.Value, () => Is.EqualTo(display));
            }
        }

        private WindowMode getWindowModeForState(WindowState state)
        {
            switch (state)
            {
                default:
                    return WindowMode.Windowed;

                case WindowState.Fullscreen:
                    return WindowMode.Fullscreen;

                case WindowState.FullscreenBorderless:
                    return WindowMode.Borderless;
            }
        }
    }
}
