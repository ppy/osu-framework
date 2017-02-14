// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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

        public override void CentreToScreen()
        {
            base.CentreToScreen();
            Location = new System.Drawing.Point(
                (DisplayDevice.Default.Width - Size.Width) / 2,
                (DisplayDevice.Default.Height - Size.Height) / 2
            );
        }

        public void OnDragEnter(DragEventArgs e) => DragEnter?.Invoke(e);

        public void OnDragLeave(EventArgs e) => DragLeave?.Invoke(e);

        public void OnDragDrop(DragEventArgs e) => DragDrop?.Invoke(e);

        public void OnDragOver(DragEventArgs e) => DragOver?.Invoke(e);
    }
}
