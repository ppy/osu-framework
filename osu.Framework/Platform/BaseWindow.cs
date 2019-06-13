// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Input;
using osuTK.Platform;
using Icon = osuTK.Icon;

namespace osu.Framework.Platform
{
    public abstract class BaseWindow : IWindow
    {
        #region IWindow

        public virtual void CycleMode()
        {
            var currentValue = WindowMode.Value;

            do
            {
                switch (currentValue)
                {
                    case Configuration.WindowMode.Windowed:
                        currentValue = Configuration.WindowMode.Borderless;
                        break;

                    case Configuration.WindowMode.Borderless:
                        currentValue = Configuration.WindowMode.Fullscreen;
                        break;

                    case Configuration.WindowMode.Fullscreen:
                        currentValue = Configuration.WindowMode.Windowed;
                        break;
                }
            } while (!SupportedWindowModes.Contains(currentValue) && currentValue != WindowMode.Value);

            WindowMode.Value = currentValue;
        }

        public abstract void SetupWindow(FrameworkConfigManager config);

        [CanBeNull]
        public event Func<bool> ExitRequested;

        [CanBeNull]
        public event Action Exited;

        public virtual bool CursorInWindow { get; protected set; }

        public abstract CursorState CursorState { get; set; }

        public virtual VSyncMode VSync { get; set; }

        public abstract WindowMode DefaultWindowMode { get; }

        public virtual Bindable<WindowMode> WindowMode { get; } = new Bindable<WindowMode>();

        public abstract DisplayDevice CurrentDisplay { get; protected set; }

        public virtual IBindable<bool> IsActive { get; } = new Bindable<bool>();

        public virtual IBindable<MarginPadding> SafeAreaPadding { get; } = new BindableMarginPadding();

        public virtual IBindableList<WindowMode> SupportedWindowModes { get; } = new BindableList<WindowMode>();

        public virtual IEnumerable<DisplayResolution> AvailableResolutions => Enumerable.Empty<DisplayResolution>();

        #endregion

        #region Event Invocation

        protected virtual bool OnExitRequested() => ExitRequested?.Invoke() ?? false;

        protected virtual void OnExited() => Exited?.Invoke();

        #endregion

        #region ILegacyWindow

        public abstract void Run();
        public abstract void Run(double updateRate);
        public abstract void MakeCurrent();
        public abstract void SwapBuffers();
        public abstract event EventHandler<EventArgs> Load;
        public abstract event EventHandler<EventArgs> Unload;
        public abstract event EventHandler<FrameEventArgs> UpdateFrame;
        public abstract event EventHandler<FrameEventArgs> RenderFrame;
        public abstract Icon Icon { get; set; }
        public abstract string Title { get; set; }
        public abstract bool Focused { get; }
        public abstract bool Visible { get; set; }
        public abstract bool Exists { get; }
        public abstract IWindowInfo WindowInfo { get; }
        public abstract WindowState WindowState { get; set; }
        public abstract WindowBorder WindowBorder { get; set; }
        public abstract Rectangle Bounds { get; set; }
        public abstract Point Location { get; set; }
        public abstract Size Size { get; set; }
        public abstract int X { get; set; }
        public abstract int Y { get; set; }
        public abstract int Width { get; set; }
        public abstract int Height { get; set; }
        public abstract Rectangle ClientRectangle { get; set; }
        public abstract Size ClientSize { get; set; }
        public abstract void Close();
        public abstract void ProcessEvents();
        public abstract Point PointToClient(Point point);
        public abstract Point PointToScreen(Point point);
        public abstract event EventHandler<EventArgs> Move;
        public abstract event EventHandler<EventArgs> Resize;
        public abstract event EventHandler<CancelEventArgs> Closing;
        public abstract event EventHandler<EventArgs> Closed;
        public abstract event EventHandler<EventArgs> Disposed;
        public abstract event EventHandler<EventArgs> IconChanged;
        public abstract event EventHandler<EventArgs> TitleChanged;
        public abstract event EventHandler<EventArgs> VisibleChanged;
        public abstract event EventHandler<EventArgs> FocusedChanged;
        public abstract event EventHandler<EventArgs> WindowBorderChanged;
        public abstract event EventHandler<EventArgs> WindowStateChanged;
        public abstract event EventHandler<KeyboardKeyEventArgs> KeyDown;
        public abstract event EventHandler<KeyPressEventArgs> KeyPress;
        public abstract event EventHandler<KeyboardKeyEventArgs> KeyUp;
        public abstract event EventHandler<EventArgs> MouseLeave;
        public abstract event EventHandler<EventArgs> MouseEnter;
        public abstract event EventHandler<MouseButtonEventArgs> MouseDown;
        public abstract event EventHandler<MouseButtonEventArgs> MouseUp;
        public abstract event EventHandler<MouseMoveEventArgs> MouseMove;
        public abstract event EventHandler<MouseWheelEventArgs> MouseWheel;
        public abstract event EventHandler<FileDropEventArgs> FileDrop;

        #endregion

        #region IDisposable

        private bool isDisposed;

        protected virtual void Dispose(bool disposing) => isDisposed = true;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseWindow()
        {
            Dispose(false);
        }

        #endregion
    }
}
