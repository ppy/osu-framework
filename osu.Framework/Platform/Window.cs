// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Configuration;
using osu.Framework.Input;
using osu.Framework.Platform.Sdl;
using osu.Framework.Threading;
using osuTK;
using osuTK.Input;
using SDL2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Implementation of <see cref="IWindow"/> that provides bindables and
    /// delegates responsibility to window and graphics backends.
    /// </summary>
    public abstract class Window : IWindow
    {
        internal IntPtr SdlWindowHandle { get; private set; } = IntPtr.Zero;

        protected readonly IGraphicsBackend GraphicsBackend;

        #region Properties

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
        public bool Exists { get; protected set; }

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
        public Bindable<Point> PositionBindable { get; } = new Bindable<Point>();

        /// <summary>
        /// Provides a bindable that controls the window's unscaled internal size.
        /// </summary>
        public Bindable<Size> SizeBindable { get; } = new BindableSize();

        public CursorState CursorState
        {
            get => CursorStateBindable.Value;
            set => CursorStateBindable.Value = value;
        }

        /// <summary>
        /// Provides a bindable that controls the window's <see cref="CursorStateBindable"/>.
        /// </summary>
        public Bindable<CursorState> CursorStateBindable { get; } = new Bindable<CursorState>();

        public Bindable<Display> CurrentDisplayBindable { get; } = new Bindable<Display>();

        public Bindable<WindowMode> WindowMode { get; } = new Bindable<WindowMode>();

        #endregion

        #region Immutable Bindables

        private readonly BindableBool isActive = new BindableBool(true);

        public IBindable<bool> IsActive => isActive;

        private bool focused;

        public bool Focused
        {
            get => focused;
            private set
            {
                if (value == focused)
                    return;

                isActive.Value = focused = value;
            }
        }

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

        protected void OnUpdate() => Update?.Invoke();
        protected void OnResized() => Resized?.Invoke();
        protected bool OnExitRequested() => ExitRequested?.Invoke() ?? false;
        protected void OnExited() => Exited?.Invoke();
        protected void OnMouseEntered() => MouseEntered?.Invoke();
        protected void OnMouseLeft() => MouseLeft?.Invoke();
        protected void OnMoved(Point point) => Moved?.Invoke(point);
        protected void OnMouseWheel(Vector2 delta, bool precise) => MouseWheel?.Invoke(delta, precise);
        protected void OnMouseMove(Vector2 position) => MouseMove?.Invoke(position);
        protected void OnMouseDown(MouseButton button) => MouseDown?.Invoke(button);
        protected void OnMouseUp(MouseButton button) => MouseUp?.Invoke(button);
        protected void OnKeyDown(Key key) => KeyDown?.Invoke(key);
        protected void OnKeyUp(Key key) => KeyUp?.Invoke(key);
        protected void OnKeyTyped(char c) => KeyTyped?.Invoke(c);
        protected void OnJoystickAxisChanged(JoystickAxis axis) => JoystickAxisChanged?.Invoke(axis);
        protected void OnJoystickButtonDown(JoystickButton button) => JoystickButtonDown?.Invoke(button);
        protected void OnJoystickButtonUp(JoystickButton button) => JoystickButtonUp?.Invoke(button);
        protected void OnDragDrop(string file) => DragDrop?.Invoke(file);

        #endregion

        /// <summary>
        /// Creates an instance of <see cref="IGraphicsBackend"/> for the platform.
        /// </summary>
        protected abstract IGraphicsBackend CreateGraphicsBackend();

        protected Window()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER);

            GraphicsBackend = CreateGraphicsBackend();

            SupportedWindowModes = new BindableList<WindowMode>(DefaultSupportedWindowModes);

            CursorStateBindable.ValueChanged += evt =>
            {
                CursorVisible = !evt.NewValue.HasFlag(CursorState.Hidden);
                CursorConfined = evt.NewValue.HasFlag(CursorState.Confined);
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
        /// Creates the concrete window implementation and initialises the graphics backend.
        /// </summary>
        public virtual void Create()
        {
            SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | // shown after first swap to avoid white flash on startup (windows)
                                        WindowState.ToFlags();

            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_NO_CLOSE_ON_ALT_F4, "1");

            SdlWindowHandle = SDL.SDL_CreateWindow($"{Title} (SDL)", Position.X, Position.Y, Size.Width, Size.Height, flags);

            cachedScale.Invalidate();

            Exists = true;

            MouseEntered += () => cursorInWindow.Value = true;
            MouseLeft += () => cursorInWindow.Value = false;

            GraphicsBackend.Initialise(this);
        }

        private bool firstDraw = true;

        public void SwapBuffers()
        {
            GraphicsBackend.SwapBuffers();

            if (firstDraw)
            {
                Visible = true;
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

        #region Bindable Handling

        protected virtual void UpdateWindowMode(WindowMode mode)
        {
            switch (mode)
            {
                case Configuration.WindowMode.Fullscreen:
                    WindowState = WindowState.Fullscreen;
                    break;

                case Configuration.WindowMode.Borderless:
                    WindowState = WindowState.FullscreenBorderless;
                    break;

                case Configuration.WindowMode.Windowed:
                    WindowState = WindowState.Normal;
                    break;
            }
        }

        private bool boundsChanging;

        private void windowBackend_Resized(Size size)
        {
            if (!boundsChanging)
            {
                boundsChanging = true;
                Position = Position;
                Size = size;
                boundsChanging = false;
            }

            OnResized();
        }

        private void windowBackend_Moved(Point point)
        {
            if (!boundsChanging)
            {
                boundsChanging = true;
                Position = point;
                boundsChanging = false;
            }

            OnMoved(point);
        }

        private void position_ValueChanged(ValueChangedEvent<Point> evt)
        {
            if (boundsChanging)
                return;

            boundsChanging = true;
            Position = evt.NewValue;
            boundsChanging = false;
        }

        private void size_ValueChanged(ValueChangedEvent<Size> evt)
        {
            if (boundsChanging)
                return;

            boundsChanging = true;
            Size = evt.NewValue;
            boundsChanging = false;
        }

        #endregion

        public virtual Point PointToClient(Point point) => point;

        public virtual Point PointToScreen(Point point) => point;

        public void Dispose()
        {
        }

        private const int default_width = 1366;
        private const int default_height = 768;

        private readonly Scheduler commandScheduler = new Scheduler();
        private readonly Scheduler eventScheduler = new Scheduler();

        private bool mouseInWindow;
        private Point previousPolledPoint = Point.Empty;

        private readonly Dictionary<int, Sdl2ControllerBindings> controllers = new Dictionary<int, Sdl2ControllerBindings>();

        #region Internal Properties

        #endregion

        #region IProperties

        private string title = "";

        /// <summary>
        /// Gets and sets the window title.
        /// </summary>
        public string Title
        {
            get => SdlWindowHandle == IntPtr.Zero ? title : SDL.SDL_GetWindowTitle(SdlWindowHandle);
            set
            {
                title = value;
                commandScheduler.Add(() => SDL.SDL_SetWindowTitle(SdlWindowHandle, $"{value} (SDL)"));
            }
        }

        private bool visible;

        public bool Visible
        {
            get => SdlWindowHandle == IntPtr.Zero ? visible : windowFlags.HasFlag(SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
            set
            {
                visible = value;
                commandScheduler.Add(() =>
                {
                    if (value)
                        SDL.SDL_ShowWindow(SdlWindowHandle);
                    else
                        SDL.SDL_HideWindow(SdlWindowHandle);
                });
            }
        }

        private Point position = Point.Empty;

        public Point Position
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return position;

                SDL.SDL_GetWindowPosition(SdlWindowHandle, out var x, out var y);
                return new Point(x, y);
            }
            set
            {
                position = value;
                commandScheduler.Add(() => SDL.SDL_SetWindowPosition(SdlWindowHandle, value.X, value.Y));
            }
        }

        private Size size = new Size(default_width, default_height);

        public Size Size
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return size;

                SDL.SDL_GetWindowSize(SdlWindowHandle, out var w, out var h);
                return new Size(w, h);
            }
            set
            {
                size = value;
                commandScheduler.Add(() => SDL.SDL_SetWindowSize(SdlWindowHandle, value.Width, value.Height));
            }
        }

        private readonly Cached<float> cachedScale = new Cached<float>();

        private float scale => validateScale();

        private float validateScale(bool force = false)
        {
            if (SdlWindowHandle == IntPtr.Zero)
                return 1f;

            if (!force && cachedScale.IsValid)
                return cachedScale.Value;

            var w = ClientSize.Width;
            float value = 1f;

            switch (windowFlags.ToWindowState())
            {
                case WindowState.Normal:
                    value = w / (float)Size.Width;
                    break;

                case WindowState.Fullscreen:
                    value = w / (float)windowDisplayMode.w;
                    break;

                case WindowState.FullscreenBorderless:
                    // SDL_GetDesktopDisplayMode gets the native display mode, and is used for *borderless* fullscreen
                    SDL.SDL_GetDesktopDisplayMode(windowDisplayIndex, out var mode);
                    value = w / (float)mode.w;
                    break;

                case WindowState.Maximised:
                case WindowState.Minimised:
                    return 1f;
            }

            cachedScale.Value = value;
            return value;
        }

        private bool cursorVisible = true;

        public bool CursorVisible
        {
            get => SdlWindowHandle == IntPtr.Zero ? cursorVisible : SDL.SDL_ShowCursor(SDL.SDL_QUERY) == SDL.SDL_ENABLE;
            set
            {
                cursorVisible = value;
                commandScheduler.Add(() => SDL.SDL_ShowCursor(value ? SDL.SDL_ENABLE : SDL.SDL_DISABLE));
            }
        }

        private bool cursorConfined;

        public bool CursorConfined
        {
            get => SdlWindowHandle == IntPtr.Zero ? cursorConfined : SDL.SDL_GetWindowGrab(SdlWindowHandle) == SDL.SDL_bool.SDL_TRUE;
            set
            {
                cursorConfined = value;
                commandScheduler.Add(() => SDL.SDL_SetWindowGrab(SdlWindowHandle, value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));
            }
        }

        private WindowState initialWindowState = WindowState.Normal;
        private WindowState lastWindowState;

        public WindowState WindowState
        {
            get => SdlWindowHandle == IntPtr.Zero ? initialWindowState : windowFlags.ToWindowState();
            set
            {
                if (SdlWindowHandle == IntPtr.Zero)
                {
                    initialWindowState = value;
                    return;
                }

                commandScheduler.Add(() =>
                {
                    switch (value)
                    {
                        case WindowState.Normal:
                            SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_bool.SDL_FALSE);
                            break;

                        case WindowState.Fullscreen:
                            // set window display mode again, just in case if it changed from the last time we were fullscreen.
                            var fullscreenMode = closestDisplayMode(currentDisplayMode);
                            SDL.SDL_SetWindowDisplayMode(SdlWindowHandle, ref fullscreenMode);

                            SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
                            break;

                        case WindowState.FullscreenBorderless:
                            SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
                            break;

                        case WindowState.Maximised:
                            SDL.SDL_MaximizeWindow(SdlWindowHandle);
                            break;

                        case WindowState.Minimised:
                            SDL.SDL_MinimizeWindow(SdlWindowHandle);
                            break;
                    }
                });
            }
        }

        public Size ClientSize
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return Size.Empty;

                SDL.SDL_GL_GetDrawableSize(SdlWindowHandle, out var w, out var h);
                return new Size(w, h);
            }
        }

        public IEnumerable<Display> Displays => Enumerable.Range(0, SDL.SDL_GetNumVideoDisplays()).Select(displayFromSDL);

        public virtual Display PrimaryDisplay => Displays.First();

        private Display currentDisplay;
        private int lastDisplayIndex = -1;

        public Display CurrentDisplay
        {
            get => currentDisplay ??= Displays.ElementAtOrDefault(SdlWindowHandle == IntPtr.Zero ? 0 : windowDisplayIndex);
            set
            {
                if (value.Index == windowDisplayIndex)
                    return;

                int x = value.Bounds.Left + value.Bounds.Width / 2 - size.Width / 2;
                int y = value.Bounds.Top + value.Bounds.Height / 2 - size.Height / 2;

                WindowState = WindowState.Normal;
                Position = new Point(x, y);
            }
        }

        private DisplayMode currentDisplayMode;

        public DisplayMode CurrentDisplayMode
        {
            get => SdlWindowHandle == IntPtr.Zero ? currentDisplayMode : displayModeFromSDL(windowDisplayMode, windowDisplayIndex, 0);
            set
            {
                currentDisplayMode = value;

                commandScheduler.Add(() =>
                {
                    var closest = closestDisplayMode(value);
                    var wasFullscreen = windowFlags.ToWindowState() == WindowState.Fullscreen;
                    if (wasFullscreen)
                        SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_bool.SDL_FALSE);

                    SDL.SDL_SetWindowDisplayMode(SdlWindowHandle, ref closest);

                    if (wasFullscreen)
                        SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);

                    cachedScale.Invalidate();
                });
            }
        }

        public IntPtr WindowHandle
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return IntPtr.Zero;

                var wmInfo = windowWmInfo;

                // Window handle is selected per subsystem as defined at:
                // https://wiki.libsdl.org/SDL_SysWMinfo
                switch (wmInfo.subsystem)
                {
                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS:
                        return wmInfo.info.win.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_X11:
                        return wmInfo.info.x11.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_DIRECTFB:
                        return wmInfo.info.dfb.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_COCOA:
                        return wmInfo.info.cocoa.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_UIKIT:
                        return wmInfo.info.uikit.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WAYLAND:
                        return wmInfo.info.wl.shell_surface;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_ANDROID:
                        return wmInfo.info.android.window;

                    default:
                        return IntPtr.Zero;
                }
            }
        }

        #endregion

        #region Convenience Functions

        private SDL.SDL_SysWMinfo windowWmInfo
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return default;

                var wmInfo = new SDL.SDL_SysWMinfo();
                SDL.SDL_GetWindowWMInfo(SdlWindowHandle, ref wmInfo);
                return wmInfo;
            }
        }

        private int windowDisplayIndex => SdlWindowHandle == IntPtr.Zero ? 0 : SDL.SDL_GetWindowDisplayIndex(SdlWindowHandle);

        private Rectangle windowDisplayBounds
        {
            get
            {
                SDL.SDL_GetDisplayBounds(windowDisplayIndex, out var rect);
                return new Rectangle(rect.x, rect.y, rect.w, rect.h);
            }
        }

        private SDL.SDL_WindowFlags windowFlags => SdlWindowHandle == IntPtr.Zero ? 0 : (SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(SdlWindowHandle);

        private SDL.SDL_DisplayMode windowDisplayMode
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return default;

                // SDL_GetWindowDisplayMode gets the resolution currently assigned to the window for *exclusive* fullscreen
                if (SDL.SDL_GetWindowDisplayMode(SdlWindowHandle, out var mode) >= 0)
                    return mode;

                // SDL_GetWindowDisplayMode can fail if the window was shown fullscreen on a different (especially larger) window before.
                // if that happens, fall back to closest mode for the current display.
                return closestDisplayMode(CurrentDisplayMode);
            }
        }

        private SDL.SDL_DisplayMode closestDisplayMode(DisplayMode mode)
        {
            var targetMode = new SDL.SDL_DisplayMode { w = mode.Size.Width, h = mode.Size.Height, refresh_rate = mode.RefreshRate };
            SDL.SDL_GetClosestDisplayMode(windowDisplayIndex, ref targetMode, out var closest);
            return closest;
        }

        private static Display displayFromSDL(int displayIndex)
        {
            var displayModes = Enumerable.Range(0, SDL.SDL_GetNumDisplayModes(displayIndex))
                                         .Select(modeIndex =>
                                         {
                                             SDL.SDL_GetDisplayMode(displayIndex, modeIndex, out var mode);
                                             return displayModeFromSDL(mode, displayIndex, modeIndex);
                                         })
                                         .ToArray();

            SDL.SDL_GetDisplayBounds(displayIndex, out var rect);
            return new Display(displayIndex, SDL.SDL_GetDisplayName(displayIndex), new Rectangle(rect.x, rect.y, rect.w, rect.h), displayModes);
        }

        private static DisplayMode displayModeFromSDL(SDL.SDL_DisplayMode mode, int displayIndex, int modeIndex)
        {
            SDL.SDL_PixelFormatEnumToMasks(mode.format, out var bpp, out _, out _, out _, out _);
            return new DisplayMode(SDL.SDL_GetPixelFormatName(mode.format), new Size(mode.w, mode.h), bpp, mode.refresh_rate, modeIndex, displayIndex);
        }

        private void enqueueJoystickAxisInput(JoystickAxisSource axisSource, short axisValue)
        {
            // SDL reports axis values in the range short.MinValue to short.MaxValue, so we scale and clamp it to the range of -1f to 1f
            var clamped = Math.Clamp((float)axisValue / short.MaxValue, -1f, 1f);
            eventScheduler.Add(() => OnJoystickAxisChanged(new JoystickAxis(axisSource, clamped)));
        }

        private void enqueueJoystickButtonInput(JoystickButton button, bool isPressed)
        {
            if (isPressed)
                eventScheduler.Add(() => OnJoystickButtonDown(button));
            else
                eventScheduler.Add(() => OnJoystickButtonUp(button));
        }

        #endregion

        #region IMethods

        /// <summary>
        /// Starts the window's run loop.
        /// </summary>
        public void Run()
        {
            while (Exists)
            {
                commandScheduler.Update();

                if (!Exists)
                    break;

                processEvents();

                if (!mouseInWindow)
                    pollMouse();

                eventScheduler.Update();

                OnUpdate();
            }

            if (SdlWindowHandle != IntPtr.Zero)
                SDL.SDL_DestroyWindow(SdlWindowHandle);

            SDL.SDL_Quit();
        }

        /// <summary>
        /// Forcefully closes the window.
        /// </summary>
        public void Close() => commandScheduler.Add(() => Exists = false);

        /// <summary>
        /// Attempts to close the window.
        /// </summary>
        public void RequestClose() => ScheduleEvent(() =>
        {
            if (!OnExitRequested())
                Close();
        });

        public unsafe void SetIcon(Image<Rgba32> image)
        {
            var data = image.GetPixelSpan().ToArray();
            var imageSize = image.Size();

            commandScheduler.Add(() =>
            {
                IntPtr surface;
                fixed (Rgba32* ptr = data)
                    surface = SDL.SDL_CreateRGBSurfaceFrom(new IntPtr(ptr), imageSize.Width, imageSize.Height, 32, imageSize.Width * 4, 0xff, 0xff00, 0xff0000, 0xff000000);

                SDL.SDL_SetWindowIcon(SdlWindowHandle, surface);
                SDL.SDL_FreeSurface(surface);
            });
        }

        private void pollMouse()
        {
            SDL.SDL_GetGlobalMouseState(out var x, out var y);
            if (previousPolledPoint.X == x && previousPolledPoint.Y == y)
                return;

            previousPolledPoint = new Point(x, y);

            var pos = windowFlags.ToWindowState() == WindowState.Normal ? Position : windowDisplayBounds.Location;
            var rx = x - pos.X;
            var ry = y - pos.Y;

            ScheduleEvent(() => OnMouseMove(new Vector2(rx * scale, ry * scale)));
        }

        #endregion

        #region SDL Event Handling

        /// <summary>
        /// Adds an <see cref="Action"/> to the <see cref="Scheduler"/> expected to handle event callbacks.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to execute.</param>
        protected void ScheduleEvent(Action action) => eventScheduler.Add(action);

        private void processEvents()
        {
            while (SDL.SDL_PollEvent(out var evt) > 0)
            {
                switch (evt.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                    case SDL.SDL_EventType.SDL_APP_TERMINATING:
                        handleQuitEvent(evt.quit);
                        break;

                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        handleWindowEvent(evt.window);
                        break;

                    case SDL.SDL_EventType.SDL_KEYDOWN:
                    case SDL.SDL_EventType.SDL_KEYUP:
                        handleKeyboardEvent(evt.key);
                        break;

                    case SDL.SDL_EventType.SDL_TEXTEDITING:
                        handleTextEditingEvent(evt.edit);
                        break;

                    case SDL.SDL_EventType.SDL_TEXTINPUT:
                        handleTextInputEvent(evt.text);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEMOTION:
                        handleMouseMotionEvent(evt.motion);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                        handleMouseButtonEvent(evt.button);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                        handleMouseWheelEvent(evt.wheel);
                        break;

                    case SDL.SDL_EventType.SDL_JOYAXISMOTION:
                        handleJoyAxisEvent(evt.jaxis);
                        break;

                    case SDL.SDL_EventType.SDL_JOYBALLMOTION:
                        handleJoyBallEvent(evt.jball);
                        break;

                    case SDL.SDL_EventType.SDL_JOYHATMOTION:
                        handleJoyHatEvent(evt.jhat);
                        break;

                    case SDL.SDL_EventType.SDL_JOYBUTTONDOWN:
                    case SDL.SDL_EventType.SDL_JOYBUTTONUP:
                        handleJoyButtonEvent(evt.jbutton);
                        break;

                    case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                    case SDL.SDL_EventType.SDL_JOYDEVICEREMOVED:
                        handleJoyDeviceEvent(evt.jdevice);
                        break;

                    case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                        handleControllerAxisEvent(evt.caxis);
                        break;

                    case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                        handleControllerButtonEvent(evt.cbutton);
                        break;

                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                        handleControllerDeviceEvent(evt.cdevice);
                        break;

                    case SDL.SDL_EventType.SDL_FINGERDOWN:
                    case SDL.SDL_EventType.SDL_FINGERUP:
                    case SDL.SDL_EventType.SDL_FINGERMOTION:
                        handleTouchFingerEvent(evt.tfinger);
                        break;

                    case SDL.SDL_EventType.SDL_DROPFILE:
                    case SDL.SDL_EventType.SDL_DROPTEXT:
                    case SDL.SDL_EventType.SDL_DROPBEGIN:
                    case SDL.SDL_EventType.SDL_DROPCOMPLETE:
                        handleDropEvent(evt.drop);
                        break;
                }
            }
        }

        private void handleQuitEvent(SDL.SDL_QuitEvent evtQuit) => RequestClose();

        private void handleDropEvent(SDL.SDL_DropEvent evtDrop)
        {
            switch (evtDrop.type)
            {
                case SDL.SDL_EventType.SDL_DROPFILE:
                    var str = SDL.UTF8_ToManaged(evtDrop.file, true);
                    if (str != null)
                        ScheduleEvent(() => OnDragDrop(str));

                    break;
            }
        }

        private void handleTouchFingerEvent(SDL.SDL_TouchFingerEvent evtTfinger)
        {
        }

        private void handleControllerDeviceEvent(SDL.SDL_ControllerDeviceEvent evtCdevice)
        {
            switch (evtCdevice.type)
            {
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    var controller = SDL.SDL_GameControllerOpen(evtCdevice.which);
                    var joystick = SDL.SDL_GameControllerGetJoystick(controller);
                    var instanceID = SDL.SDL_JoystickGetDeviceInstanceID(evtCdevice.which);
                    controllers[instanceID] = new Sdl2ControllerBindings(joystick, controller);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    SDL.SDL_GameControllerClose(controllers[evtCdevice.which].ControllerHandle);
                    controllers.Remove(evtCdevice.which);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                    if (controllers.TryGetValue(evtCdevice.which, out var state))
                        state.PopulateBindings();

                    break;
            }
        }

        private void handleControllerButtonEvent(SDL.SDL_ControllerButtonEvent evtCbutton)
        {
            var button = ((SDL.SDL_GameControllerButton)evtCbutton.button).ToJoystickButton();

            switch (evtCbutton.type)
            {
                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    enqueueJoystickButtonInput(button, true);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    enqueueJoystickButtonInput(button, false);
                    break;
            }
        }

        private void handleControllerAxisEvent(SDL.SDL_ControllerAxisEvent evtCaxis) =>
            enqueueJoystickAxisInput(((SDL.SDL_GameControllerAxis)evtCaxis.axis).ToJoystickAxisSource(), evtCaxis.axisValue);

        private void handleJoyDeviceEvent(SDL.SDL_JoyDeviceEvent evtJdevice)
        {
            switch (evtJdevice.type)
            {
                case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                    var instanceID = SDL.SDL_JoystickGetDeviceInstanceID(evtJdevice.which);

                    // if the joystick is already opened, ignore it
                    if (controllers.ContainsKey(instanceID))
                        break;

                    var joystick = SDL.SDL_JoystickOpen(evtJdevice.which);
                    controllers[instanceID] = new Sdl2ControllerBindings(joystick, IntPtr.Zero);
                    break;

                case SDL.SDL_EventType.SDL_JOYDEVICEREMOVED:
                    // if the joystick is already closed, ignore it
                    if (!controllers.ContainsKey(evtJdevice.which))
                        break;

                    SDL.SDL_JoystickClose(controllers[evtJdevice.which].JoystickHandle);
                    controllers.Remove(evtJdevice.which);
                    break;
            }
        }

        private void handleJoyButtonEvent(SDL.SDL_JoyButtonEvent evtJbutton)
        {
            // if this button exists in the controller bindings, skip it
            if (controllers.TryGetValue(evtJbutton.which, out var state) && state.GetButtonForIndex(evtJbutton.button) != SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID)
                return;

            var button = JoystickButton.FirstButton + evtJbutton.button;

            switch (evtJbutton.type)
            {
                case SDL.SDL_EventType.SDL_JOYBUTTONDOWN:
                    enqueueJoystickButtonInput(button, true);
                    break;

                case SDL.SDL_EventType.SDL_JOYBUTTONUP:
                    enqueueJoystickButtonInput(button, false);
                    break;
            }
        }

        private void handleJoyHatEvent(SDL.SDL_JoyHatEvent evtJhat)
        {
        }

        private void handleJoyBallEvent(SDL.SDL_JoyBallEvent evtJball)
        {
        }

        private void handleJoyAxisEvent(SDL.SDL_JoyAxisEvent evtJaxis)
        {
            // if this axis exists in the controller bindings, skip it
            if (controllers.TryGetValue(evtJaxis.which, out var state) && state.GetAxisForIndex(evtJaxis.axis) != SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_INVALID)
                return;

            enqueueJoystickAxisInput(JoystickAxisSource.Axis1 + evtJaxis.axis, evtJaxis.axisValue);
        }

        private void handleMouseWheelEvent(SDL.SDL_MouseWheelEvent evtWheel) =>
            ScheduleEvent(() => OnMouseWheel(new Vector2(evtWheel.x, evtWheel.y), false));

        private void handleMouseButtonEvent(SDL.SDL_MouseButtonEvent evtButton)
        {
            MouseButton button = mouseButtonFromEvent(evtButton.button);

            switch (evtButton.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    ScheduleEvent(() => OnMouseDown(button));
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    ScheduleEvent(() => OnMouseUp(button));
                    break;
            }
        }

        private void handleMouseMotionEvent(SDL.SDL_MouseMotionEvent evtMotion) =>
            ScheduleEvent(() => OnMouseMove(new Vector2(evtMotion.x * scale, evtMotion.y * scale)));

        private unsafe void handleTextInputEvent(SDL.SDL_TextInputEvent evtText)
        {
            var ptr = new IntPtr(evtText.text);
            if (ptr == IntPtr.Zero)
                return;

            string text = Marshal.PtrToStringUTF8(ptr) ?? "";

            foreach (char c in text)
                ScheduleEvent(() => OnKeyTyped(c));
        }

        private void handleTextEditingEvent(SDL.SDL_TextEditingEvent evtEdit)
        {
        }

        private void handleKeyboardEvent(SDL.SDL_KeyboardEvent evtKey)
        {
            Key key = evtKey.keysym.ToKey();

            if (key == Key.Unknown || key == Key.CapsLock)
                return;

            switch (evtKey.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    ScheduleEvent(() => OnKeyDown(key));
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    ScheduleEvent(() => OnKeyUp(key));
                    break;
            }
        }

        private void handleWindowEvent(SDL.SDL_WindowEvent evtWindow)
        {
            var currentState = windowFlags.ToWindowState();
            var displayIndex = windowDisplayIndex;

            if (lastWindowState != currentState)
            {
                lastWindowState = currentState;
                cachedScale.Invalidate();
                ScheduleEvent(() => OnWindowStateChanged(currentState));
            }

            if (lastDisplayIndex != displayIndex)
            {
                lastDisplayIndex = displayIndex;
                currentDisplay = null;
                cachedScale.Invalidate();
                ScheduleEvent(() => OnDisplayChanged(Displays.ElementAtOrDefault(displayIndex) ?? PrimaryDisplay));
            }

            switch (evtWindow.windowEvent)
            {
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
                    ScheduleEvent(OnShown);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
                    ScheduleEvent(OnHidden);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
                    var eventPos = new Point(evtWindow.data1, evtWindow.data2);

                    if (currentState == WindowState.Normal && !eventPos.Equals(position))
                    {
                        position = eventPos;
                        cachedScale.Invalidate();
                        ScheduleEvent(() => OnMoved(eventPos));
                    }

                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                    var newSize = new Size(evtWindow.data1, evtWindow.data2);

                    if (currentState == WindowState.Normal && !newSize.Equals(size))
                    {
                        size = newSize;
                        cachedScale.Invalidate();
                        ScheduleEvent(() => OnResized());
                    }

                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                    mouseInWindow = true;
                    ScheduleEvent(OnMouseEntered);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                    mouseInWindow = false;
                    ScheduleEvent(OnMouseLeft);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                    ScheduleEvent(OnFocusGained);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                    ScheduleEvent(OnFocusLost);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                    break;
            }
        }

        protected void OnHidden()
        {
        }

        protected void OnShown()
        {
        }

        protected void OnWindowStateChanged(WindowState currentState)
        {
            // todo: implement?
        }

        protected void OnDisplayChanged(Display display) => CurrentDisplayBindable.Value = display;

        protected void OnFocusGained() => Focused = true;

        protected void OnFocusLost() => Focused = false;

        private MouseButton mouseButtonFromEvent(byte button)
        {
            switch ((uint)button)
            {
                default:
                case SDL.SDL_BUTTON_LEFT:
                    return MouseButton.Left;

                case SDL.SDL_BUTTON_RIGHT:
                    return MouseButton.Right;

                case SDL.SDL_BUTTON_MIDDLE:
                    return MouseButton.Middle;

                case SDL.SDL_BUTTON_X1:
                    return MouseButton.Button1;

                case SDL.SDL_BUTTON_X2:
                    return MouseButton.Button2;
            }
        }

        #endregion
    }
}
