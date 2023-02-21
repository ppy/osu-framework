// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Tests.Visual.Platform
{
    public partial class TestSceneFullscreen : FrameworkTestScene
    {
        private readonly SpriteText currentActualSize = new SpriteText();
        private readonly SpriteText currentDisplayMode = new SpriteText();
        private readonly SpriteText currentWindowMode = new SpriteText();
        private readonly SpriteText currentWindowState = new SpriteText();
        private readonly SpriteText supportedWindowModes = new SpriteText();
        private readonly Dropdown<Display> displaysDropdown;

        private IWindow window;
        private readonly BindableSize sizeFullscreen = new BindableSize();
        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();

        public TestSceneFullscreen()
        {
            var currentBindableSize = new SpriteText();

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Padding = new MarginPadding(10),
                    Spacing = new Vector2(10),
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        currentBindableSize,
                        currentActualSize,
                        currentDisplayMode,
                        currentWindowMode,
                        currentWindowState,
                        supportedWindowModes,
                        displaysDropdown = new BasicDropdown<Display> { Width = 800 }
                    }
                },
                new WindowDisplaysPreview
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 230 }
                }
            };

            sizeFullscreen.ValueChanged += newSize => currentBindableSize.Text = $"Fullscreen size: {newSize.NewValue}";
            windowMode.ValueChanged += newMode => currentWindowMode.Text = $"Window Mode: {newMode.NewValue}";
        }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            window = host.Window;
            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);
            config.BindWith(FrameworkSetting.WindowMode, windowMode);
            currentWindowMode.Text = $"Window Mode: {windowMode}";

            if (window == null)
                return;

            window.DisplaysChanged += onDisplaysChanged;
            updateDisplays(window.Displays);

            displaysDropdown.Current.BindTo(window.CurrentDisplayBindable);

            supportedWindowModes.Text = $"Supported Window Modes: {string.Join(", ", window.SupportedWindowModes)}";
        }

        private void onDisplaysChanged(IEnumerable<Display> displays)
        {
            Scheduler.AddOnce(updateDisplays, displays);
        }

        private void updateDisplays(IEnumerable<Display> displays) => displaysDropdown.Items = displays;

        [Test]
        public void TestScreenModeSwitch()
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            // so the test case doesn't change fullscreen size just when you enter it
            AddStep("nothing", () => { });

            var initialWindowMode = windowMode.Value;

            // if we support windowed mode, switch to it and test resizing the window
            if (window.SupportedWindowModes.Contains(WindowMode.Windowed))
            {
                AddStep("change to windowed", () => windowMode.Value = WindowMode.Windowed);
                AddStep("change window size", () => config.SetValue(FrameworkSetting.WindowedSize, new Size(640, 640)));
            }

            // if we support borderless, test that it can be used
            if (window.SupportedWindowModes.Contains(WindowMode.Borderless))
                AddStep("change to borderless", () => windowMode.Value = WindowMode.Borderless);

            // if we support fullscreen mode, switch to it and test swapping resolutions
            if (window.SupportedWindowModes.Contains(WindowMode.Fullscreen))
            {
                AddStep("change to fullscreen", () => windowMode.Value = WindowMode.Fullscreen);
                AddAssert("window position updated", () => ((SDL2DesktopWindow)window).Position, () => Is.EqualTo(window.CurrentDisplayBindable.Value.Bounds.Location));
                testResolution(1920, 1080);
                testResolution(1280, 960);
                testResolution(9999, 9999);
            }

            // go back to initial window mode
            AddStep($"revert to {initialWindowMode.ToString()}", () => windowMode.Value = initialWindowMode);

            // show the available displays
            AddStep("query Window.Displays", () =>
            {
                var displaysArray = window.Displays.ToArray();
                Logger.Log($"Available displays: {displaysArray.Length}");
                displaysArray.ForEach(display =>
                {
                    Logger.Log(display.ToString());
                    display.DisplayModes.ForEach(mode => Logger.Log($"-- {mode}"));
                });
            });

            AddStep("query Window.CurrentDisplay", () => Logger.Log(window.CurrentDisplayBindable.ToString()));

            AddStep("query Window.CurrentDisplayMode", () => Logger.Log(window.CurrentDisplayMode.ToString()));

            AddStep("set default display", () => window.CurrentDisplayBindable.SetDefault());
        }

        [Test]
        public void TestConfineModes()
        {
            AddStep("set confined to never", () => config.SetValue(FrameworkSetting.ConfineMouseMode, ConfineMouseMode.Never));
            AddStep("set confined to fullscreen", () => config.SetValue(FrameworkSetting.ConfineMouseMode, ConfineMouseMode.Fullscreen));
            AddStep("set confined to always", () => config.SetValue(FrameworkSetting.ConfineMouseMode, ConfineMouseMode.Always));
        }

        protected override void Update()
        {
            base.Update();

            currentActualSize.Text = $"Window size: {window?.ClientSize}";
            currentDisplayMode.Text = $"Display mode: {window?.CurrentDisplayMode}";
            currentWindowState.Text = $"Window State: {window?.WindowState}";
        }

        private void testResolution(int w, int h)
        {
            AddStep($"set to {w}x{h}", () => sizeFullscreen.Value = new Size(w, h));
        }

        protected override void Dispose(bool isDisposing)
        {
            if (window != null)
                window.DisplaysChanged -= onDisplaysChanged;

            base.Dispose(isDisposing);
        }
    }
}
