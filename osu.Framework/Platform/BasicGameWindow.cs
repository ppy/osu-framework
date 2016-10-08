// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;

namespace osu.Framework.Platform
{
    public abstract class BasicGameWindow
    {
        public event EventHandler ClientSizeChanged;
        public event EventHandler ScreenDeviceNameChanged;
        public event EventHandler Activated;
        public event EventHandler Deactivated;
        public event EventHandler Paint;

        //todo: remove the need for this.
        public BasicGameForm Form { get; protected set; }

        /// <summary>
        /// Return value decides whether we should intercept and cancel this exit (if possible).
        /// </summary>
        public event Func<bool> ExitRequested;

        public event Action Exited;

        public abstract Rectangle ClientBounds { get; }
        public abstract IntPtr Handle { get; }
        public abstract bool IsMinimized { get; }

        public abstract Size Size { get; set; }

        public abstract void Close();

        private string title;

        public string Title
        {
            get { return title; }
            set
            {
                if (value == null || title == value)
                    return;

                SetTitle(title = value);
            }
        }

        protected void OnActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        protected void OnClientSizeChanged()
        {
            ClientSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void OnExited()
        {
            Exited?.Invoke();
        }

        protected bool OnExitRequested()
        {
            return ExitRequested?.Invoke() ?? false;
        }

        protected void OnDeactivated()
        {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }

        protected void OnPaint()
        {
            Paint?.Invoke(this, EventArgs.Empty);
        }

        protected void OnScreenDeviceNameChanged()
        {
            ScreenDeviceNameChanged?.Invoke(this, EventArgs.Empty);
        }

        protected abstract void SetTitle(string title);
    }
}
