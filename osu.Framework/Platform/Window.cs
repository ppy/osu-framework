// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Input;
using osuTK;
using osuTK.Input;
using osuTK.Platform;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Implementation of <see cref="IWindow"/> that provides bindables and
    /// delegates responsibility to window and graphics backends.
    /// </summary>
    public abstract class Window : IWindow
    {
        protected readonly IWindowBackend WindowBackend;
        protected readonly IGraphicsBackend GraphicsBackend;

        #region Properties

        /// <summary>
        /// Gets and sets the window title.
        /// </summary>
        public string Title
        {
            get => WindowBackend.Title;
            set => WindowBackend.Title = value;
        }

        /// <summary>
        /// Enables or disables vertical sync.
        /// </summary>
        public bool VerticalSync
        {
            get => GraphicsBackend.VerticalSync;
            set => GraphicsBackend.VerticalSync = value;
        }

        /// <summary>
        /// Returns true if window has been created.
        /// Returns false if the window has not yet been created, or has been closed.
        /// </summary>
        public bool Exists => WindowBackend.Exists;

        /// <summary>
        /// Enables or disables relative mouse mode.
        /// While relative mouse mode is disabled, <see cref="MouseMove"/> events will be fired.
        /// While relative mouse mode is enabled, <see cref="MouseMoveRelative"/> events will be fired.
        /// </summary>
        public bool RelativeMouseMode
        {
            get => WindowBackend.RelativeMouseMode;
            set => WindowBackend.RelativeMouseMode = value;
        }

        public Display PrimaryDisplay => WindowBackend.PrimaryDisplay;

        public DisplayMode CurrentDisplayMode => WindowBackend.CurrentDisplayMode;

        public IEnumerable<Display> Displays => WindowBackend.Displays;

        public WindowMode DefaultWindowMode => Configuration.WindowMode.Windowed;

        /// <summary>
        /// Returns the window modes that the platform should support by default.
        /// </summary>
        protected virtual IEnumerable<WindowMode> DefaultSupportedWindowModes => Enum.GetValues(typeof(WindowMode)).OfType<WindowMode>();

        #endregion

        #region Mutable Bindables

        /// <summary>
        /// Provides a bindable that controls the window's position.
        /// </summary>
        public Bindable<Point> Position { get; } = new Bindable<Point>();

        /// <summary>
        /// Provides a bindable that controls the window's unscaled internal size.
        /// </summary>
        public Bindable<Size> Size { get; } = new BindableSize();

        /// <summary>
        /// Provides a bindable that controls the window's <see cref="WindowState"/>.
        /// </summary>
        public Bindable<WindowState> WindowState { get; } = new Bindable<WindowState>();

        /// <summary>
        /// Provides a bindable that controls the window's <see cref="CursorState"/>.
        /// </summary>
        public Bindable<CursorState> CursorState { get; } = new Bindable<CursorState>();

        /// <summary>
        /// Provides a bindable that controls the window's visibility.
        /// </summary>
        public Bindable<bool> Visible { get; } = new BindableBool();

        public Bindable<Display> CurrentDisplay { get; } = new Bindable<Display>();

        public Bindable<WindowMode> WindowMode { get; } = new Bindable<WindowMode>();

        #endregion

        #region Immutable Bindables

        private readonly BindableBool isActive = new BindableBool(true);

        public IBindable<bool> IsActive => isActive;

        private readonly BindableBool focused = new BindableBool();

        /// <summary>
        /// Provides a read-only bindable that monitors the window's focused state.
        /// </summary>
        public IBindable<bool> Focused => focused;

        private readonly BindableBool cursorInWindow = new BindableBool();

        /// <summary>
        /// Provides a read-only bindable that monitors the whether the cursor is in the window.
        /// </summary>
        public IBindable<bool> CursorInWindow => cursorInWindow;

        public IBindableList<WindowMode> SupportedWindowModes { get; }

        public BindableSafeArea SafeAreaPadding { get; } = new BindableSafeArea();

        #endregion

        #region Events

        /// <summary>
        /// Invoked once every window event loop.
        /// </summary>
        public event Action Update;

        /// <summary>
        /// Invoked after the window has resized.
        /// </summary>
        public event Action Resized;

        /// <summary>
        /// Invoked when the user attempts to close the window.
        /// </summary>
        public event Func<bool> ExitRequested;

        /// <summary>
        /// Invoked when the window is about to close.
        /// </summary>
        public event Action Exited;

        /// <summary>
        /// Invoked when the window loses focus.
        /// </summary>
        public event Action FocusLost;

        /// <summary>
        /// Invoked when the window gains focus.
        /// </summary>
        public event Action FocusGained;

        /// <summary>
        /// Invoked when the window becomes visible.
        /// </summary>
        public event Action Shown;

        /// <summary>
        /// Invoked when the window becomes invisible.
        /// </summary>
        public event Action Hidden;

        /// <summary>
        /// Invoked when the mouse cursor enters the window.
        /// </summary>
        public event Action MouseEntered;

        /// <summary>
        /// Invoked when the mouse cursor leaves the window.
        /// </summary>
        public event Action MouseLeft;

        /// <summary>
        /// Invoked when the window moves.
        /// </summary>
        public event Action<Point> Moved;

        /// <summary>
        /// Invoked when the user scrolls the mouse wheel over the window.
        /// </summary>
        public event Action<Vector2, bool> MouseWheel;

        /// <summary>
        /// Invoked when the user moves the mouse cursor, if relative mouse mode is disabled.
        /// The <see cref="Vector2"/> provided is the position relative to the top left corner of the window,
        /// with DPI scaling applied.
        /// </summary>
        public event Action<Vector2> MouseMove;

        /// <summary>
        /// Invoked when the user moves the mouse cursor, if relative mouse mode is enabled.
        /// The <see cref="Vector2"/> provided is the number of pixels the cursor has moved since the previous
        /// <see cref="MouseMoveRelative"/> invocation.
        /// </summary>
        public event Action<Vector2> MouseMoveRelative;

        /// <summary>
        /// Invoked when the user presses a mouse button.
        /// </summary>
        public event Action<MouseButton> MouseDown;

        /// <summary>
        /// Invoked when the user releases a mouse button.
        /// </summary>
        public event Action<MouseButton> MouseUp;

        /// <summary>
        /// Invoked when the user presses a key.
        /// </summary>
        public event Action<Key> KeyDown;

        /// <summary>
        /// Invoked when the user releases a key.
        /// </summary>
        public event Action<Key> KeyUp;

        /// <summary>
        /// Invoked when the user types a character.
        /// </summary>
        public event Action<char> KeyTyped;

        /// <summary>
        /// Invoked when a joystick axis changes.
        /// </summary>
        public event Action<JoystickAxis> JoystickAxisChanged;

        /// <summary>
        /// Invoked when the user presses a button on a joystick.
        /// </summary>
        public event Action<JoystickButton> JoystickButtonDown;

        /// <summary>
        /// Invoked when the user releases a button on a joystick.
        /// </summary>
        public event Action<JoystickButton> JoystickButtonUp;

        /// <summary>
        /// Invoked when the user drops a file into the window.
        /// </summary>
        public event Action<string> DragDrop;

        #endregion

        #region Event Invocation

        protected virtual void OnUpdate() => Update?.Invoke();
        protected virtual void OnResized() => Resized?.Invoke();
        protected virtual bool OnExitRequested() => ExitRequested?.Invoke() ?? false;
        protected virtual void OnExited() => Exited?.Invoke();
        protected virtual void OnFocusLost() => FocusLost?.Invoke();
        protected virtual void OnFocusGained() => FocusGained?.Invoke();
        protected virtual void OnShown() => Shown?.Invoke();
        protected virtual void OnHidden() => Hidden?.Invoke();
        protected virtual void OnMouseEntered() => MouseEntered?.Invoke();
        protected virtual void OnMouseLeft() => MouseLeft?.Invoke();
        protected virtual void OnMoved(Point point) => Moved?.Invoke(point);
        protected virtual void OnMouseWheel(Vector2 delta, bool precise) => MouseWheel?.Invoke(delta, precise);
        protected virtual void OnMouseMove(Vector2 position) => MouseMove?.Invoke(position);
        protected virtual void OnMouseMoveRelative(Vector2 delta) => MouseMoveRelative?.Invoke(delta);
        protected virtual void OnMouseDown(MouseButton button) => MouseDown?.Invoke(button);
        protected virtual void OnMouseUp(MouseButton button) => MouseUp?.Invoke(button);
        protected virtual void OnKeyDown(Key key) => KeyDown?.Invoke(key);
        protected virtual void OnKeyUp(Key key) => KeyUp?.Invoke(key);
        protected virtual void OnKeyTyped(char c) => KeyTyped?.Invoke(c);
        protected virtual void OnJoystickAxisChanged(JoystickAxis axis) => JoystickAxisChanged?.Invoke(axis);
        protected virtual void OnJoystickButtonDown(JoystickButton button) => JoystickButtonDown?.Invoke(button);
        protected virtual void OnJoystickButtonUp(JoystickButton button) => JoystickButtonUp?.Invoke(button);
        protected virtual void OnDragDrop(string file) => DragDrop?.Invoke(file);

        #endregion

        /// <summary>
        /// Creates an instance of <see cref="IWindowBackend"/> for the platform.
        /// </summary>
        protected abstract IWindowBackend CreateWindowBackend();

        /// <summary>
        /// Creates an instance of <see cref="IGraphicsBackend"/> for the platform.
        /// </summary>
        protected abstract IGraphicsBackend CreateGraphicsBackend();

        protected Window()
        {
            WindowBackend = CreateWindowBackend();
            GraphicsBackend = CreateGraphicsBackend();

            SupportedWindowModes = new BindableList<WindowMode>(DefaultSupportedWindowModes);

            Position.ValueChanged += position_ValueChanged;
            Size.ValueChanged += size_ValueChanged;

            CursorState.ValueChanged += evt =>
            {
                WindowBackend.CursorVisible = !evt.NewValue.HasFlag(Platform.CursorState.Hidden);
                WindowBackend.CursorConfined = evt.NewValue.HasFlag(Platform.CursorState.Confined);
            };

            WindowState.ValueChanged += evt => WindowBackend.WindowState = evt.NewValue;

            Visible.ValueChanged += visible_ValueChanged;

            focused.ValueChanged += evt =>
            {
                isActive.Value = evt.NewValue;

                if (evt.NewValue)
                    OnFocusGained();
                else
                    OnFocusLost();
            };

            cursorInWindow.ValueChanged += evt =>
            {
                if (evt.NewValue)
                    OnMouseEntered();
                else
                    OnMouseLeft();
            };
        }

        /// <summary>
        /// Starts the window's run loop.
        /// </summary>
        public void Run() => WindowBackend.Run();

        /// <summary>
        /// Attempts to close the window.
        /// </summary>
        public void Close() => WindowBackend.RequestClose();

        /// <summary>
        /// Creates the concrete window implementation and initialises the graphics backend.
        /// </summary>
        public void Create()
        {
            WindowBackend.Create();

            WindowBackend.Resized += windowBackend_Resized;
            WindowBackend.WindowStateChanged += windowBackend_WindowStateChanged;
            WindowBackend.Moved += windowBackend_Moved;
            WindowBackend.Hidden += () => Visible.Value = false;
            WindowBackend.Shown += () => Visible.Value = true;

            WindowBackend.FocusGained += () => focused.Value = true;
            WindowBackend.FocusLost += () => focused.Value = false;
            WindowBackend.MouseEntered += () => cursorInWindow.Value = true;
            WindowBackend.MouseLeft += () => cursorInWindow.Value = false;

            WindowBackend.Closed += OnExited;
            WindowBackend.CloseRequested += handleCloseRequested;
            WindowBackend.Update += OnUpdate;
            WindowBackend.KeyDown += OnKeyDown;
            WindowBackend.KeyUp += OnKeyUp;
            WindowBackend.KeyTyped += OnKeyTyped;
            WindowBackend.JoystickAxisChanged += OnJoystickAxisChanged;
            WindowBackend.JoystickButtonDown += OnJoystickButtonDown;
            WindowBackend.JoystickButtonUp += OnJoystickButtonUp;
            WindowBackend.MouseDown += OnMouseDown;
            WindowBackend.MouseUp += OnMouseUp;
            WindowBackend.MouseMove += OnMouseMove;
            WindowBackend.MouseMoveRelative += OnMouseMoveRelative;
            WindowBackend.MouseWheel += OnMouseWheel;
            WindowBackend.DragDrop += OnDragDrop;

            WindowBackend.DisplayChanged += d => CurrentDisplay.Value = d;

            GraphicsBackend.Initialise(WindowBackend);

            CurrentDisplay.Value = WindowBackend.CurrentDisplay;
            CurrentDisplay.ValueChanged += evt => WindowBackend.CurrentDisplay = evt.NewValue;
        }

        /// <summary>
        /// Requests that the graphics backend perform a buffer swap.
        /// </summary>
        public void SwapBuffers() => GraphicsBackend.SwapBuffers();

        /// <summary>
        /// Requests that the graphics backend become the current context.
        /// May not be required for some backends.
        /// </summary>
        public void MakeCurrent() => GraphicsBackend.MakeCurrent();

        /// <summary>
        /// Requests that the current context be cleared.
        /// </summary>
        public void ClearCurrent() => GraphicsBackend.ClearCurrent();

        public virtual void CycleMode()
        {
        }

        public virtual void SetupWindow(FrameworkConfigManager config)
        {
        }

        private void handleCloseRequested()
        {
            if (!OnExitRequested())
                WindowBackend.Close();
        }

        #region Bindable Handling

        protected virtual void UpdateWindowMode(WindowMode mode)
        {
            switch (mode)
            {
                case Configuration.WindowMode.Fullscreen:
                    WindowBackend.WindowState = Platform.WindowState.Fullscreen;
                    break;

                case Configuration.WindowMode.Borderless:
                    WindowBackend.WindowState = Platform.WindowState.FullscreenBorderless;
                    break;

                case Configuration.WindowMode.Windowed:
                    WindowBackend.WindowState = Platform.WindowState.Normal;
                    break;
            }
        }

        private void visible_ValueChanged(ValueChangedEvent<bool> evt)
        {
            WindowBackend.Visible = evt.NewValue;

            if (evt.NewValue)
                OnShown();
            else
                OnHidden();
        }

        private bool boundsChanging;

        private void windowBackend_Resized(Size size)
        {
            if (!boundsChanging)
            {
                boundsChanging = true;
                Position.Value = WindowBackend.Position;
                Size.Value = size;
                boundsChanging = false;
            }

            OnResized();
        }

        private void windowBackend_Moved(Point point)
        {
            if (!boundsChanging)
            {
                boundsChanging = true;
                Position.Value = point;
                boundsChanging = false;
            }

            OnMoved(point);
        }

        private void position_ValueChanged(ValueChangedEvent<Point> evt)
        {
            if (boundsChanging)
                return;

            boundsChanging = true;
            WindowBackend.Position = evt.NewValue;
            boundsChanging = false;
        }

        private void size_ValueChanged(ValueChangedEvent<Size> evt)
        {
            if (boundsChanging)
                return;

            boundsChanging = true;
            WindowBackend.Size = evt.NewValue;
            boundsChanging = false;
        }

        private void windowBackend_WindowStateChanged(WindowState windowState)
        {
            WindowState.Value = windowState;

            switch (windowState)
            {
                case Platform.WindowState.Fullscreen:
                    WindowMode.Value = Configuration.WindowMode.Fullscreen;
                    break;

                case Platform.WindowState.FullscreenBorderless:
                    WindowMode.Value = Configuration.WindowMode.Borderless;
                    break;

                case Platform.WindowState.Normal:
                    WindowMode.Value = Configuration.WindowMode.Windowed;
                    break;
            }
        }

        #endregion

        #region Deprecated IGameWindow

        public IWindowInfo WindowInfo => throw new NotImplementedException();

        osuTK.WindowState INativeWindow.WindowState
        {
            get => WindowState.Value.ToOsuTK();
            set => WindowState.Value = value.ToFramework();
        }

        public WindowBorder WindowBorder { get; set; }

        public Rectangle Bounds
        {
            get => new Rectangle(X, Y, Width, Height);
            set
            {
                Position.Value = value.Location;
                Size.Value = value.Size;
            }
        }

        public Point Location
        {
            get => Position.Value;
            set => Position.Value = value;
        }

        Size INativeWindow.Size
        {
            get => Size.Value;
            set => Size.Value = value;
        }

        public int X
        {
            get => Position.Value.X;
            set => Position.Value = new Point(value, Position.Value.Y);
        }

        public int Y
        {
            get => Position.Value.Y;
            set => Position.Value = new Point(Position.Value.X, value);
        }

        public int Width
        {
            get => Size.Value.Width;
            set => Size.Value = new Size(value, Size.Value.Height);
        }

        public int Height
        {
            get => Size.Value.Height;
            set => Size.Value = new Size(Size.Value.Width, value);
        }

        public Rectangle ClientRectangle
        {
            get => new Rectangle(Point.Empty, WindowBackend.ClientSize);
            set
            {
            }
        }

        Size INativeWindow.ClientSize
        {
            get => WindowBackend.ClientSize;
            set
            {
            }
        }

        public MouseCursor Cursor { get; set; }

        public bool CursorVisible
        {
            get => WindowBackend.CursorVisible;
            set => WindowBackend.CursorVisible = value;
        }

        public bool CursorGrabbed
        {
            get => WindowBackend.CursorConfined;
            set => WindowBackend.CursorConfined = value;
        }

#pragma warning disable 0067

        public event EventHandler<EventArgs> Move;

        public event EventHandler<EventArgs> Resize;

        public event EventHandler<CancelEventArgs> Closing;

        event EventHandler<EventArgs> INativeWindow.Closed
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<EventArgs> Disposed;

        public event EventHandler<EventArgs> IconChanged;

        public event EventHandler<EventArgs> TitleChanged;

        public event EventHandler<EventArgs> VisibleChanged;

        public event EventHandler<EventArgs> FocusedChanged;

        public event EventHandler<EventArgs> WindowBorderChanged;

        public event EventHandler<EventArgs> WindowStateChanged;

        event EventHandler<KeyboardKeyEventArgs> INativeWindow.KeyDown
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<KeyPressEventArgs> KeyPress;

        event EventHandler<KeyboardKeyEventArgs> INativeWindow.KeyUp
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<EventArgs> MouseLeave;

        public event EventHandler<EventArgs> MouseEnter;

        event EventHandler<MouseButtonEventArgs> INativeWindow.MouseDown
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<MouseButtonEventArgs> INativeWindow.MouseUp
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<MouseMoveEventArgs> INativeWindow.MouseMove
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<MouseWheelEventArgs> INativeWindow.MouseWheel
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<FileDropEventArgs> FileDrop;

        public event EventHandler<EventArgs> Load;
        public event EventHandler<EventArgs> Unload;
        public event EventHandler<FrameEventArgs> UpdateFrame;
        public event EventHandler<FrameEventArgs> RenderFrame;

#pragma warning restore 0067

        bool IWindow.CursorInWindow => CursorInWindow.Value;

        CursorState IWindow.CursorState
        {
            get => CursorState.Value;
            set => CursorState.Value = value;
        }

        bool INativeWindow.Focused => Focused.Value;

        bool INativeWindow.Visible
        {
            get => Visible.Value;
            set => Visible.Value = value;
        }

        bool INativeWindow.Exists => Exists;

        public void Run(double updateRate) => Run();

        public void ProcessEvents()
        {
        }

        public Point PointToClient(Point point) => point;

        public Point PointToScreen(Point point) => point;

        public Icon Icon { get; set; }

        public void Dispose()
        {
        }

        #endregion
    }
}
