// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using OpenTK;

namespace osu.Framework.Platform
{
    public abstract class BasicGameWindow : GameWindow
    {
        public BasicGameWindow(int width, int height) : base(width, height)
        {
            Closing += (sender, e) => e.Cancel = ExitRequested();
            Closed += (sender, e) => Exited();
            Cursor = MouseCursor.Empty;
        }

        public void SetTitle(string title)
        {
            Title = title;
        }

        /// <summary>
        /// Return value decides whether we should intercept and cancel this exit (if possible).
        /// </summary>
        public event Func<bool> ExitRequested;

        public event Action Exited;
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Context.MakeCurrent(null);
        }

        protected void OnExited()
        {
            Exited?.Invoke();
        }

        protected bool OnExitRequested()
        {
            return ExitRequested?.Invoke() ?? false;
        }
    }
}
