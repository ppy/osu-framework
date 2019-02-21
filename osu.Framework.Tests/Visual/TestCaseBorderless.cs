// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseBorderless : TestCase
    {
        private readonly SpriteText currentActualSize = new SpriteText();
        private readonly SpriteText currentClientSize = new SpriteText();
        private readonly SpriteText currentWindowMode = new SpriteText();
        private readonly SpriteText currentDisplay = new SpriteText();

        private readonly TextFlowContainer windowCaption;
        private readonly Container paddedContainer;
        private readonly Container screenContainer;
        private readonly Container windowContainer;
        private Vector2 screenContainerOffset;

        private static readonly Color4 active_fill = new Color4(255, 138, 104, 255);
        private static readonly Color4 active_stroke = new Color4(244, 74, 25, 255);
        private static readonly Color4 screen_fill = new Color4(255, 181, 104, 255);
        private static readonly Color4 screen_stroke = new Color4(244, 137, 25, 255);
        private static readonly Color4 window_fill = new Color4(95, 113, 197, 255);
        private static readonly Color4 window_stroke = new Color4(36, 59, 166, 255);

        private DesktopGameWindow window;
        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();

        public TestCaseBorderless()
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(100),
                        Child = paddedContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = screenContainer = new Container
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Child = windowContainer = new Container
                                {
                                    BorderColour = window_stroke,
                                    BorderThickness = 20,
                                    Masking = true,

                                    Children = new Drawable[] {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = window_fill
                                        },
                                        windowCaption = new TextFlowContainer(sprite =>
                                        {
                                            sprite.TextSize = 150;
                                            sprite.Colour = Color4.White;
                                        })
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding(50),
                                            Colour = Color4.White
                                        }
                                    }
                                }
                            }
                        }
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

            windowMode.ValueChanged += newMode => currentWindowMode.Text = $"Window Mode: {newMode}";
            windowMode.TriggerChange();
        }

        private Container createScreen(DisplayDevice device, string name)
        {
            return new Container
            {
                X = device.Bounds.X,
                Y = device.Bounds.Y,
                Width = device.Bounds.Width,
                Height = device.Bounds.Height,

                BorderColour = device.IsPrimary ? active_stroke : screen_stroke,
                BorderThickness = 20,
                Masking = true,

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = device.IsPrimary ? active_fill : screen_fill
                    },
                    new SpriteText
                    {
                        Padding = new MarginPadding(50),
                        Text = name,
                        TextSize = 200,
                        Colour = Color4.Black
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config, GameHost host)
        {
            window = host.Window as DesktopGameWindow;
            config.BindWith(FrameworkSetting.WindowMode, windowMode);

            if (window == null)
            {
                Console.WriteLine("No suitable window found");
                return;
            }

            refreshScreens();
            AddStep("set up screens", refreshScreens);

            const string desc1 = "Check whether the borderless window is properly set to the top left corner, even if it is obstructed by the taskbar";
            const string desc2 = "Check whether the window size is one pixel wider than the screen in each direction";

            for(int i = 0; ; i++)
            {
                var display = DisplayDevice.GetDisplay((DisplayIndex)i);
                if(display == null) break;

                // set up window
                AddStep("switch to windowed", () => windowMode.Value = WindowMode.Windowed);
                AddStep("set client size to 1280x720", () => window.ClientSize = new Size(1280, 720));
                AddStep("center window on screen " + i, () => window.CentreToScreen(display));

                // borderless alignment tests
                AddStep("switch to borderless", () => windowMode.Value = WindowMode.Borderless);
                AddAssert("check window location", () => window.Location == display.Bounds.Location, desc1);
                AddAssert("check window size", () => new Size(window.Width - 1, window.Height - 1) == display.Bounds.Size, desc2);
                AddAssert("check current screen", () => window.CurrentDisplay == display);

                // verify the window size is restored correctly
                AddStep("switch to windowed", () => windowMode.Value = WindowMode.Windowed);
                AddAssert("check client size", () => window.ClientSize == new Size(1280, 720));
                AddAssert("check window position", () => Math.Abs(window.Position.X - 0.5f) < 0.01 && Math.Abs(window.Position.Y - 0.5f) < 0.01);
                AddAssert("check current screen", () => window.CurrentDisplay == display);
            }
        }

        private void refreshScreens()
        {
            screenContainer.Remove(windowContainer);
            screenContainer.Clear();
            var bounds = new RectangleI();

            for(int i = 0; ; i++)
            {
                var device = DisplayDevice.GetDisplay((DisplayIndex)i);
                if(device == null) break;

                screenContainer.Add(createScreen(device, device.IsPrimary ? $"Screen {i} (Primary)" : $"Screen {i}"));
                bounds = RectangleI.Union(bounds, new RectangleI(device.Bounds.X, device.Bounds.Y, device.Width, device.Height));
            }

            screenContainer.Add(windowContainer);
            screenContainerOffset = bounds.Location;

            foreach(var box in screenContainer.Children)
            {
                box.Position -= bounds.Location;
            }
            screenContainer.Size = bounds.Size;
        }

        private void updateWindowContainer()
        {
            if(window == null) return;
            bool fullscreen = window.WindowMode.Value == WindowMode.Fullscreen;

            windowContainer.X = window.X;
            windowContainer.Y = window.Y;
            windowContainer.Width = fullscreen ? window.CurrentDisplay.Width : window.Width;
            windowContainer.Height = fullscreen ? window.CurrentDisplay.Height : window.Height;
            windowContainer.Position -= screenContainerOffset;
            windowCaption.Text = $"{windowMode}\nSize: {window.Size.Width}x{window.Size.Height}\nClient: {window.ClientSize.Width}x{window.ClientSize.Height}";
        }

        protected override void Update()
        {
            base.Update();

            if(window == null)
            {
                currentDisplay.Text = "No suitable window found";
                return;
            }

            updateWindowContainer();
            var scale = Vector2.Divide(paddedContainer.DrawSize, screenContainer.Size);
            screenContainer.Scale = new Vector2(Math.Min(scale.X, scale.Y));

            currentActualSize.Text = $"Window size: {window?.Size}";
            currentClientSize.Text = $"Client size: {window?.ClientSize}";
            currentDisplay.Text = $"Current Display: {window?.CurrentDisplay}";
        }
    }
}
