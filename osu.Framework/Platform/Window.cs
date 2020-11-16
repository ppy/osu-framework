// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Input;
using osuTK;
using osuTK.Input;

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

        public Size ClientSize
        {
            get => Size.Value;
            set => Size.Value = value;
        }

        public WindowState WindowState
        {
            get => WindowStateBindable.Value;
            set => WindowStateBindable.Value = value;
        }

        public CursorState CursorState
        {
            get => CursorStateBindable.Value;
            set => CursorStateBindable.Value = value;
        }

        /// <summary>
        /// Provides a bindable that controls the window's <see cref="WindowStateBindable"/>.
        /// </summary>
        public Bindable<WindowState> WindowStateBindable { get; } = new Bindable<WindowState>();

        /// <summary>
        /// Provides a bindable that controls the window's <see cref="CursorStateBindable"/>.
        /// </summary>
        public Bindable<CursorState> CursorStateBindable { get; } = new Bindable<CursorState>();

        /// <summary>
        /// Provides a bindable that controls the window's visibility.
        /// </summary>
        public Bindable<bool> Visible { get; } = new BindableBool(true);

        public Bindable<Display> CurrentDisplay { get; } = new Bindable<Display>();

        public Bindable<WindowMode> WindowMode { get; } = new Bindable<WindowMode>();

        #endregion

        #region Immutable Bindables

        private readonly BindableBool isActive = new BindableBool(true);

        public IBindable<bool> IsActive => isActive;

        private readonly BindableBool focused = new BindableBool();

        public bool Focused => focused.Value;

        private readonly BindableBool cursorInWindow = new BindableBool(true);

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
        /// Invoked when the user moves the mouse cursor within the window.
        /// </summary>
        public event Action<Vector2> MouseMove;

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

            CursorStateBindable.ValueChanged += evt =>
            {
                WindowBackend.CursorVisible = !evt.NewValue.HasFlag(CursorState.Hidden);
                WindowBackend.CursorConfined = evt.NewValue.HasFlag(CursorState.Confined);
            };

            WindowStateBindable.ValueChanged += evt => WindowBackend.WindowState = evt.NewValue;

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
            WindowBackend.MouseWheel += OnMouseWheel;
            WindowBackend.DragDrop += OnDragDrop;

            WindowBackend.DisplayChanged += d => CurrentDisplay.Value = d;

            GraphicsBackend.Initialise(WindowBackend);

            CurrentDisplay.Value = WindowBackend.CurrentDisplay;
            CurrentDisplay.ValueChanged += evt => WindowBackend.CurrentDisplay = evt.NewValue;
        }

        private bool firstDraw = true;

        public void SwapBuffers()
        {
            GraphicsBackend.SwapBuffers();

            if (firstDraw)
            {
                WindowBackend.Visible = Visible.Value;
                firstDraw = false;
            }
        }

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
                    WindowBackend.WindowState = WindowState.Fullscreen;
                    break;

                case Configuration.WindowMode.Borderless:
                    WindowBackend.WindowState = WindowState.FullscreenBorderless;
                    break;

                case Configuration.WindowMode.Windowed:
                    WindowBackend.WindowState = WindowState.Normal;
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
            WindowStateBindable.Value = windowState;

            switch (windowState)
            {
                case WindowState.Fullscreen:
                    WindowMode.Value = Configuration.WindowMode.Fullscreen;
                    break;

                case WindowState.FullscreenBorderless:
                    WindowMode.Value = Configuration.WindowMode.Borderless;
                    break;

                case WindowState.Normal:
                    WindowMode.Value = Configuration.WindowMode.Windowed;
                    break;
            }
        }

        #endregion

        public virtual Point PointToClient(Point point) => point;

        public virtual Point PointToScreen(Point point) => point;

        public void Dispose()
        {
        }
    }
}
