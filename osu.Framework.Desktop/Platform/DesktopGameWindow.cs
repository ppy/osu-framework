// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.Windows.Forms;
using osu.Framework.Configuration;
using osu.Framework.Input;
using OpenTK;
using GameWindow = osu.Framework.Platform.GameWindow;

namespace osu.Framework.Desktop.Platform
{
    public class DesktopGameWindow : GameWindow, IDropTarget
    {
        private const int default_width = 1366;
        private const int default_height = 768;

        public event Action<DragEventArgs> DragEnter;
        public event Action<EventArgs> DragLeave;
        public event Action<DragEventArgs> DragDrop;
        public event Action<DragEventArgs> DragOver;

        private readonly BindableInt widthFullscreen = new BindableInt();
        private readonly BindableInt heightFullscreen = new BindableInt();
        private readonly BindableInt width = new BindableInt();
        private readonly BindableInt height = new BindableInt();

        private readonly BindableDouble windowPositionX = new BindableDouble();
        private readonly BindableDouble windowPositionY = new BindableDouble();

        private readonly Bindable<WindowMode> windowMode = new Bindable<WindowMode>();

        private readonly Bindable<ConfineMouseMode> confineMouseMode = new Bindable<ConfineMouseMode>();

        public DesktopGameWindow()
            : base(default_width, default_height)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            config.BindWith(FrameworkSetting.WidthFullscreen, widthFullscreen);
            config.BindWith(FrameworkSetting.HeightFullscreen, heightFullscreen);

            config.BindWith(FrameworkSetting.Width, width);
            config.BindWith(FrameworkSetting.Height, height);

            config.BindWith(FrameworkSetting.WindowedPositionX, windowPositionX);
            config.BindWith(FrameworkSetting.WindowedPositionY, windowPositionY);

            config.BindWith(FrameworkSetting.ConfineMouseMode, confineMouseMode);

            confineMouseMode.ValueChanged += confineMouseMode_ValueChanged;
            confineMouseMode.TriggerChange();

            config.BindWith(FrameworkSetting.WindowMode, windowMode);

            windowMode.ValueChanged += windowMode_ValueChanged;
            windowMode.TriggerChange();

            Exited += onExit;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            switch (windowMode.Value)
            {
                case WindowMode.Windowed:
                    width.Value = ClientSize.Width;
                    height.Value = ClientSize.Height;
                    break;
            }
        }

        private void confineMouseMode_ValueChanged(ConfineMouseMode newValue)
        {
            bool confine = false;

            switch (newValue)
            {
                case ConfineMouseMode.Fullscreen:
                    confine = windowMode.Value != WindowMode.Windowed;
                    break;
                case ConfineMouseMode.Always:
                    confine = true;
                    break;
            }

            if (confine)
                CursorState |= Framework.Platform.CursorState.Confined;
            else
                CursorState &= ~Framework.Platform.CursorState.Confined;
        }

        private void windowMode_ValueChanged(WindowMode newMode)
        {
            switch (newMode)
            {
                case WindowMode.Fullscreen:
                    DisplayResolution newResolution = DisplayDevice.Default.SelectResolution(widthFullscreen, heightFullscreen, 0, DisplayDevice.Default.RefreshRate);
                    DisplayDevice.Default.ChangeResolution(newResolution);

                    WindowState = WindowState.Fullscreen;
                    break;
                case WindowMode.Borderless:
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

                    ClientSize = new Size(width, height);
                    Position = new Vector2((float)windowPositionX, (float)windowPositionY);
                    break;
            }

            confineMouseMode.TriggerChange();
        }

        private void onExit()
        {
            switch (windowMode.Value)
            {
                case WindowMode.Fullscreen:
                    widthFullscreen.Value = ClientSize.Width;
                    heightFullscreen.Value = ClientSize.Height;
                    break;
                case WindowMode.Windowed:
                    windowPositionX.Value = Position.X;
                    windowPositionY.Value = Position.Y;
                    break;
            }

            DisplayDevice.Default.RestoreResolution();
        }

        public override Vector2 Position
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
            switch (windowMode.Value)
            {
                case WindowMode.Windowed:
                    windowMode.Value = WindowMode.Borderless;
                    break;
                case WindowMode.Borderless:
                    windowMode.Value = WindowMode.Fullscreen;
                    break;
                default:
                    windowMode.Value = WindowMode.Windowed;
                    break;
            }
        }

        public void OnDragEnter(DragEventArgs e) => DragEnter?.Invoke(e);

        public void OnDragLeave(EventArgs e) => DragLeave?.Invoke(e);

        public void OnDragDrop(DragEventArgs e) => DragDrop?.Invoke(e);

        public void OnDragOver(DragEventArgs e) => DragOver?.Invoke(e);
    }
}
