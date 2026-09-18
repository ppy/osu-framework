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
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot run in headless mode (a window instance is required).")]
    public partial class TestSceneBorderless : FrameworkTestScene
    {
        public override bool AutomaticallyRunFirstStep => false;

        private readonly SpriteText currentActualSize = new SpriteText();
        private readonly SpriteText currentClientSize = new SpriteText();
        private readonly SpriteText currentWindowMode = new SpriteText();
        private readonly SpriteText currentDisplay = new SpriteText();

        private IWindow? window;
        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();

        public TestSceneBorderless()
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new WindowDisplaysPreview
                    {
                        Padding = new MarginPadding { Top = 100 },
                        RelativeSizeAxes = Axes.Both,
                    },
                    new FillFlowContainer
                    {
                        Padding = new MarginPadding(10),
                        Children = new[]
                        {
                            currentActualSize,
                            currentClientSize,
                            currentWindowMode,
                            currentDisplay
                        },
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config, GameHost host)
        {
            window = host.Window;
            config.BindWith(FrameworkSetting.WindowMode, windowMode);

            windowMode.BindValueChanged(mode => currentWindowMode.Text = $"Window Mode: {mode.NewValue}", true);

            if (window == null)
            {
                Logger.Log("No suitable window found. Skipping test.");
                return;
            }

            if (!window.SupportedWindowModes.Contains(WindowMode.Borderless))
            {
                Logger.Log("Borderless window mode is not supported on this platform. Skipping test.");
                return;
            }

            bool supportsWindowed = window.SupportedWindowModes.Contains(WindowMode.Windowed);
            var testSize = new Size(1280, 720);
            var originalWindowPosition = Point.Empty;

            foreach (var display in window.Displays)
            {
                AddLabel($"Steps for display {display.Index}");

                // set up window
                if (supportsWindowed)
                {
                    AddStep("switch to windowed", () => windowMode.Value = WindowMode.Windowed);
                    AddWaitStep("wait some", 10); // for macOS transition animation
                    AddStep($"move window to display {display.Index}", () => window.CurrentDisplayBindable.Value = window.Displays.ElementAt(display.Index));
                    AddStep("set window size to 1280x720", () => config.SetValue(FrameworkSetting.WindowedSize, testSize));
                    AddStep("store window position", () => originalWindowPosition = window.Position);
                }

                // borderless alignment tests
                AddStep("switch to borderless", () => windowMode.Value = WindowMode.Borderless);
                AddWaitStep("wait some", 10); // for macOS transition animation
                AddAssert("check current screen", () => window.CurrentDisplayBindable.Value.Index == display.Index);

                // Depending on platform and display, borderless windows can either cover the entire display or just the usable area. TODO: tighten the assertion to explicitly distinguish these cases.
                AddAssert("check window position", () => new Point(window.Position.X, window.Position.Y) == display.UsableBounds.Location || new Point(window.Position.X, window.Position.Y) == display.Bounds.Location);
                AddAssert("check window size", () => new Size(window.Size.Width, window.Size.Height) == display.UsableBounds.Size || new Size(window.Size.Width, window.Size.Height) == display.Bounds.Size);
                AddAssert("check client size", () => window.ClientSize == new Size((int)(display.UsableBounds.Width * window.Scale), (int)(display.UsableBounds.Height * window.Scale)) || window.ClientSize == new Size((int)(display.Bounds.Width * window.Scale), (int)(display.Bounds.Height * window.Scale)));

                // verify the window size is restored correctly
                if (supportsWindowed)
                {
                    AddStep("switch to windowed", () => windowMode.Value = WindowMode.Windowed);
                    AddWaitStep("wait some", 10); // for macOS transition animation
                    AddAssert("check window size", () => window.Size == testSize);
                    AddAssert("check client size", () => window.ClientSize == new Size((int)(testSize.Width * window.Scale), (int)(testSize.Height * window.Scale)));
                    AddAssert("check window position", () => originalWindowPosition == window.Position);
                    AddAssert("check current screen", () => window.CurrentDisplayBindable.Value.Index == display.Index);
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            if (window == null)
            {
                currentDisplay.Text = "No suitable window found";
                return;
            }

            currentActualSize.Text = $"Window size: {window?.Size}";
            currentClientSize.Text = $"Client size: {window?.ClientSize}";
            currentDisplay.Text = $"Current Display: {window?.CurrentDisplayBindable.Value.Name}";
        }
    }
}
