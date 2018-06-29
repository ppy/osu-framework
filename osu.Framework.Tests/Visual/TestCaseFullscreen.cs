// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseFullscreen : TestCase
    {
        private readonly SpriteText currentActualSize = new SpriteText();

        private DesktopGameWindow window;
        private readonly BindableSize sizeFullscreen = new BindableSize();
        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();

        public TestCaseFullscreen()
        {
            var currentBindableSize = new SpriteText();

            Child = new FillFlowContainer
            {
                Children = new[]
                {
                    currentBindableSize,
                    currentActualSize,
                },
            };

            sizeFullscreen.ValueChanged += newSize => currentBindableSize.Text = $"Fullscreen size: {newSize}";
        }

        private void testResolution(int w, int h)
        {
            AddStep($"set to {w}x{h}", () => sizeFullscreen.Value = new Size(w, h));
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config, GameHost host)
        {
            window = (DesktopGameWindow)host.Window;
            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);
            config.BindWith(FrameworkSetting.WindowMode, windowMode);

            // so the test case doesn't change fullscreen size just when you enter it
            AddStep("nothing", () => { });

            // I'll assume that most monitors are compatible with 1280x720, and this is just for testing anyways
            testResolution(1280, 720);
            AddStep("change to fullscreen", () => windowMode.Value = WindowMode.Fullscreen);
            testResolution(1920, 1080);
            testResolution(1280, 960);
            AddStep("go back to windowed", () => windowMode.Value = WindowMode.Windowed);
        }

        protected override void Update()
        {
            base.Update();

            currentActualSize.Text = $"Window size: {window?.Bounds.Size}";
        }
    }
}
