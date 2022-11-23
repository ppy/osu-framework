// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Platform
{
    public partial class WindowDisplaysPreview : Container
    {
        public const float FONT_SIZE = 120f;

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

        private SDL2DesktopWindow? window;
        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();
        private readonly Bindable<Display> currentDisplay = new Bindable<Display>();

        public WindowDisplaysPreview()
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding(70),
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
                            Depth = -10,
                            Alpha = 0.7f,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = window_fill
                                },
                                windowCaption = new TextFlowContainer(sprite =>
                                {
                                    sprite.Font = sprite.Font.With(size: FONT_SIZE);
                                    sprite.Colour = Color4.White;
                                })
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Padding = new MarginPadding(50),
                                    Colour = Color4.White
                                }
                            }
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config, GameHost host)
        {
            window = host.Window as SDL2DesktopWindow;
            config.BindWith(FrameworkSetting.WindowMode, windowMode);

            if (window != null)
            {
                window.DisplaysChanged += onDisplaysChanged;
                currentDisplay.BindTo(window.CurrentDisplayBindable);
                currentDisplay.BindValueChanged(_ => Scheduler.AddOnce(() => refreshScreens(window.Displays)));

                refreshScreens(window.Displays);
            }
        }

        private void onDisplaysChanged(IEnumerable<Display> displays)
        {
            Scheduler.AddOnce(refreshScreens, displays);
        }

        private void refreshScreens(IEnumerable<Display> displays)
        {
            screenContainer.RemoveAll(d => d != windowContainer, false);

            var bounds = new RectangleI();

            foreach (var display in displays)
            {
                screenContainer.Add(createScreen(display, window.AsNonNull().CurrentDisplayBindable.Value.Index));
                bounds = RectangleI.Union(bounds, new RectangleI(display.Bounds.X, display.Bounds.Y, display.Bounds.Width, display.Bounds.Height));
            }

            screenContainerOffset = bounds.Location;

            foreach (var box in screenContainer.Children)
            {
                box.Position -= bounds.Location;
            }

            screenContainer.Size = bounds.Size;
        }

        private Container createScreen(Display display, int activeDisplayIndex)
        {
            bool isActive = display.Index == activeDisplayIndex;

            return new Container
            {
                X = display.Bounds.X,
                Y = display.Bounds.Y,
                Width = display.Bounds.Width,
                Height = display.Bounds.Height,

                BorderColour = isActive ? active_stroke : screen_stroke,
                BorderThickness = 20,
                Masking = true,

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = isActive ? active_fill : screen_fill
                    },
                    new TextFlowContainer(sprite =>
                    {
                        sprite.Font = new FontUsage(size: FONT_SIZE);
                        sprite.Colour = Color4.Black;
                    })
                    {
                        RelativeSizeAxes = Axes.Both,
                        Text = $"{display.Name}\n"
                               + $"{display.Bounds.Width}x{display.Bounds.Height}\n"
                               + $"Mode: {modeName(display.DisplayModes.First())}",
                        Padding = new MarginPadding(50),
                    }
                }
            };
        }

        private string modeName(DisplayMode mode) => $"{mode.Size.Width}x{mode.Size.Height}@{mode.RefreshRate}";

        private void updateWindowContainer()
        {
            if (window == null) return;

            bool fullscreen = window.WindowMode.Value == WindowMode.Fullscreen;
            var currentBounds = window.CurrentDisplayBindable.Value.Bounds;

            windowContainer.X = window.Position.X;
            windowContainer.Y = window.Position.Y;
            windowContainer.Width = fullscreen ? currentBounds.Width : window.Size.Width;
            windowContainer.Height = fullscreen ? currentBounds.Height : window.Size.Height;
            windowContainer.Position -= screenContainerOffset;
            windowCaption.Text = $"{windowMode}\nSize: {window.Size.Width}x{window.Size.Height}\nClient: {window.ClientSize.Width}x{window.ClientSize.Height}";
        }

        protected override void Update()
        {
            base.Update();

            if (window == null)
                return;

            updateWindowContainer();
            var scale = Vector2.Divide(paddedContainer.DrawSize, screenContainer.Size);
            screenContainer.Scale = new Vector2(Math.Min(scale.X, scale.Y));
        }

        protected override void Dispose(bool isDisposing)
        {
            if (window != null)
                window.DisplaysChanged -= onDisplaysChanged;

            base.Dispose(isDisposing);
        }
    }
}
