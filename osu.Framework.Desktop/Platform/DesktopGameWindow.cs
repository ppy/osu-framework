// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.Platform;
using OpenTK;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace osu.Framework.Desktop.Platform
{
    public class DesktopGameWindow : BasicGameWindow, IDropTarget
    {
        private const int default_width = 1366;
        private const int default_height = 768;

        public event Action<DragEventArgs> DragEnter;
        public event Action<EventArgs> DragLeave;
        public event Action<DragEventArgs> DragDrop;
        public event Action<DragEventArgs> DragOver;

        public DesktopGameWindow() : base(default_width, default_height)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            base.SetupWindow(config);

            Exited += SaveWindow;

            CurrentWindowMode = Config.Get<WindowMode>(FrameworkConfig.WindowMode);

            Config.GetBindable<WindowMode>(FrameworkConfig.WindowMode).ValueChanged += delegate (object sender, EventArgs e)
                {
                    CurrentWindowMode = ((Bindable<WindowMode>)sender).Value;
                };
        }

        public virtual void SaveWindow()
        {
            if (CurrentWindowMode == WindowMode.Fullscreen)
            {
                Config.Set(FrameworkConfig.WidthFullscreen, ClientSize.Width);
                Config.Set(FrameworkConfig.HeightFullscreen, ClientSize.Height);
            }
            else
            {
                Config.Set(FrameworkConfig.Width, ClientSize.Width);
                Config.Set(FrameworkConfig.Height, ClientSize.Height);

                Config.Set<double>(FrameworkConfig.WindowedPositionX, Position.X);
                Config.Set<double>(FrameworkConfig.WindowedPositionY, Position.Y);
            }

            DisplayDevice.Default.RestoreResolution();
        }

        public override Vector2 Position
        {
            set
            {
                Location = new Point(
                    (int)Math.Round((DisplayDevice.Default.Width - Size.Width) * value.X),
                    (int)Math.Round((DisplayDevice.Default.Height - Size.Height) * value.Y));
            }

            get
            {
                return new Vector2((float) Location.X / (DisplayDevice.Default.Width - Size.Width),
                                   (float) Location.Y / (DisplayDevice.Default.Height - Size.Height));
            }
        }

        public override WindowMode CurrentWindowMode
        {
            set
            {
                if (value == WindowMode.Fullscreen)
                {
                    WindowState = WindowState.Normal;
                    DisplayResolution newResolution =
                        DisplayDevice.Default.SelectResolution(Config.Get<int>(FrameworkConfig.WidthFullscreen),
                                                               Config.Get<int>(FrameworkConfig.HeightFullscreen),
                                                               0, 0);
                    DisplayDevice.Default.ChangeResolution(newResolution);
                    WindowState = WindowState.Fullscreen;
                }
                else
                {
                    WindowState = value == WindowMode.Windowed ? WindowState.Normal : WindowState.Fullscreen;
                    DisplayDevice.Default.RestoreResolution();
                    ClientSize = new Size(Config.Get<int>(FrameworkConfig.Width), Config.Get<int>(FrameworkConfig.Height));
                    Position = new Vector2((float)Config.Get<double>(FrameworkConfig.WindowedPositionX),
                                           (float)Config.Get<double>(FrameworkConfig.WindowedPositionY));
                }
            }

            get
            {
                if (WindowState == WindowState.Normal)
                {
                    return WindowMode.Windowed;
                }
                else
                {
                    if (DisplayDevice.Default.Width == ClientSize.Width && DisplayDevice.Default.Height == ClientSize.Height)
                    {
                        return WindowMode.Fullscreen;
                    }

                    return WindowMode.Borderless;
                }
            }
        }

        public override void ToggleFullscreen()
        {
            if (CurrentWindowMode == WindowMode.Windowed)
            {
                Config.Set(FrameworkConfig.WindowMode, WindowMode.Borderless);
            }
            else if (CurrentWindowMode == WindowMode.Borderless)
            {
                Config.Set(FrameworkConfig.WindowMode, WindowMode.Fullscreen);
            }
            else
            {
                Config.Set(FrameworkConfig.WindowMode, WindowMode.Windowed);
            }
        }

        public void OnDragEnter(DragEventArgs e) => DragEnter?.Invoke(e);

        public void OnDragLeave(EventArgs e) => DragLeave?.Invoke(e);

        public void OnDragDrop(DragEventArgs e) => DragDrop?.Invoke(e);

        public void OnDragOver(DragEventArgs e) => DragOver?.Invoke(e);
    }
}
