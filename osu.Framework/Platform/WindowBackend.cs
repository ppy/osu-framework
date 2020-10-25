// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using osu.Framework.Input;
using osuTK;
using osuTK.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
        public abstract IntPtr WindowHandle { get; }

        public virtual IEnumerable<Display> Displays => Enumerable.Empty<Display>();
        public virtual Display PrimaryDisplay => Displays.First();

        public event Action Update;
        public event Action<Size> Resized;
        public event Action<WindowState> WindowStateChanged;
        public event Action CloseRequested;
        public event Action Closed;
        public event Action FocusLost;
        public event Action FocusGained;
        public event Action Shown;
        public event Action Hidden;
        public event Action MouseEntered;
        public event Action MouseLeft;
        public event Action<Point> Moved;
        public event Action<Vector2, bool> MouseWheel;
        public event Action<Vector2> MouseMove;
        public event Action<MouseButton> MouseDown;
        public event Action<MouseButton> MouseUp;
        public event Action<Key> KeyDown;
        public event Action<Key> KeyUp;
        public event Action<char> KeyTyped;
        public event Action<JoystickAxis> JoystickAxisChanged;
        public event Action<JoystickButton> JoystickButtonDown;
        public event Action<JoystickButton> JoystickButtonUp;
        public event Action<string> DragDrop;
        public event Action<Display> DisplayChanged;

        public abstract void Create();

        public abstract void Run();

        public abstract void Close();

        public abstract void RequestClose();

        public abstract void SetIcon(Image<Rgba32> image);

        #region Event Invocation

        protected virtual void OnUpdate() => Update?.Invoke();
        protected virtual void OnResized(Size size) => Resized?.Invoke(size);
        protected virtual void OnWindowStateChanged(WindowState windowState) => WindowStateChanged?.Invoke(windowState);
        protected virtual void OnCloseRequested() => CloseRequested?.Invoke();
        protected virtual void OnClosed() => Closed?.Invoke();
        protected virtual void OnFocusLost() => FocusLost?.Invoke();
        protected virtual void OnFocusGained() => FocusGained?.Invoke();
        protected virtual void OnShown() => Shown?.Invoke();
        protected virtual void OnHidden() => Hidden?.Invoke();
        protected virtual void OnMouseEntered() => MouseEntered?.Invoke();
        protected virtual void OnMouseLeft() => MouseLeft?.Invoke();
        protected virtual void OnMoved(Point point) => Moved?.Invoke(point);
        protected virtual void OnMouseWheel(Vector2 delta, bool precise) => MouseWheel?.Invoke(delta, precise);
        protected virtual void OnMouseMove(Vector2 position) => MouseMove?.Invoke(position);
        protected virtual void OnMouseDown(MouseButton button) => MouseDown?.Invoke(button);
        protected virtual void OnMouseUp(MouseButton button) => MouseUp?.Invoke(button);
        protected virtual void OnKeyDown(Key key) => KeyDown?.Invoke(key);
        protected virtual void OnKeyUp(Key key) => KeyUp?.Invoke(key);
        protected virtual void OnKeyTyped(char c) => KeyTyped?.Invoke(c);
        protected virtual void OnJoystickAxisChanged(JoystickAxis axis) => JoystickAxisChanged?.Invoke(axis);
        protected virtual void OnJoystickButtonDown(JoystickButton button) => JoystickButtonDown?.Invoke(button);
        protected virtual void OnJoystickButtonUp(JoystickButton button) => JoystickButtonUp?.Invoke(button);
        protected virtual void OnDragDrop(string file) => DragDrop?.Invoke(file);
        protected virtual void OnDisplayChanged(Display display) => DisplayChanged?.Invoke(display);

        #endregion
    }
}
