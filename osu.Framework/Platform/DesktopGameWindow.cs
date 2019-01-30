// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Input;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Platform
{
    public abstract class DesktopGameWindow : GameWindow
    {
        private const int default_width = 1366;
        private const int default_height = 768;

        private readonly BindableSize sizeFullscreen = new BindableSize();
        private readonly BindableSize sizeWindowed = new BindableSize();

        private readonly BindableDouble windowPositionX = new BindableDouble();
        private readonly BindableDouble windowPositionY = new BindableDouble();
        private readonly Bindable<DisplayIndex> windowDisplayIndex = new Bindable<DisplayIndex>();

        private DisplayDevice lastFullscreenDisplay;
        private bool inWindowModeTransition;

        public readonly Bindable<WindowMode> WindowMode = new Bindable<WindowMode>();

        public readonly Bindable<ConfineMouseMode> ConfineMouseMode = new Bindable<ConfineMouseMode>();

        public override IGraphicsContext Context => Implementation.Context;

        protected new osuTK.GameWindow Implementation => (osuTK.GameWindow)base.Implementation;

        public readonly BindableBool MapAbsoluteInputToWindow = new BindableBool();

        public override DisplayDevice CurrentDisplay
        {
            set
            {
                if (value == null || value == CurrentDisplay) return;

                var windowMode = WindowMode.Value;
                WindowMode.Value = Configuration.WindowMode.Windowed;

                var position = Position;
                Location = value.Bounds.Location;
                Position = position;

                WindowMode.Value = windowMode;
            }
        }

        public override IEnumerable<DisplayResolution> AvailableResolutions => CurrentDisplay.AvailableResolutions;

        protected DesktopGameWindow()
            : base(default_width, default_height)
        {
            Resize += OnResize;
            Move += OnMove;
        }

        public virtual void SetIconFromStream(Stream stream)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);

            sizeFullscreen.ValueChanged += newSize =>
            {
                if (WindowState == WindowState.Fullscreen)
                    ChangeResolution(CurrentDisplay, newSize);
            };

            sizeWindowed.ValueChanged += newSize =>
            {
                if (WindowState == WindowState.Normal)
                    ClientSize = sizeWindowed.Value;
            };

            config.BindWith(FrameworkSetting.WindowedSize, sizeWindowed);

            config.BindWith(FrameworkSetting.WindowedPositionX, windowPositionX);
            config.BindWith(FrameworkSetting.WindowedPositionY, windowPositionY);

            config.BindWith(FrameworkSetting.LastDisplayDevice, windowDisplayIndex);
            windowDisplayIndex.BindValueChanged(windowDisplayIndexChanged, true);

            config.BindWith(FrameworkSetting.WindowMode, WindowMode);
            WindowMode.BindValueChanged(windowModeChanged, true);

            config.BindWith(FrameworkSetting.ConfineMouseMode, ConfineMouseMode);
            ConfineMouseMode.BindValueChanged(confineMouseModeChanged, true);

            config.BindWith(FrameworkSetting.MapAbsoluteInputToWindow, MapAbsoluteInputToWindow);

            Exited += onExit;
        }

        protected virtual void ChangeResolution(DisplayDevice display, Size newSize)
        {
            if (newSize.Width == display.Width && newSize.Height == display.Height)
                return;

            var newResolution = display.AvailableResolutions
                                              .Where(r => r.Width == newSize.Width && r.Height == newSize.Height)
                                              .OrderByDescending(r => r.RefreshRate)
                                              .ThenByDescending(r => r.BitsPerPixel)
                                              .FirstOrDefault();

            if (newResolution == null)
            {
                // we wanted a new resolution but got nothing, which means osuTK didn't find this resolution
                RestoreResolution(display);
            }
            else
            {
                display.ChangeResolution(newResolution);
                ClientSize = newSize;
            }
        }

        protected virtual void RestoreResolution(DisplayDevice displayDevice) => displayDevice.RestoreResolution();

        protected void OnResize(object sender, EventArgs e)
        {
            if (ClientSize.IsEmpty) return;

            switch (WindowMode.Value)
            {
                case Configuration.WindowMode.Windowed:
                    sizeWindowed.Value = ClientSize;
                    break;
            }
        }

        protected void OnMove(object sender, EventArgs e)
        {
            if (inWindowModeTransition) return;
            if (WindowMode.Value == Configuration.WindowMode.Windowed)
            {
                // Values are clamped to a range of [-0.5, 1.5], so if more than half of the window was
                // outside of the combined screen area before the game was closed, it will be moved so
                // that at least half of it is on screen after a restart.
                windowPositionX.Value = Position.X;
                windowPositionY.Value = Position.Y;
            }

            windowDisplayIndex.Value = CurrentDisplay.GetIndex();
        }

        private void windowDisplayIndexChanged(DisplayIndex index) => CurrentDisplay = DisplayDevice.GetDisplay(index);

        private void confineMouseModeChanged(ConfineMouseMode newValue)
        {
            bool confine = false;

            switch (newValue)
            {
                case Input.ConfineMouseMode.Fullscreen:
                    confine = WindowMode.Value != Configuration.WindowMode.Windowed;
                    break;
                case Input.ConfineMouseMode.Always:
                    confine = true;
                    break;
            }

            if (confine)
                CursorState |= CursorState.Confined;
            else
                CursorState &= ~CursorState.Confined;
        }

        public void CentreToScreen(DisplayDevice display = null)
        {
            if (display != null) CurrentDisplay = display;
            Position = new Vector2(0.5f);
        }

        private void windowModeChanged(WindowMode newMode) => UpdateWindowMode(newMode);

        protected virtual void UpdateWindowMode(WindowMode newMode)
        {
            var currentDisplay = CurrentDisplay;

            try
            {
                inWindowModeTransition = true;
                switch (newMode)
                {
                    case Configuration.WindowMode.Fullscreen:
                        ChangeResolution(currentDisplay, sizeFullscreen);
                        lastFullscreenDisplay = currentDisplay;

                        WindowState = WindowState.Fullscreen;
                        break;
                    case Configuration.WindowMode.Borderless:
                        if (lastFullscreenDisplay != null)
                            RestoreResolution(lastFullscreenDisplay);
                        lastFullscreenDisplay = null;

                        WindowState = WindowState.Maximized;
                        WindowBorder = WindowBorder.Hidden;

                        // must add 1 to enter borderless
                        ClientSize = new Size(currentDisplay.Bounds.Width + 1, currentDisplay.Bounds.Height + 1);
                        Location = currentDisplay.Bounds.Location;
                        break;
                    case Configuration.WindowMode.Windowed:
                        if (lastFullscreenDisplay != null)
                            RestoreResolution(lastFullscreenDisplay);
                        lastFullscreenDisplay = null;

                        var newSize = sizeWindowed.Value;

                        WindowState = WindowState.Normal;
                        WindowBorder = WindowBorder.Resizable;

                        ClientSize = newSize;
                        Position = new Vector2((float)windowPositionX, (float)windowPositionY);
                        break;
                }
            }
            finally
            {
                inWindowModeTransition = false;
            }

            ConfineMouseMode.TriggerChange();
        }

        private void onExit()
        {
            switch (WindowMode.Value)
            {
                case Configuration.WindowMode.Fullscreen:
                    sizeFullscreen.Value = ClientSize;
                    break;
            }

            if (lastFullscreenDisplay != null)
                RestoreResolution(lastFullscreenDisplay);
            lastFullscreenDisplay = null;
        }

        public Vector2 Position
        {
            get
            {
                var display = CurrentDisplay;
                var relativeLocation = new Point(Location.X - display.Bounds.X, Location.Y - display.Bounds.Y);

                return new Vector2(
                    display.Width  > Size.Width  ? (float)relativeLocation.X / (display.Width  - Size.Width)  : 0,
                    display.Height > Size.Height ? (float)relativeLocation.Y / (display.Height - Size.Height) : 0);
            }
            set
            {
                var display = CurrentDisplay;

                var relativeLocation = new Point(
                    (int)Math.Round((display.Width - Size.Width) * value.X),
                    (int)Math.Round((display.Height - Size.Height) * value.Y));

                Location = new Point(relativeLocation.X + display.Bounds.X, relativeLocation.Y + display.Bounds.Y);
            }
        }

        public override void CycleMode()
        {
            switch (WindowMode.Value)
            {
                case Configuration.WindowMode.Windowed:
                    WindowMode.Value = Configuration.WindowMode.Borderless;
                    break;
                case Configuration.WindowMode.Borderless:
                    WindowMode.Value = Configuration.WindowMode.Fullscreen;
                    break;
                default:
                    WindowMode.Value = Configuration.WindowMode.Windowed;
                    break;
            }
        }

        public override VSyncMode VSync
        {
            get => Implementation.VSync;
            set => Implementation.VSync = value;
        }
    }
}
