extern alias IOS;

using System;
using System.ComponentModel;
using OpenTK;
using OpenTK.Input;
using OpenTK.Platform;
using IOS::System.Drawing;
using OpenTK.Platform.iPhoneOS;

namespace osu.Framework.Platform.iOS
{
    public class iOSPlatformGameWindow : IGameWindow
    {
        private readonly iPhoneOSGameView gameView;

        public iOSPlatformGameWindow(iPhoneOSGameView gameView)
        {
            this.gameView = gameView;
        }

        public Icon Icon { get; set; }
        public string Title
        {
            get => gameView.Title;
            set => gameView.Title = value;
        }

        public bool Focused => gameView.Focused;
        public bool Visible
        {
            get => gameView.Visible;
            set => gameView.Visible = value;
        }

        public bool Exists => true;

        public IWindowInfo WindowInfo => gameView.WindowInfo;
        public WindowState WindowState
        {
            get => WindowState.Fullscreen;
            set { }
        }

        public WindowBorder WindowBorder
        {
            get => WindowBorder.Hidden;
            set { }
        }

        public Rectangle Bounds
        {
            get => new Rectangle(0, 0, Width, Height);
            set => gameView.Bounds = value;
        }

        public Point Location
        {
            get => Point.Empty;
            set { }
        }

        public Size Size
        {
            get => gameView.Size;
            set => gameView.Size = value;
        }

        public int X
        {
            get => 0;
            set { }
        }

        public int Y
        {
            get => 0;
            set { }
        }

        public int Width
        {
            get => Size.Width;
            set => Size = new Size(value, Size.Height);
        }

        public int Height
        {
            get => Size.Height;
            set => Size = new Size(Size.Width, value);
        }

        public Rectangle ClientRectangle
        {
            get => new Rectangle(0, 0, Width, Height);
            set => Size = value.Size;
        }

        public Size ClientSize
        {
            get => Size;
            set => Size = value;
        }

        public IInputDriver InputDriver => gameView.InputDriver;

        public MouseCursor Cursor
        {
            get => MouseCursor.Default;
            set { }
        }

        public bool CursorVisible
        {
            get => false;
            set { }
        }

        public event EventHandler<EventArgs> Load;
        public event EventHandler<EventArgs> Unload;
        public event EventHandler<FrameEventArgs> UpdateFrame;
        public event EventHandler<FrameEventArgs> RenderFrame;
        public event EventHandler<EventArgs> Move;
        public event EventHandler<EventArgs> Resize;
        public event EventHandler<CancelEventArgs> Closing;
        public event EventHandler<EventArgs> Closed;
        public event EventHandler<EventArgs> Disposed;
        public event EventHandler<EventArgs> IconChanged;
        public event EventHandler<EventArgs> TitleChanged;
        public event EventHandler<EventArgs> VisibleChanged;
        public event EventHandler<EventArgs> FocusedChanged;
        public event EventHandler<EventArgs> WindowBorderChanged;
        public event EventHandler<EventArgs> WindowStateChanged;
        public event EventHandler<KeyboardKeyEventArgs> KeyDown;
        public event EventHandler<KeyPressEventArgs> KeyPress;
        public event EventHandler<KeyboardKeyEventArgs> KeyUp;
        public event EventHandler<EventArgs> MouseLeave;
        public event EventHandler<EventArgs> MouseEnter;
        public event EventHandler<MouseButtonEventArgs> MouseDown;
        public event EventHandler<MouseButtonEventArgs> MouseUp;
        public event EventHandler<MouseMoveEventArgs> MouseMove;
        public event EventHandler<MouseWheelEventArgs> MouseWheel;

        public void Close()
        {
            gameView.Close();
        }

        public void Dispose()
        {
            gameView.Dispose();
        }

        public void MakeCurrent()
        {
            gameView.MakeCurrent();
        }

        public Point PointToClient(Point point) => point;

        public Point PointToScreen(Point point) => point;

        public void ProcessEvents()
        {
        }

        public void Run()
        {
        }

        public void Run(double updateRate)
        {
        }

        public void SwapBuffers()
        {
            gameView.SwapBuffers();
        }
    }
}
