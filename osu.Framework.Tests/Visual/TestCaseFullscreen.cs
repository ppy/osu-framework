// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Testing;
using System.Drawing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseFullscreen : TestCase
    {
        private readonly SpriteText currentActualSize = new SpriteText();

        private DesktopGameWindow window;
        private readonly BindableSize sizeFullscreen = new BindableSize();

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
            AddStep($"{w}x{h}", () => sizeFullscreen.Value = new Size(w, h));
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config, GameHost host)
        {
            window = (DesktopGameWindow)host.Window;
            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);

            // so the test case doesn't change fullscreen size just when you enter it
            AddStep("nothing", () => { });
            testResolution(1920, 1080);
            testResolution(1280, 720);
            testResolution(1280, 960);
            testResolution(999, 999);
        }

        protected override void Update()
        {
            base.Update();

            currentActualSize.Text = $"Window size: {window.Bounds.Size}";
        }
    }
}
