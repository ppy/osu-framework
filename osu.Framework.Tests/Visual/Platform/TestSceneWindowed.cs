// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Drawing;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot be run in headless mode (a window instance is required).")]
    public class TestSceneWindowed : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        private SDL2DesktopWindow sdlWindow;

        [BackgroundDependencyLoader]
        private void load()
        {
            sdlWindow = (SDL2DesktopWindow)host.Window;
        }

        [SetUp]
        public void SetUp() => Schedule(() => sdlWindow.Resizable = true);

        [Test]
        public void TestToggleResizable()
        {
            AddToggleStep("toggle resizable", state => sdlWindow.Resizable = state);
        }

        [Test]
        public void TestMinimumSize()
        {
            const int min_width = 1024;
            const int min_height = 768;

            AddStep("reset window to valid size", () => setWindowSize(new Size(640, 480)));
            AddStep("set minimum size above client size", () => sdlWindow.MinSize = new Size(min_width, min_height));
            assertWindowSize(new Size(min_width, min_height));

            AddStep("reset window to valid size", () => setWindowSize(new Size(1280, 960)));
            AddStep("set client size below minimum size", () => setWindowSize(new Size(640, 480)));
            assertWindowSize(new Size(min_width, min_height));

            AddStep("overlapping size throws", () => Assert.Throws<InvalidOperationException>(() => sdlWindow.MinSize = sdlWindow.MaxSize + new Size(1, 1)));
            AddStep("negative size throws", () => Assert.Throws<InvalidOperationException>(() => sdlWindow.MinSize = new Size(-1, -1)));
            AddStep("reset minimum size", () => sdlWindow.MinSize = new Size(640, 480));
        }

        [Test]
        public void TestMaximumSize()
        {
            const int max_width = 1024;
            const int max_height = 768;

            AddStep("reset window to valid size", () => setWindowSize(new Size(1280, 960)));
            AddStep("set maximum size below client size", () => sdlWindow.MaxSize = new Size(max_width, max_height));
            assertWindowSize(new Size(max_width, max_height));

            // when the maximum window size changes to a value below the current size, the window implicitly enters maximised state.
            // when in maximised state, the "windowed size" config bindable is ineffective until the window goes back to normal.
            AddStep("reset window to normal state", () => sdlWindow.WindowState = WindowState.Normal);

            AddStep("reset window to valid size", () => setWindowSize(new Size(640, 480)));
            AddStep("set client size above maximum size", () => setWindowSize(new Size(1280, 960)));
            assertWindowSize(new Size(max_width, max_height));

            AddStep("overlapping size throws", () => Assert.Throws<InvalidOperationException>(() => sdlWindow.MaxSize = sdlWindow.MinSize - new Size(1, 1)));
            AddStep("negative size throws", () => Assert.Throws<InvalidOperationException>(() => sdlWindow.MaxSize = new Size(-1, -1)));
            AddStep("reset maximum size", () => sdlWindow.MaxSize = new Size(65536, 65536));
        }

        private void setWindowSize(Size size) => config.SetValue(FrameworkSetting.WindowedSize, size);

        private void assertWindowSize(Size size)
        {
            AddAssert($"client size = {size.Width}x{size.Height} (with scale)", () => sdlWindow.ClientSize == (size * sdlWindow.Scale).ToSize());
            AddAssert($"size in config = {size.Width}x{size.Height}", () => config.Get<Size>(FrameworkSetting.WindowedSize) == size);
        }
    }
}
