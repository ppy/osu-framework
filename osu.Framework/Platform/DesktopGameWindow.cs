// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.IO;
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

        public override DisplayDevice GetCurrentDisplay() => DisplayDevice.Default;

        protected DesktopGameWindow()
            : base(default_width, default_height)
        {
            Resize += OnResize;
            Move += OnMove;
        }

        public virtual void SetIconFromStream(Stream stream) { }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);

            sizeFullscreen.ValueChanged += newSize =>
            {
                if (WindowState == WindowState.Fullscreen)
                    changeResolution(newSize);
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

        private void changeResolution(Size newSize)
        {
            var currentDisplay = DisplayDevice.Default;

            if (newSize.Width == currentDisplay.Width && newSize.Height == currentDisplay.Height)
                return;

            DisplayResolution newResolution = currentDisplay.SelectResolution(
                newSize.Width,
                newSize.Height,
                currentDisplay.BitsPerPixel,
                currentDisplay.RefreshRate
            );

            if (newResolution.Width == currentDisplay.Width && newResolution.Height == currentDisplay.Height)
            {
                // we wanted a new resolution but got the old one, which means OpenTK didn't find this resolution
                currentDisplay.RestoreResolution();
            }
            else
            {
                currentDisplay.ChangeResolution(newResolution);
                ClientSize = newSize;
            }
        }

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

        private void windowMode_ValueChanged(WindowMode newMode)
        {
            switch (newMode)
            {
                case Configuration.WindowMode.Fullscreen:
                    changeResolution(sizeFullscreen);

                    WindowState = WindowState.Fullscreen;
                    break;
                case Configuration.WindowMode.Borderless:
                    DisplayDevice.Default.RestoreResolution();

                    WindowState = WindowState.Maximized;
                    WindowBorder = WindowBorder.Hidden;

                    //must add 1 to enter borderless
                    ClientSize = new Size(DisplayDevice.Default.Bounds.Width + 1, DisplayDevice.Default.Bounds.Height + 1);
                    Position = Vector2.Zero;
                    break;
                default:
                    DisplayDevice.Default.RestoreResolution();

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

            DisplayDevice.Default.RestoreResolution();
        }

        public Vector2 Position
        {
            get
            {
                return new Vector2((float)Location.X / (DisplayDevice.Default.Width - Size.Width),
                    (float)Location.Y / (DisplayDevice.Default.Height - Size.Height));
            }
            set
            {
                Location = new Point(
                    (int)Math.Round((DisplayDevice.Default.Width - Size.Width) * value.X),
                    (int)Math.Round((DisplayDevice.Default.Height - Size.Height) * value.Y));
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
