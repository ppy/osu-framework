// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osuTK;
using osuTK.Input;
using osuTK.Platform;
using Veldrid;
using Veldrid.Sdl2;
using MouseMoveEventArgs = Veldrid.Sdl2.MouseMoveEventArgs;
using MouseWheelEventArgs = Veldrid.Sdl2.MouseWheelEventArgs;
using Point = Veldrid.Point;
using Rectangle = System.Drawing.Rectangle;
using Vector2 = System.Numerics.Vector2;
using WindowState = Veldrid.WindowState;

namespace osu.Framework.Platform
{
    public class Window : IWindow
    {
        private readonly IWindowBackend windowBackend;
        private readonly IGraphicsBackend graphicsBackend;
        private bool initialised;

        #region Properties

        public string Title
        {
            get => windowBackend.Title;
            set => windowBackend.Title = value;
        }

        public bool VerticalSync
        {
            get => graphicsBackend.VerticalSync;
            set => graphicsBackend.VerticalSync = value;
        }

        public bool Exists => windowBackend.Exists;

        #endregion

        #region Mutable Bindables

        public Bindable<Vector2> Position { get; } = new Bindable<Vector2>();

        public Bindable<Vector2> InternalSize { get; } = new Bindable<Vector2>();

        public Bindable<WindowState> WindowState { get; } = new Bindable<WindowState>();

        public Bindable<CursorState> CursorState { get; } = new Bindable<CursorState>();

        public Bindable<bool> Visible { get; } = new Bindable<bool>();

        #endregion

        #region Immutable Bindables

        private readonly BindableBool focused = new BindableBool();
        public IBindable<bool> Focused => focused;

        private readonly BindableBool cursorInWindow = new BindableBool();
        public IBindable<bool> CursorInWindow => cursorInWindow;

        private readonly Bindable<Vector2> windowSize = new Bindable<Vector2>();
        public IBindable<Vector2> WindowSize => windowSize;

        #endregion

        #region Events

        public event Action Update;

        public event Action Resized;
        public event Func<bool> CloseRequested;
        public event Action Closed;
        public event Action FocusLost;
        public event Action FocusGained;
        public event Action Shown;
        public event Action Hidden;
        public event Action MouseEntered;
        public event Action MouseLeft;
        public event Action<Point> Moved;
        public event Action<MouseWheelEventArgs> MouseWheel;
        public event Action<MouseMoveEventArgs> MouseMove;
        public event Action<MouseEvent> MouseDown;
        public event Action<MouseEvent> MouseUp;
        public event Action<KeyEvent> KeyDown;
        public event Action<KeyEvent> KeyUp;
        public event Action<char> KeyTyped;
        public event Action<DragDropEvent> DragDrop;

        #endregion

        #region Event Invocation

        protected virtual void OnUpdate() => Update?.Invoke();
        protected virtual void OnResized() => Resized?.Invoke();
        protected virtual bool OnCloseRequested() => CloseRequested?.Invoke() ?? false;
        protected virtual void OnClosed() => Closed?.Invoke();
        protected virtual void OnFocusLost() => FocusLost?.Invoke();
        protected virtual void OnFocusGained() => FocusGained?.Invoke();
        protected virtual void OnShown() => Shown?.Invoke();
        protected virtual void OnHidden() => Hidden?.Invoke();
        protected virtual void OnMouseEntered() => MouseEntered?.Invoke();
        protected virtual void OnMouseLeft() => MouseLeft?.Invoke();
        protected virtual void OnMoved(Point point) => Moved?.Invoke(point);
        protected virtual void OnMouseWheel(MouseWheelEventArgs args) => MouseWheel?.Invoke(args);
        protected virtual void OnMouseMove(MouseMoveEventArgs args) => MouseMove?.Invoke(args);
        protected virtual void OnMouseDown(MouseEvent evt) => MouseDown?.Invoke(evt);
        protected virtual void OnMouseUp(MouseEvent evt) => MouseUp?.Invoke(evt);
        protected virtual void OnKeyDown(KeyEvent evt) => KeyDown?.Invoke(evt);
        protected virtual void OnKeyUp(KeyEvent evt) => KeyUp?.Invoke(evt);
        protected virtual void OnKeyTyped(char c) => KeyTyped?.Invoke(c);
        protected virtual void OnDragDrop(DragDropEvent evt) => DragDrop?.Invoke(evt);

