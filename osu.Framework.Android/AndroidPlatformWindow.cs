using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using osuTK;
using osuTK.Input;
using osuTK.Platform;

namespace osu.Framework.Android
{
    public class AndroidPlatformWindow : IGameWindow
    {
        private readonly AndroidGameView gameView;

        public AndroidPlatformWindow(AndroidGameView gameView)
        {
            this.gameView = gameView;

            gameView.Load += (o, e) => Load?.Invoke(o, e);
            gameView.Unload += (o, e) => Unload?.Invoke(o, e);
            gameView.UpdateFrame += (o, e) => UpdateFrame?.Invoke(o, e);
            gameView.RenderFrame += (o, e) => RenderFrame?.Invoke(o, e);
            gameView.Resize += (o, e) => Resize?.Invoke(o, e);
            gameView.Closed += (o, e) => Closed?.Invoke(o, e);
            gameView.Disposed += (o, e) => Disposed?.Invoke(o, e);
            gameView.TitleChanged += (o, e) => TitleChanged?.Invoke(o, e);
            gameView.VisibleChanged += (o, e) => VisibleChanged?.Invoke(o, e);
            gameView.WindowStateChanged += (o, e) => WindowStateChanged?.Invoke(o, e);
        }
        // We cannot have titles.
        public string Title { get => gameView.Title; set { } }

        // Always set this to true.
        public bool Focused => true;

        public bool Visible { get => gameView.Visible; set { } }

        public bool Exists => true;

        public IWindowInfo WindowInfo => gameView.WindowInfo;

        public WindowState WindowState { get => WindowState.Fullscreen; set { } }
        public WindowBorder WindowBorder { get => WindowBorder.Hidden; set { } }
        public Rectangle Bounds { get => new Rectangle(0, 0, Width, Height); set { } }
        public Point Location { get => Point.Empty; set { } }
        public Size Size { get => new Size((int)(gameView.Size.Width * gameView.ScaleX), (int)(gameView.Height * gameView.ScaleY)); set { } }
        public int X { get => 0; set { } }
        public int Y { get => 0; set { } }
        public int Width { get => Size.Width; set { } }
        public int Height { get => Size.Height; set { } }
        public Rectangle ClientRectangle { get => new Rectangle(0, 0, Width, Height); set { } }
        public Size ClientSize { get => Size; set { } }
        public MouseCursor Cursor { get => MouseCursor.Default; set { } }
        public bool CursorVisible { get => false; set { } }
        public bool CursorGrabbed { get => true; set { } }

        public Icon Icon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
        public event EventHandler<FileDropEventArgs> FileDrop;

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

        public Point PointToClient(Point point)
        {
            return point;
        }

        public Point PointToScreen(Point point)
        {
            return point;
        }

        public void ProcessEvents()
        {
            gameView.ProcessEvents();
        }

        public void Run()
        {
            new Thread(() =>
            {
                while (true)
                {
                    gameView.Handler.Post(() =>
                    {
                        RenderFrame?.Invoke(this, new FrameEventArgs() { });
                        SwapBuffers();
                    });
                    UpdateFrame?.Invoke(this, new FrameEventArgs() { });

                    Thread.Sleep(100);
                }
            });
            gameView.Run();
        }

        public void Run(double updateRate)
        {
            gameView.Run(updateRate);
        }

        public void SwapBuffers()
        {
            gameView.SwapBuffers();
        }
    }
}
