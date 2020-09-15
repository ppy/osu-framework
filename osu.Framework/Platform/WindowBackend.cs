// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using osu.Framework.Input.StateChanges;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Abstract implementation of <see cref="IWindowBackend"/> that provides default functionality
    /// for window backend subclasses.
    /// </summary>
    public abstract class WindowBackend : IWindowBackend
    {
        public abstract string Title { get; set; }
        public abstract bool Visible { get; set; }
        public abstract Point Position { get; set; }
        public abstract Size Size { get; set; }
        public abstract Size ClientSize { get; }
        public abstract bool CursorVisible { get; set; }
        public abstract bool CursorConfined { get; set; }
        public abstract WindowState WindowState { get; set; }
        public abstract bool Exists { get; protected set; }
        public abstract Display CurrentDisplay { get; set; }
        public abstract DisplayMode CurrentDisplayMode { get; set; }

        public virtual IEnumerable<Display> Displays => Enumerable.Empty<Display>();
        public virtual Display PrimaryDisplay => Displays.First();

        #region Events

        public event Action Update;
        public event Action<Size> Resized;
        public event Action<WindowState> WindowStateChanged;
        public event Func<bool> CloseRequested;
        public event Action Closed;
        public event Action FocusLost;
        public event Action FocusGained;
        public event Action Shown;
        public event Action Hidden;
        public event Action MouseEntered;
        public event Action MouseLeft;
        public event Action<Point> Moved;
        public event Action<MouseScrollRelativeInput> MouseWheel;
        public event Action<MousePositionAbsoluteInput> MouseMove;
        public event Action<MouseButtonInput> MouseDown;
        public event Action<MouseButtonInput> MouseUp;
        public event Action<KeyboardKeyInput> KeyDown;
        public event Action<KeyboardKeyInput> KeyUp;
        public event Action<char> KeyTyped;
        public event Action<string> DragDrop;
        public event Action<Display> DisplayChanged;

        #endregion

        #region Event Invocation

        protected virtual void OnUpdate() => Update?.Invoke();
        protected virtual void OnResized(Size size) => Resized?.Invoke(size);
        protected virtual void OnWindowStateChanged(WindowState windowState) => WindowStateChanged?.Invoke(windowState);
        protected virtual bool OnCloseRequested() => CloseRequested?.Invoke() ?? false;
        protected virtual void OnClosed() => Closed?.Invoke();
        protected virtual void OnFocusLost() => FocusLost?.Invoke();
        protected virtual void OnFocusGained() => FocusGained?.Invoke();
        protected virtual void OnShown() => Shown?.Invoke();
        protected virtual void OnHidden() => Hidden?.Invoke();
        protected virtual void OnMouseEntered() => MouseEntered?.Invoke();
        protected virtual void OnMouseLeft() => MouseLeft?.Invoke();
        protected virtual void OnMoved(Point point) => Moved?.Invoke(point);
        protected virtual void OnMouseWheel(MouseScrollRelativeInput evt) => MouseWheel?.Invoke(evt);
        protected virtual void OnMouseMove(MousePositionAbsoluteInput args) => MouseMove?.Invoke(args);
        protected virtual void OnMouseDown(MouseButtonInput evt) => MouseDown?.Invoke(evt);
        protected virtual void OnMouseUp(MouseButtonInput evt) => MouseUp?.Invoke(evt);
        protected virtual void OnKeyDown(KeyboardKeyInput evt) => KeyDown?.Invoke(evt);
        protected virtual void OnKeyUp(KeyboardKeyInput evt) => KeyUp?.Invoke(evt);
        protected virtual void OnKeyTyped(char c) => KeyTyped?.Invoke(c);
        protected virtual void OnDragDrop(string file) => DragDrop?.Invoke(file);
        protected virtual void OnDisplayChanged(Display display) => DisplayChanged?.Invoke(display);

        #endregion

        public abstract void Create();

        public abstract void Run();

        public abstract void Close();
    }
}
