// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.Drawing;
using osuTK;
using osuTK.Input;
using osuTK.Platform;
using Icon = osuTK.Icon;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Contains an amalgamation of osuTK's <see cref="IGameWindow"/> and <see cref="INativeWindow"/>.
    /// Will eventually be removed as part of the <see cref="GameWindow"/> refactor.
    /// </summary>
    public interface ILegacyWindow
    {
        #region IGameWindow

        void Run();
        void Run(double updateRate);
        void MakeCurrent();
        void SwapBuffers();
        event EventHandler<EventArgs> Load;
        event EventHandler<EventArgs> Unload;
        event EventHandler<FrameEventArgs> UpdateFrame;
        event EventHandler<FrameEventArgs> RenderFrame;

        #endregion

        #region INativeWindow

        Icon Icon { get; set; }
        string Title { get; set; }
        bool Focused { get; }
        bool Visible { get; set; }
        bool Exists { get; }
        IWindowInfo WindowInfo { get; }
        WindowState WindowState { get; set; }
        WindowBorder WindowBorder { get; set; }
        Rectangle Bounds { get; set; }
        Point Location { get; set; }
        Size Size { get; set; }
        int X { get; set; }
        int Y { get; set; }
        int Width { get; set; }
        int Height { get; set; }
        Rectangle ClientRectangle { get; set; }
        Size ClientSize { get; set; }
        MouseCursor Cursor { get; set; }
        bool CursorVisible { get; set; }
        bool CursorGrabbed { get; set; }
        void Close();
        void ProcessEvents();
        Point PointToClient(Point point);
        Point PointToScreen(Point point);
        event EventHandler<EventArgs> Move;
        event EventHandler<EventArgs> Resize;
        event EventHandler<CancelEventArgs> Closing;
        event EventHandler<EventArgs> Closed;
        event EventHandler<EventArgs> Disposed;
        event EventHandler<EventArgs> IconChanged;
        event EventHandler<EventArgs> TitleChanged;
        event EventHandler<EventArgs> VisibleChanged;
        event EventHandler<EventArgs> FocusedChanged;
        event EventHandler<EventArgs> WindowBorderChanged;
        event EventHandler<EventArgs> WindowStateChanged;
        event EventHandler<KeyboardKeyEventArgs> KeyDown;
        event EventHandler<KeyPressEventArgs> KeyPress;
        event EventHandler<KeyboardKeyEventArgs> KeyUp;
        event EventHandler<EventArgs> MouseLeave;
        event EventHandler<EventArgs> MouseEnter;
        event EventHandler<MouseButtonEventArgs> MouseDown;
        event EventHandler<MouseButtonEventArgs> MouseUp;
        event EventHandler<MouseMoveEventArgs> MouseMove;
        event EventHandler<MouseWheelEventArgs> MouseWheel;
        event EventHandler<FileDropEventArgs> FileDrop;

        #endregion
    }
}
