// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;
using osu.Framework.Bindables;
using Veldrid;
using Veldrid.Sdl2;

namespace osu.Framework.Platform
{
    public class Window
    {
        private readonly IWindowBackend windowBackend;
        private readonly IGraphicsBackend graphicsBackend;

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

        public void Create()
        {
            windowBackend.Create();

            windowBackend.Resized += windowBackend_Resized;
            windowBackend.Moved += windowBackend_Moved;
            windowBackend.Hidden += () => Visible.Value = false;
            windowBackend.Shown += () => Visible.Value = true;

            windowBackend.FocusGained += () => focused.Value = true;
            windowBackend.FocusLost += () => focused.Value = false;
            windowBackend.MouseEntered += () => cursorInWindow.Value = true;
            windowBackend.MouseLeft += () => cursorInWindow.Value = false;

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
    }
}
