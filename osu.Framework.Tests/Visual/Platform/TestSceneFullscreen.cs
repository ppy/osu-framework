// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
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
using WindowState = osu.Framework.Platform.WindowState;

namespace osu.Framework.Tests.Visual.Platform
{
    public partial class TestSceneFullscreen : FrameworkTestScene
    {
        public override bool AutomaticallyRunFirstStep => false;

        private readonly SpriteText currentActualSize = new SpriteText();
        private readonly SpriteText currentDisplayMode = new SpriteText();
        private readonly SpriteText currentWindowMode = new SpriteText();
        private readonly SpriteText currentWindowState = new SpriteText();
        private readonly SpriteText supportedWindowModes = new SpriteText();
        private readonly Dropdown<Display> displaysDropdown;

        private IWindow window;
        private Display display;
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

        private void updateDisplays(IEnumerable<Display> displays)
        {
            displaysDropdown.Items = displays;
            display = window.CurrentDisplayBindable.Value;
        }

        [Test]
        public void TestScreenModeSwitch()
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            var initialWindowMode = windowMode.Value;
            var testSize = new Size(640, 640);
            var originalWindowPosition = Point.Empty;

            // if we support windowed mode, switch to it and test resizing the window
            if (window.SupportedWindowModes.Contains(WindowMode.Windowed))
            {
                AddStep("change to windowed", () => windowMode.Value = WindowMode.Windowed);
                AddWaitStep("wait some", 10); // for macOS transition animation
                AddStep("change window size", () => config.SetValue(FrameworkSetting.WindowedSize, testSize));

                AddAssert("check window size", () => window.Size, () => Is.EqualTo(testSize));
                AddAssert("check client size", () => window.ClientSize, () => Is.EqualTo((testSize * window.Scale).ToSize()));
                AddStep("store window position", () => originalWindowPosition = window.Position);
            }

            // if we support borderless, test that it can be used
            if (window.SupportedWindowModes.Contains(WindowMode.Borderless))
            {
                AddStep("change to borderless", () => windowMode.Value = WindowMode.Borderless);
                AddWaitStep("wait some", 10); // for macOS transition animation

                // Depending on platform and display, borderless windows can either cover the entire display or just the usable area. TODO: tighten the assertion to explicitly distinguish these cases.
                AddAssert("check window position", () => window.Position, () => Is.EqualTo(display.UsableBounds.Location).Or.EqualTo(display.Bounds.Location));
                AddAssert("check window size", () => window.Size, () => Is.EqualTo(display.UsableBounds.Size).Or.EqualTo(display.Bounds.Size));
                AddAssert("check client size", () => window.ClientSize, () => Is.EqualTo((window.Size * window.Scale).ToSize()));
            }

            // if we support fullscreen mode, switch to it and test swapping resolutions
            if (window.SupportedWindowModes.Contains(WindowMode.Fullscreen))
            {
                AddStep("change to fullscreen", () => windowMode.Value = WindowMode.Fullscreen);
                testResolution(1920, 1080);
                testResolution(1280, 960);
                testResolution(9999, 9999);
            }

            if (window.SupportedWindowModes.Contains(WindowMode.Windowed))
            {
                AddStep("change to windowed", () => windowMode.Value = WindowMode.Windowed);
                AddWaitStep("wait some", 10); // for macOS transition animation
                AddAssert("check window size", () => window.Size, () => Is.EqualTo(testSize));
                AddAssert("check client size", () => window.ClientSize, () => Is.EqualTo((testSize * window.Scale).ToSize()));
                AddAssert("check window position", () => originalWindowPosition, () => Is.EqualTo(window.Position));
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

        [Test]
        public void TestMinimiseOnFocusLoss([Values] bool enabled, [Values] WindowMode mode)
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            AddStep($"set to {mode}", () => windowMode.Value = mode);
            AddStep($"set minimise on focus: {enabled}", () => config.SetValue(FrameworkSetting.MinimiseOnFocusLossInFullscreen, enabled));
            AddUntilStep("wait for window to lose focus", () => !window.IsActive.Value);

            switch (mode)
            {
                case WindowMode.Windowed:
                case WindowMode.Borderless:
                    assertMinimised(false);
                    break;

                case WindowMode.Fullscreen:
                    assertMinimised(enabled);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            void assertMinimised(bool minimised)
            {
                if (minimised)
                    AddAssert("window is minimised", () => window.WindowState, () => Is.EqualTo(WindowState.Minimised));
                else
                    AddAssert("window not minimised", () => window.WindowState, () => Is.Not.EqualTo(WindowState.Minimised));
            }
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
            AddWaitStep("wait some", 10); // for macOS transition animation

            AddAssert("check window position", () => window.Position, () => Is.EqualTo(display.Bounds.Location));
            AddAssert("check window size", () => window.Size, () => Is.EqualTo(display.Bounds.Size));
            AddAssert("check client size", () => window.ClientSize, () => Is.EqualTo((display.Bounds.Size * window.Scale).ToSize()));
            AddAssert("check current screen", () => window.CurrentDisplayBindable.Value.Index, () => Is.EqualTo(display.Index));
        }

        protected override void Dispose(bool isDisposing)
        {
            if (window != null)
                window.DisplaysChanged -= onDisplaysChanged;

            base.Dispose(isDisposing);
        }
    }
}
