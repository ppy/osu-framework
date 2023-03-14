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

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot run in headless mode (a window instance is required).")]
    public partial class TestSceneBorderless : FrameworkTestScene
    {
        private readonly SpriteText currentActualSize = new SpriteText();
        private readonly SpriteText currentClientSize = new SpriteText();
        private readonly SpriteText currentWindowMode = new SpriteText();
        private readonly SpriteText currentDisplay = new SpriteText();

        private SDL2DesktopWindow? window;
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
            window = host.Window as SDL2DesktopWindow;
            config.BindWith(FrameworkSetting.WindowMode, windowMode);

            windowMode.BindValueChanged(mode => currentWindowMode.Text = $"Window Mode: {mode.NewValue}", true);

            if (window == null)
            {
                return;
            }

            const string desc2 = "Check whether the window size is one pixel wider than the screen in each direction";

            Point originalWindowPosition = Point.Empty;

            // so the test doesn't switch to windowed on startup.
            AddStep("nothing", () => { });

            foreach (var display in window.Displays)
            {
                AddLabel($"Steps for display {display.Index}");

                // set up window
                AddStep("switch to windowed", () => windowMode.Value = WindowMode.Windowed);
                AddStep($"move window to display {display.Index}", () => window.CurrentDisplayBindable.Value = window.Displays.ElementAt(display.Index));
                AddStep("set window size to 1280x720", () => config.SetValue(FrameworkSetting.WindowedSize, new Size(1280, 720)));
                AddStep("store window position", () => originalWindowPosition = window.Position);

                // borderless alignment tests
                AddStep("switch to borderless", () => windowMode.Value = WindowMode.Borderless);
                AddAssert("check window position", () => new Point(window.Position.X, window.Position.Y) == display.Bounds.Location);
                AddAssert("check window size", () => new Size(window.Size.Width, window.Size.Height) == display.Bounds.Size, desc2);
                AddAssert("check current screen", () => window.CurrentDisplayBindable.Value.Index == display.Index);

                // verify the window size is restored correctly
                AddStep("switch to windowed", () => windowMode.Value = WindowMode.Windowed);
                AddAssert("check client size", () => window.ClientSize == new Size(1280, 720));
                AddAssert("check window position", () => originalWindowPosition == window.Position);
                AddAssert("check current screen", () => window.CurrentDisplayBindable.Value.Index == display.Index);
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
