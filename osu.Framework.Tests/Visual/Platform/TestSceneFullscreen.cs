// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneFullscreen : FrameworkTestScene
    {
        private readonly SpriteText currentActualSize = new SpriteText();
        private readonly SpriteText currentWindowMode = new SpriteText();
        private readonly SpriteText currentDisplay = new SpriteText();
        private readonly SpriteText supportedWindowModes = new SpriteText();

        private IWindow window;
        private readonly BindableSize sizeFullscreen = new BindableSize();
        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();

        public TestSceneFullscreen()
        {
            var currentBindableSize = new SpriteText();

            Child = new FillFlowContainer
            {
                Children = new[]
                {
                    currentBindableSize,
                    currentActualSize,
                    currentWindowMode,
                    supportedWindowModes,
                    currentDisplay
                },
            };

            sizeFullscreen.ValueChanged += newSize => currentBindableSize.Text = $"Fullscreen size: {newSize.NewValue}";
            windowMode.ValueChanged += newMode => currentWindowMode.Text = $"Window Mode: {newMode.NewValue}";
        }

        private void testResolution(int w, int h)
        {
            AddStep($"set to {w}x{h}", () => sizeFullscreen.Value = new Size(w, h));
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config, GameHost host)
        {
            window = host.Window;
            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);
            config.BindWith(FrameworkSetting.WindowMode, windowMode);
            currentWindowMode.Text = $"Window Mode: {windowMode}";

            if (window == null)
                return;

            supportedWindowModes.Text = $"Supported Window Modes: {string.Join(", ", window.SupportedWindowModes)}";

            // so the test case doesn't change fullscreen size just when you enter it
            AddStep("nothing", () => { });

            var initialWindowMode = windowMode.Value;

            // if we support windowed mode, switch to it and test resizing the window
            if (window.SupportedWindowModes.Contains(WindowMode.Windowed))
            {
                AddStep("change to windowed", () => windowMode.Value = WindowMode.Windowed);
                AddStep("change window size", () => config.GetBindable<Size>(FrameworkSetting.WindowedSize).Value = new Size(640, 640));
            }

            // if we support borderless, test that it can be used
            if (window.SupportedWindowModes.Contains(WindowMode.Borderless))
                AddStep("change to borderless", () => windowMode.Value = WindowMode.Borderless);

            // if we support fullscreen mode, switch to it and test swapping resolutions
            if (window.SupportedWindowModes.Contains(WindowMode.Fullscreen))
            {
                AddStep("change to fullscreen", () => windowMode.Value = WindowMode.Fullscreen);
                testResolution(1920, 1080);
                testResolution(1280, 960);
                testResolution(9999, 9999);
            }

            // go back to initial window mode
            AddStep($"revert to {initialWindowMode.ToString()}", () => windowMode.Value = initialWindowMode);

            // show the available displays
            AddStep("query Window.Displays", () =>
            {
                var displays = window.Displays.ToArray();
                Logger.Log($"Available displays: {displays.Length}");
                displays.ForEach(display =>
                {
                    Logger.Log(display.ToString());
                    display.DisplayModes.ForEach(mode => Logger.Log($"-- {mode}"));
                });
            });

            AddStep("query Window.Display", () => Logger.Log(window.Display.ToString()));

            AddStep("query Window.DisplayMode", () => Logger.Log(window.DisplayMode.ToString()));
        }

        protected override void Update()
        {
            base.Update();

            currentActualSize.Text = $"Window size: {window?.Bounds.Size}";
            currentDisplay.Text = $"Current display device: {window?.CurrentDisplay}";
        }
    }
}
