// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Tests.Visual.Platform
{
    [System.ComponentModel.Description("For complete validation, this test should run be with different WindowModes at startup, and with different resolutions in Fullscreen")]
    public class TestSceneDisplayMode : FrameworkTestScene
    {
        [Resolved]
        private FrameworkConfigManager config { get; set; }

        private readonly BindableSize configSizeFullscreen = new BindableSize();
        private readonly Bindable<WindowMode> configWindowMode = new Bindable<WindowMode>();

        private IWindow window;

        private DisplayMode displayMode => window.CurrentDisplayMode.Value;
        private Display currentDisplay => window.CurrentDisplayBindable.Value;

        private readonly Bindable<string> textConfigSizeFullscreen = new Bindable<string>();
        private readonly Bindable<string> textConfigWindowMode = new Bindable<string>();
        private readonly Bindable<string> textCurrentDisplay = new Bindable<string>();
        private readonly Bindable<string> textCurrentDisplayMode = new Bindable<string>();
        private readonly TextFlowContainer textWindowDisplayModes;

        public TestSceneDisplayMode()
        {
            Child = new FillFlowContainer
            {
                Padding = new MarginPadding(10),
                Spacing = new Vector2(10),
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = "FrameworkConfigManager settings: ",
                        Font = FrameworkFont.Regular.With(size: 24),
                    },
                    new SpriteText { Current = textConfigSizeFullscreen, Font = FrameworkFont.Condensed },
                    new SpriteText { Current = textConfigWindowMode, Font = FrameworkFont.Condensed },
                    new SpriteText
                    {
                        Text = "IWindow properties: ",
                        Font = FrameworkFont.Regular.With(size: 24),
                        Margin = new MarginPadding { Top = 7 },
                    },
                    new SpriteText { Current = textCurrentDisplay, Font = FrameworkFont.Condensed },
                    new SpriteText { Current = textCurrentDisplayMode, Font = FrameworkFont.Condensed.With(size: 16) },
                    new SpriteText { Text = "Available display modes for current display:", Font = FrameworkFont.Condensed },
                    textWindowDisplayModes = new TextFlowContainer(text => text.Font = FrameworkFont.Condensed.With(size: 16))
                    {
                        Width = 1000,
                        AutoSizeAxes = Axes.Y,
                    }
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            window = host.Window;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            config.BindWith(FrameworkSetting.SizeFullscreen, configSizeFullscreen);
            config.BindWith(FrameworkSetting.WindowMode, configWindowMode);

            configSizeFullscreen.BindValueChanged(e => textConfigSizeFullscreen.Value = $"SizeFullscreen: {e.NewValue}", true);
            configWindowMode.BindValueChanged(e => textConfigWindowMode.Value = $"WindowMode: {e.NewValue}", true);

            window.CurrentDisplayMode.BindValueChanged(e => textCurrentDisplayMode.Value = $"CurrentDisplayMode: {e.NewValue}", true);
            window.CurrentDisplayBindable.BindValueChanged(e => Schedule(() => textWindowDisplayModes.Text = string.Join('\n', e.NewValue.DisplayModes)), true);
        }

        protected override void Update()
        {
            base.Update();
            // update every time, as Display.Equals will cause the bindable not to update if only the resolution changes.
            textCurrentDisplay.Value = $"CurrentDisplay: {currentDisplay}";
        }

        [TestCase(null)]
        [TestCase(WindowMode.Windowed)]
        [TestCase(WindowMode.Borderless)]
        [TestCase(WindowMode.Fullscreen)]
        public void TestDisplayModeSanity(WindowMode? windowMode)
        {
            if (window == null)
            {
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");
                return;
            }

            switch (windowMode)
            {
                case null: // test startup
                    AddAssert("mode has valid DisplayIndex", () => displayMode.DisplayIndex != -1);
                    checkDisplayModeSanity(configWindowMode.Value == WindowMode.Fullscreen && !configSizeFullscreen.IsDefault);
                    break;

                case WindowMode.Windowed:
                case WindowMode.Borderless:
                    AddStep($"change to {windowMode}", () => configWindowMode.Value = windowMode.Value);
                    checkDisplayModeSanity(false);
                    break;

                case WindowMode.Fullscreen:
                    AddStep("change to fullscreen", () => configWindowMode.Value = WindowMode.Fullscreen);

                    setFullscreenResolution(9999, 9999);
                    checkDisplayModeSanity(false); // importantly, at default fullscreen resolution, it should match the desktop resolution.

                    setFullscreenResolution(1920, 1080);
                    checkDisplayModeSanity(true);

                    setFullscreenResolution(1280, 720);
                    checkDisplayModeSanity(true);
                    break;
            }
        }

        private void checkDisplayModeSanity(bool checkResolutionAgainstFullscreen)
        {
            // TODO: this should probably be valid
            // AddAssert("mode has valid Index", () => displayMode.Index != -1);

            AddAssert("DisplayIndex matches display", () => displayMode.DisplayIndex == currentDisplay.Index);
            AddAssert("display has current mode", () => currentDisplay.DisplayModes.Any(mode => modesSimilar(mode, displayMode)));
            AddAssert("mode has valid RefreshRate", () => displayMode.RefreshRate != 0);

            // TODO: the current display bounds should match even in fullscreen, this doesn't seem to work currently

            if (checkResolutionAgainstFullscreen)
                AddAssert("Size matches fullscreen resolution", () => displayMode.Size == configSizeFullscreen.Value);
            else
                AddAssert("Size matches bindable display resolution", () => displayMode.Size == currentDisplay.Bounds.Size);

            // this shouldn't work, but it does...
            AddAssert("Size matches actual display resolution", () => displayMode.Size == window.Displays.ElementAt(displayMode.DisplayIndex).Bounds.Size);
        }

        private void setFullscreenResolution(int w, int h)
        {
            AddStep($"set fullscreen to {w}x{h}", () => configSizeFullscreen.Value = new Size(w, h));
            AddWaitStep("wait for resolution change", 5);
        }

        private bool modesSimilar(DisplayMode left, DisplayMode right) =>
            left.Format == right.Format
            && left.Size == right.Size
            && left.BitsPerPixel == right.BitsPerPixel
            && left.RefreshRate == right.RefreshRate
            // && left.Index == right.Index
            && left.DisplayIndex == right.DisplayIndex;
    }
}
