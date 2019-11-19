// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace osu.Framework.Platform
{
    public interface IWindowBackend
    {
        #region Properties

        string Title { get; set; }
        bool Visible { get; set; }
        Vector2 Position { get; set; }
        Vector2 InternalSize { get; set; }
        bool CursorVisible { get; set; }
        bool CursorConfined { get; set; }
        WindowState WindowState { get; set; }

        #endregion

        #region Events

        event Action Update;
        event Action Resized;
        event Func<bool> CloseRequested;
        event Action Closed;
        event Action FocusLost;
        event Action FocusGained;
        event Action Shown;
        event Action Hidden;
        event Action MouseEntered;
        event Action MouseLeft;
        event Action<Point> Moved;
        event Action<MouseWheelEventArgs> MouseWheel;
        event Action<MouseMoveEventArgs> MouseMove;
        event Action<MouseEvent> MouseDown;
        event Action<MouseEvent> MouseUp;
        event Action<KeyEvent> KeyDown;
        event Action<KeyEvent> KeyUp;
        event Action<char> KeyTyped;
        event Action<DragDropEvent> DragDrop;

        #endregion

        #region Methods

        void Create();
        void Run();
        void Close();

        #endregion
    }
}
