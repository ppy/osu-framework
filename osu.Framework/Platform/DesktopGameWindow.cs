// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;

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

        public readonly Bindable<WindowMode> WindowMode = new Bindable<WindowMode>();

        public readonly Bindable<ConfineMouseMode> ConfineMouseMode = new Bindable<ConfineMouseMode>();

        internal override IGraphicsContext Context => Implementation.Context;

        protected new OpenTK.GameWindow Implementation => (OpenTK.GameWindow)base.Implementation;

        public readonly BindableBool MapAbsoluteInputToWindow = new BindableBool();

        public override DisplayDevice GetCurrentDisplay() => DisplayDevice.FromRectangle(Bounds) ?? DisplayDevice.Default;

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
                    ChangeResolution(GetCurrentDisplay(), newSize);
            };

            config.BindWith(FrameworkSetting.WindowedSize, sizeWindowed);

            config.BindWith(FrameworkSetting.WindowedPositionX, windowPositionX);
            config.BindWith(FrameworkSetting.WindowedPositionY, windowPositionY);

            config.BindWith(FrameworkSetting.ConfineMouseMode, ConfineMouseMode);

            config.BindWith(FrameworkSetting.MapAbsoluteInputToWindow, MapAbsoluteInputToWindow);

            ConfineMouseMode.ValueChanged += confineMouseMode_ValueChanged;
            ConfineMouseMode.TriggerChange();

            config.BindWith(FrameworkSetting.WindowMode, WindowMode);

            WindowMode.ValueChanged += windowMode_ValueChanged;
            WindowMode.TriggerChange();

            Exited += onExit;
        }

        protected virtual void ChangeResolution(DisplayDevice display, Size newSize)
        {
            if (newSize.Width == display.Width && newSize.Height == display.Height)
                return;

            var newResolution = display.AvailableResolutions
                                              .Where(r => r.Width == newSize.Width && r.Height == newSize.Height)
                                              .OrderByDescending(r => r.RefreshRate)
                                              .FirstOrDefault();

            if (newResolution == null)
            {
                // we wanted a new resolution but got nothing, which means OpenTK didn't find this resolution
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
            // The game is windowed and the whole window is on the screen (it is not minimized or moved outside of the screen)
            if (WindowMode.Value == Configuration.WindowMode.Windowed
                && Position.X > 0 && Position.X < 1
                && Position.Y > 0 && Position.Y < 1)
            {
                windowPositionX.Value = Position.X;
                windowPositionY.Value = Position.Y;
            }
        }

        private void confineMouseMode_ValueChanged(ConfineMouseMode newValue)
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

        private DisplayDevice lastFullscreenDisplay;

        private void windowMode_ValueChanged(WindowMode newMode) => UpdateWindowMode(newMode);

        protected virtual void UpdateWindowMode(WindowMode newMode)
        {
            var currentDisplay = GetCurrentDisplay();

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

                    //must add 1 to enter borderless
                    ClientSize = new Size(currentDisplay.Bounds.Width + 1, currentDisplay.Bounds.Height + 1);
                    break;
                default:
                    if (lastFullscreenDisplay != null)
                        RestoreResolution(lastFullscreenDisplay);
                    lastFullscreenDisplay = null;

                    WindowState = WindowState.Normal;
                    WindowBorder = WindowBorder.Resizable;

                    ClientSize = sizeWindowed;
                    Position = new Vector2((float)windowPositionX, (float)windowPositionY);
                    break;
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
                var display = GetCurrentDisplay();

                return new Vector2((float)Location.X / (display.Width - Size.Width),
                    (float)Location.Y / (display.Height - Size.Height));
            }
            set
            {
                var display = GetCurrentDisplay();

                Location = new Point(
                    (int)Math.Round((display.Width - Size.Width) * value.X),
                    (int)Math.Round((display.Height - Size.Height) * value.Y));
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
