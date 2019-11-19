// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using Veldrid;
using Veldrid.Sdl2;

namespace osu.Framework.Platform
{
    public class SdlWindow : IWindowBackend
    {
        private Sdl2Window implementation;
        private InputSnapshot inputSnapshot;

        #region Internal Properties

        internal IntPtr SdlWindowHandle => implementation?.SdlWindowHandle ?? IntPtr.Zero;

        private readonly Cached<float> scale = new Cached<float>();

        internal float Scale
        {
            get
            {
                if (scale.IsValid)
                    return scale.Value;

                var borders = Sdl2Functions.SDL_GetWindowBordersSize(SdlWindowHandle);
                float realWidth = implementation.Width - borders.TotalHorizontal;
                float scaledWidth = Sdl2Functions.SDL_GL_GetDrawableSize(SdlWindowHandle).X;
                scale.Value = scaledWidth / realWidth;
                return scale.Value;
            }
        }

        #endregion

        #region IWindowBackend.Properties

        private string title = "";

        public string Title
        {
            get => implementation?.Title ?? title;
            set
            {
                title = value;

                if (implementation != null)
                    implementation.Title = value;
            }
        }

        private bool visible;

        public bool Visible
        {
            get => implementation?.Visible ?? visible;
            set
            {
                visible = true;

                if (implementation != null)
                    implementation.Visible = value;
            }
        }

        private Vector2 position = Vector2.Zero;

        public Vector2 Position
        {
            get => implementation == null ? position : new Vector2(implementation.X, implementation.Y);
            set
            {
                position = value;

                if (implementation == null)
                    return;

                implementation.X = (int)value.X;
                implementation.Y = (int)value.Y;
            }
        }

        private Vector2 internalSize = Vector2.Zero;

        public Vector2 InternalSize
        {
            get
            {
                if (implementation == null)
                    return internalSize;

                var padding = Sdl2Functions.SDL_GetWindowBordersSize(SdlWindowHandle);
                var unscaled = new Vector2(implementation.Width - padding.TotalHorizontal, implementation.Height - padding.TotalVertical);
                return unscaled * Scale;
            }
            set
            {
                internalSize = value;

                if (implementation == null)
                    return;

                var padding = Sdl2Functions.SDL_GetWindowBordersSize(SdlWindowHandle);
                float scaledWidth = internalSize.X + padding.TotalHorizontal;
                float scaledHeight = internalSize.Y + padding.TotalVertical;
                implementation.Width = (int)(scaledWidth / Scale);
                implementation.Height = (int)(scaledHeight / Scale);
            }
        }

        private bool cursorVisible = true;

        public bool CursorVisible
        {
            get => implementation?.CursorVisible ?? cursorVisible;
            set
            {
                cursorVisible = value;

                if (implementation != null)
                    implementation.CursorVisible = value;
            }
        }

        private bool cursorConfined;

        public bool CursorConfined
        {
            get => cursorConfined;
            set
            {
                cursorConfined = value;

                if (implementation == null)
                    return;

                // TODO: cursor confinement
            }
        }

        private WindowState windowState;

        public WindowState WindowState
        {
            get => implementation?.WindowState ?? windowState;
            set
            {
                windowState = value;

                if (implementation != null)
                    implementation.WindowState = value;
            }
        }

        #endregion

        #region IWindowBackend.Events

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

        #region IWindowBackend.Methods

        public void Create()
        {
            SDL_WindowFlags flags = SDL_WindowFlags.OpenGL |
                                    SDL_WindowFlags.Resizable |
                                    SDL_WindowFlags.AllowHighDpi |
                                    getWindowFlags(WindowState);

            // for now we will guess the window size on creation
            var defaultBorder = new MarginPadding { Horizontal = 5, Top = 20, Bottom = 5 };
            int windowWidth = (int)(internalSize.X + defaultBorder.TotalHorizontal);
            int windowHeight = (int)(internalSize.Y + defaultBorder.TotalVertical);

            implementation = new Sdl2Window(Title, (int)position.X, (int)position.Y, windowWidth, windowHeight, flags, false);

            implementation.MouseDown += OnMouseDown;
            implementation.MouseUp += OnMouseUp;
            implementation.MouseMove += OnMouseMove;
            implementation.MouseWheel += OnMouseWheel;
            implementation.KeyDown += OnKeyDown;
            implementation.KeyUp += OnKeyUp;
            implementation.FocusGained += OnFocusGained;
            implementation.FocusLost += OnFocusLost;
            implementation.Resized += OnResized;
            implementation.Moved += OnMoved;
            implementation.MouseEntered += OnMouseEntered;
            implementation.MouseLeft += OnMouseLeft;
            implementation.Hidden += OnHidden;
            implementation.Shown += OnShown;
            implementation.Closed += OnClosed;
            implementation.DragDrop += OnDragDrop;
        }

        public void Run()
        {
            while (implementation.Exists)
            {
                inputSnapshot = implementation.PumpEvents();

                foreach (var c in inputSnapshot.KeyCharPresses)
                    OnKeyTyped(c);
            }
        }

        public void Close()
        {
            if (!OnCloseRequested())
                implementation.Close();
        }

        #endregion

        private static SDL_WindowFlags getWindowFlags(WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                    return 0;

                case WindowState.FullScreen:
                    return SDL_WindowFlags.Fullscreen;

                case WindowState.Maximized:
                    return SDL_WindowFlags.Maximized;

                case WindowState.Minimized:
                    return SDL_WindowFlags.Minimized;

                case WindowState.BorderlessFullScreen:
                    return SDL_WindowFlags.FullScreenDesktop;

                case WindowState.Hidden:
                    return SDL_WindowFlags.Hidden;
            }

            return 0;
        }
    }
}
