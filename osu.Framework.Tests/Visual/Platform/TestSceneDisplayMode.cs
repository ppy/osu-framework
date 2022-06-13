// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
    [System.ComponentModel.Description("For complete validation, run this with different window modes and resolutions at startup.")]
    [Ignore("This test cannot run in headless mode (a window instance is required).")]
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
            Child = new BasicScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
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
            switch (windowMode)
            {
                case null: // test startup
                    AddAssert("mode has valid DisplayIndex", () => displayMode.DisplayIndex != -1);
                    checkDisplayModeSanity(configWindowMode.Value == WindowMode.Fullscreen && !configSizeFullscreen.IsDefault, configWindowMode.Value != WindowMode.Windowed);
                    break;

                case WindowMode.Windowed:
                case WindowMode.Borderless:
                    AddStep($"change to {windowMode}", () => configWindowMode.Value = windowMode.Value);
                    checkDisplayModeSanity(false, windowMode != WindowMode.Windowed);
                    break;

                case WindowMode.Fullscreen:
                    AddStep("change to fullscreen", () => configWindowMode.Value = WindowMode.Fullscreen);

                    setFullscreenResolution(new Size(9999, 9999));
                    checkDisplayModeSanity(false, true); // importantly, at default fullscreen resolution, it should match the desktop resolution.

                    // reload (Ctrl+R) the test if testing on another display.
                    setFullscreenResolution(currentDisplay.FindDisplayMode(new Size(1920, 1080)).Size);
                    checkDisplayModeSanity(true, true);

                    setFullscreenResolution(currentDisplay.FindDisplayMode(new Size(1280, 720)).Size);
                    checkDisplayModeSanity(true, true);
                    break;
            }
        }

        private void checkDisplayModeSanity(bool checkResolutionAgainstFullscreen, bool checkClientSize)
        {
            AddAssert("DisplayIndex matches display", () => displayMode.DisplayIndex == currentDisplay.Index);
            AddAssert("display has current mode", () => currentDisplay.DisplayModes.Any(mode => mode == displayMode));
            AddAssert("mode has valid RefreshRate", () => displayMode.RefreshRate != 0);

            if (checkResolutionAgainstFullscreen)
                AddAssert("Size matches config fullscreen resolution", () => displayMode.Size == configSizeFullscreen.Value);

            if (!checkResolutionAgainstFullscreen)
                // This assert should be equivalent to the one under it, but `currentDisplay` is not updated when only the resolution changes.
                // Since the display resolution doesn't change in windowed and borderless, we can safely check this.
                // In fullscreen, the display resolution changes, so we can't check against `currentDisplay`.
                // TODO: fix CurrentDisplayBindable not updating when resolution changes
                AddAssert("Size matches bindable display resolution", () => displayMode.Size == currentDisplay.Bounds.Size);

            AddAssert("Size matches actual display resolution", () => displayMode.Size == window.Displays.ElementAt(displayMode.DisplayIndex).Bounds.Size);

            if (checkClientSize)
                AddAssert("Size matches client size", () => displayMode.Size == window.ClientSize); // not applicable in windowed
        }

        private void setFullscreenResolution(Size resolution)
        {
            AddStep($"set fullscreen to {resolution.Width}x{resolution.Height}", () => configSizeFullscreen.Value = resolution);
            AddWaitStep("wait for resolution change", 5);
        }
    }
}