        #endregion

        #region Constructors

        public Window(IWindowBackend windowBackend, IGraphicsBackend graphicsBackend)
        {
            this.windowBackend = windowBackend;
            this.graphicsBackend = graphicsBackend;

            Position.ValueChanged += position_ValueChanged;
            InternalSize.ValueChanged += internalSize_ValueChanged;

            CursorState.ValueChanged += evt =>
            {
                this.windowBackend.CursorVisible = !evt.NewValue.HasFlag(Platform.CursorState.Hidden);
                this.windowBackend.CursorConfined = evt.NewValue.HasFlag(Platform.CursorState.Confined);
            };

            WindowState.ValueChanged += evt => this.windowBackend.WindowState = evt.NewValue;

            Visible.ValueChanged += visible_ValueChanged;

            focused.ValueChanged += evt =>
            {
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

        #endregion

        #region Methods

        public void Initialise()
        {
            if (initialised)
                return;

            initialised = true;

            windowBackend.Initialise();

            windowBackend.Resized += windowBackend_Resized;
            windowBackend.Moved += windowBackend_Moved;
            windowBackend.Hidden += () => Visible.Value = false;
            windowBackend.Shown += () => Visible.Value = true;

            windowBackend.FocusGained += () => focused.Value = true;
            windowBackend.FocusLost += () => focused.Value = false;
            windowBackend.MouseEntered += () => cursorInWindow.Value = true;
            windowBackend.MouseLeft += () => cursorInWindow.Value = false;

            windowBackend.Update += OnUpdate;
            windowBackend.KeyDown += OnKeyDown;
            windowBackend.KeyUp += OnKeyUp;
            windowBackend.KeyTyped += OnKeyTyped;
            windowBackend.MouseDown += OnMouseDown;
            windowBackend.MouseUp += OnMouseUp;
            windowBackend.MouseMove += OnMouseMove;
            windowBackend.MouseWheel += OnMouseWheel;
            windowBackend.DragDrop += OnDragDrop;

            graphicsBackend.Initialise(windowBackend);
        }

        public void Run() => windowBackend.Run();

        public void Close() => windowBackend.Close();

        public void SwapBuffers() => graphicsBackend.SwapBuffers();

        public void MakeCurrent() => graphicsBackend.MakeCurrent();

        #endregion

        #region Bindable Handling

        private void visible_ValueChanged(ValueChangedEvent<bool> evt)
        {
            windowBackend.Visible = evt.NewValue;

            if (evt.NewValue)
                OnShown();
            else
                OnHidden();
        }

        private bool boundsChanging;

        private void windowBackend_Resized()
        {
            if (!boundsChanging)
            {
                boundsChanging = true;
                Position.Value = windowBackend.Position;
                InternalSize.Value = windowBackend.InternalSize;
                boundsChanging = false;
            }

            OnResized();
        }

        private void windowBackend_Moved(Point point)
        {
            if (!boundsChanging)
            {
                boundsChanging = true;
                Position.Value = new Vector2(point.X, point.Y);
                boundsChanging = false;
            }

            OnMoved(point);
        }

        private void position_ValueChanged(ValueChangedEvent<Vector2> evt)
        {
            if (boundsChanging)
                return;

            boundsChanging = true;
            windowBackend.Position = evt.NewValue;
            boundsChanging = false;
        }

        private void internalSize_ValueChanged(ValueChangedEvent<Vector2> evt)
        {
            if (boundsChanging)
                return;

            boundsChanging = true;
            windowBackend.InternalSize = evt.NewValue;
            boundsChanging = false;
        }

        #endregion

        #region Deprecated IGameWindow

        public IWindowInfo WindowInfo => throw new NotImplementedException();

        osuTK.WindowState INativeWindow.WindowState
        {
            get => WindowState.Value.ToOsuTK();
            set => WindowState.Value = value.ToVeldrid();
        }

        public WindowBorder WindowBorder { get; set; }

        public Rectangle Bounds
        {
            get => new Rectangle(X, Y, Width, Height);
            set
            {
                Position.Value = new Vector2(value.X, value.Y);
                InternalSize.Value = new Vector2(value.Width, value.Height);
            }
        }

        public System.Drawing.Point Location
        {
            get => Position.Value.ToSystemDrawingPoint();
            set => Position.Value = value.ToSystemNumerics();
        }

        public Size Size
        {
            get => InternalSize.Value.ToSystemDrawingSize();
            set => InternalSize.Value = value.ToSystemNumerics();
        }

        public int X
        {
            get => (int)Position.Value.X;
            set => Position.Value = new Vector2(value, Position.Value.Y);
        }

        public int Y
        {
            get => (int)Position.Value.Y;
            set => Position.Value = new Vector2(Position.Value.X, value);
        }

        public int Width
        {
            get => (int)InternalSize.Value.X;
            set => InternalSize.Value = new Vector2(value, InternalSize.Value.Y);
        }

        public int Height
        {
            get => (int)InternalSize.Value.Y;
            set => InternalSize.Value = new Vector2(InternalSize.Value.X, value);
        }

        public Rectangle ClientRectangle
        {
            get => new Rectangle(Position.Value.ToSystemDrawingPoint(), InternalSize.Value.ToSystemDrawingSize());
            set
            {
                Position.Value = new Vector2(value.X, value.Y);
                InternalSize.Value = new Vector2(value.Width, value.Height);
            }
        }

        public Size ClientSize
        {
            get => InternalSize.Value.ToSystemDrawingSize();
            set => InternalSize.Value = value.ToSystemNumerics();
        }

        public MouseCursor Cursor { get; set; }

        public bool CursorVisible
        {
            get => windowBackend.CursorVisible;
            set => windowBackend.CursorVisible = value;
        }

        public bool CursorGrabbed
        {
            get => windowBackend.CursorConfined;
            set => windowBackend.CursorConfined = value;
        }

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

        event EventHandler<osuTK.Input.MouseMoveEventArgs> INativeWindow.MouseMove
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        event EventHandler<osuTK.Input.MouseWheelEventArgs> INativeWindow.MouseWheel
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<FileDropEventArgs> FileDrop;

        bool IWindow.CursorInWindow => CursorInWindow.Value;

        CursorState IWindow.CursorState { get; set; }

        public VSyncMode VSync
        {
            get => VerticalSync ? VSyncMode.On : VSyncMode.Off;
            set => VerticalSync = value == VSyncMode.On;
        }

        public WindowMode DefaultWindowMode => WindowMode.Windowed;

        public DisplayDevice CurrentDisplay { get; } = new DisplayDevice();

        private readonly BindableBool isActive = new BindableBool(true);
        public IBindable<bool> IsActive => isActive;

        public BindableSafeArea SafeAreaPadding { get; } = new BindableSafeArea();

        public IBindableList<WindowMode> SupportedWindowModes { get; } = new BindableList<WindowMode>();

        public IEnumerable<DisplayResolution> AvailableResolutions => new DisplayResolution[0];

        bool INativeWindow.Focused => Focused.Value;

        bool INativeWindow.Visible
        {
            get => Visible.Value;
            set => Visible.Value = value;
        }

        bool INativeWindow.Exists => Exists;

        public void CycleMode()
        {
            // TODO: CycleMode
        }

        public void SetupWindow(FrameworkConfigManager config)
        {
            // TODO: SetupWindow
        }

        public event Func<bool> ExitRequested;
        public event Action Exited;

        public void Run(double updateRate) => Run();

        public void ProcessEvents()
        {
        }

        public System.Drawing.Point PointToClient(System.Drawing.Point point) => point;

        public System.Drawing.Point PointToScreen(System.Drawing.Point point) => point;

        public Icon Icon { get; set; }

        public event EventHandler<EventArgs> Load;
        public event EventHandler<EventArgs> Unload;
        public event EventHandler<FrameEventArgs> UpdateFrame;
        public event EventHandler<FrameEventArgs> RenderFrame;

        public void Dispose()
        {
        }

        #endregion
    }
}
