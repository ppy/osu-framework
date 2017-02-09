// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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

        public override bool Fullscreen
        {
            set
            {
                WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
            }

            get
            {
                return WindowState == WindowState.Fullscreen;
            }
        }

        public override bool Maximized
        {
            set
            {
                if (WindowState != WindowState.Fullscreen)
                {
                    WindowState = value ? WindowState.Maximized : WindowState.Normal;
                }
            }

            get
            {
                return WindowState == WindowState.Maximized;
            }
        }

        public void OnDragEnter(DragEventArgs e) => DragEnter?.Invoke(e);

        public void OnDragLeave(EventArgs e) => DragLeave?.Invoke(e);

        public void OnDragDrop(DragEventArgs e) => DragDrop?.Invoke(e);

        public void OnDragOver(DragEventArgs e) => DragOver?.Invoke(e);
    }
}
