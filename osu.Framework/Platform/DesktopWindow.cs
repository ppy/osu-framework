// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Input;
using osu.Framework.Platform.Sdl;
using osuTK;

namespace osu.Framework.Platform
{
    public class DesktopWindow : Window
    {
        private readonly BindableSize sizeFullscreen = new BindableSize();
        private readonly BindableSize sizeWindowed = new BindableSize();
        private readonly BindableDouble windowPositionX = new BindableDouble();
        private readonly BindableDouble windowPositionY = new BindableDouble();
        private readonly Bindable<DisplayIndex> windowDisplayIndex = new Bindable<DisplayIndex>();

        public readonly Bindable<ConfineMouseMode> ConfineMouseMode = new Bindable<ConfineMouseMode>();

        /// <summary>
        /// Initialises a window for desktop platforms.
        /// Uses <see cref="Sdl2WindowBackend"/> and <see cref="PassthroughGraphicsBackend"/>.
        /// </summary>
        public DesktopWindow()
            : base(new Sdl2WindowBackend(), new PassthroughGraphicsBackend())
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            base.SetupWindow(config);

            config.BindWith(FrameworkSetting.LastDisplayDevice, windowDisplayIndex);
            windowDisplayIndex.BindValueChanged(evt => CurrentDisplay.Value = Displays.ElementAtOrDefault((int)evt.NewValue), true);

            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);

            sizeFullscreen.ValueChanged += evt =>
            {
                if (!evt.NewValue.IsEmpty && WindowState.Value == Platform.WindowState.Fullscreen)
                    Size.Value = evt.NewValue;
            };

            sizeWindowed.ValueChanged += evt =>
            {
                if (!evt.NewValue.IsEmpty && WindowState.Value == Platform.WindowState.Normal)
                    Size.Value = evt.NewValue;
            };

            config.BindWith(FrameworkSetting.WindowedSize, sizeWindowed);

            config.BindWith(FrameworkSetting.WindowedPositionX, windowPositionX);
            config.BindWith(FrameworkSetting.WindowedPositionY, windowPositionY);

            config.BindWith(FrameworkSetting.WindowMode, WindowMode);
            WindowMode.BindValueChanged(evt => UpdateWindowMode(evt.NewValue), true);

            config.BindWith(FrameworkSetting.ConfineMouseMode, ConfineMouseMode);
            ConfineMouseMode.BindValueChanged(confineMouseModeChanged, true);

            Resized += onResized;
            Moved += onMoved;
        }

        protected Vector2 GetRelativePosition(Display display)
        {
            var displaySize = display.Bounds.Size;
            var windowX = Position.Value.X - display.Bounds.Location.X;
            var windowY = Position.Value.Y - display.Bounds.Location.Y;
            var windowSize = Size.Value;

            return new Vector2(
                displaySize.Width > windowSize.Width ? (float)windowX / (displaySize.Width - windowSize.Width) : 0,
                displaySize.Height > windowSize.Height ? (float)windowY / (displaySize.Height - windowSize.Height) : 0);
        }

        protected void SetRelativePosition(float x, float y, Display display)
        {
            var displaySize = display.Bounds.Size;
            var windowSize = Size.Value;
            var windowX = (int)Math.Round((displaySize.Width - windowSize.Width) * x);
            var windowY = (int)Math.Round((displaySize.Height - windowSize.Height) * y);

            Position.Value = new Point(windowX + display.Bounds.X, windowY + display.Bounds.Y);
        }

        protected virtual void UpdateWindowMode(WindowMode mode)
        {
            var currentDisplay = CurrentDisplay.Value;

            switch (mode)
            {
                case Configuration.WindowMode.Fullscreen:
                    var newFullscreenSize = sizeFullscreen.Value;

                    WindowState.Value = Platform.WindowState.Fullscreen;
                    Size.Value = newFullscreenSize;

                    break;

                case Configuration.WindowMode.Borderless:
                    var newBorderlessSize = sizeFullscreen.Value;

                    WindowState.Value = Platform.WindowState.FullscreenBorderless;
                    Size.Value = newBorderlessSize;

                    break;

                case Configuration.WindowMode.Windowed:
                    var newWindowedSize = sizeWindowed.Value;

                    WindowState.Value = Platform.WindowState.Normal;
                    Size.Value = newWindowedSize;
                    SetRelativePosition((float)windowPositionX.Value, (float)windowPositionY.Value, currentDisplay);

                    break;
            }
        }

        private void onResized()
        {
            if (!Size.Value.IsEmpty && WindowMode.Value == Configuration.WindowMode.Windowed)
                sizeWindowed.Value = Size.Value;
        }

        private void onMoved(Point point)
        {
            // Need to call onResized as it's possible the window may have moved between a
            // high-dpi and regular display. This means the logical window size would be the same,
            // but the scale will have changed.
            onResized();

            var currentDisplay = CurrentDisplay.Value;

            if (WindowMode.Value == Configuration.WindowMode.Windowed)
            {
                var relativePosition = GetRelativePosition(currentDisplay);
                windowPositionX.Value = relativePosition.X;
                windowPositionY.Value = relativePosition.Y;
            }

            windowDisplayIndex.Value = (DisplayIndex)currentDisplay.Index;
        }

        private void confineMouseModeChanged(ValueChangedEvent<ConfineMouseMode> args)
        {
            bool confine = false;

            switch (args.NewValue)
            {
                case Input.ConfineMouseMode.Fullscreen:
                    confine = WindowMode.Value != Configuration.WindowMode.Windowed;
                    break;

                case Input.ConfineMouseMode.Always:
                    confine = true;
                    break;
            }

            if (confine)
                CursorState.Value |= Platform.CursorState.Confined;
            else
                CursorState.Value &= ~Platform.CursorState.Confined;
        }
    }
}
