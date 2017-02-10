// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.Platform;
using OpenTK;
using System;
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

        public override Vector2 Position
        {
            set
            {
                Location = new System.Drawing.Point(
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
                switch (value)
                {
                    case WindowMode.Windowed:
                        WindowState = WindowState.Normal;
                        break;
                    case WindowMode.Borderless:
                        WindowState = WindowState.Normal;
                        break;
                    case WindowMode.Fullscreen:
                        WindowState = WindowState.Fullscreen;
                        break;
                }
            }

            get
            {
                switch (WindowState)
                {
                    case WindowState.Normal:
                        return WindowMode.Windowed;
                    case WindowState.Fullscreen:
                        return WindowMode.Fullscreen;
                    default:
                        return WindowMode.Windowed;
                }
            }
        }

        public void OnDragEnter(DragEventArgs e) => DragEnter?.Invoke(e);

        public void OnDragLeave(EventArgs e) => DragLeave?.Invoke(e);

        public void OnDragDrop(DragEventArgs e) => DragDrop?.Invoke(e);

        public void OnDragOver(DragEventArgs e) => DragOver?.Invoke(e);
    }
}
