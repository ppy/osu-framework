// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Caching;
using osu.Framework.Input.StateChanges;
using osu.Framework.Threading;
using osuTK;
using osuTK.Input;
using SDL2;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace osu.Framework.Platform.Sdl
{
    /// <summary>
    /// Implementation of <see cref="IWindowBackend"/> that uses libSDL2.
    /// </summary>
    public class Sdl2WindowBackend : IWindowBackend
    {
        private const int default_width = 1366;
        private const int default_height = 768;

        private readonly Scheduler scheduler = new Scheduler();

        private bool mouseInWindow;
        private Point previousPolledPoint = Point.Empty;
        private readonly Cached windowDirty = new Cached();

        #region Internal Properties

        internal IntPtr SdlWindowHandle { get; private set; } = IntPtr.Zero;

        #endregion

        #region IWindowBackend.Properties

        public bool Exists { get; private set; }

        private string title = "";

        public string Title
        {
            get => SdlWindowHandle == IntPtr.Zero ? title : SDL.SDL_GetWindowTitle(SdlWindowHandle);
            set
            {
                title = value;

                if (SdlWindowHandle != IntPtr.Zero)
                    scheduler.Add(() => SDL.SDL_SetWindowTitle(SdlWindowHandle, $"{value} (SDL)"));
            }
        }

        private bool visible;

        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                scheduler.Add(() =>
                {
                    if (value)
                        SDL.SDL_ShowWindow(SdlWindowHandle);
                    else
                        SDL.SDL_HideWindow(SdlWindowHandle);
                });
            }
        }

        private Point position;

        public Point Position
        {
            get => position;
            set
            {
                position = value;
                windowDirty.Invalidate();
            }
        }

        private Size size = new Size(default_width, default_height);

        public Size Size
        {
            get => size;
            set
            {
                size = value;
                windowDirty.Invalidate();
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

        private readonly Cached<float> scale = new Cached<float>();

        public float Scale => validateScale();

        private float validateScale(bool force = false)
        {
            if (SdlWindowHandle == IntPtr.Zero)
                return 1f;

            if (!force && scale.IsValid)
                return scale.Value;

            var w = ClientSize.Width;

            switch (windowState)
            {
                case WindowState.Normal:
                    scale.Value = w / (float)size.Width;
                    break;

                case WindowState.Fullscreen:
                    scale.Value = w / (float)currentDisplayMode.Size.Width;
                    break;

                case WindowState.FullscreenBorderless:
                    SDL.SDL_GetDesktopDisplayMode(windowDisplayIndex, out var mode);
                    scale.Value = w / (float)mode.w;
                    break;

                case WindowState.Maximised:
                case WindowState.Minimised:
                    return 1f;
            }

            return scale.Value;
        }

        private bool cursorVisible = true;

        public bool CursorVisible
        {
            get => SdlWindowHandle == IntPtr.Zero ? cursorVisible : SDL.SDL_ShowCursor(SDL.SDL_QUERY) == SDL.SDL_ENABLE;
            set
            {
                cursorVisible = value;
                scheduler.Add(() => SDL.SDL_ShowCursor(value ? SDL.SDL_ENABLE : SDL.SDL_DISABLE));
            }
        }

        private bool cursorConfined;

        public bool CursorConfined
        {
            get => SdlWindowHandle == IntPtr.Zero ? cursorConfined : SDL.SDL_GetWindowGrab(SdlWindowHandle) == SDL.SDL_bool.SDL_TRUE;
            set
            {
                cursorConfined = value;
                scheduler.Add(() => SDL.SDL_SetWindowGrab(SdlWindowHandle, value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));
            }
        }

        private WindowState? windowState = WindowState.Normal;

        public WindowState WindowState
        {
            get => windowState ?? (SdlWindowHandle == IntPtr.Zero ? WindowState.Normal : windowFlags.ToWindowState());
            set
            {
                windowState = value;

                scheduler.Add(() =>
                {
                    updateWindow();

                    switch (value)
                    {
                        case WindowState.Normal:
                            SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_bool.SDL_FALSE);
                            break;

                        case WindowState.Fullscreen:
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

        public IEnumerable<Display> Displays => Enumerable.Range(0, SDL.SDL_GetNumVideoDisplays()).Select(displayFromSDL);

        public Display PrimaryDisplay => Displays.First();

        private Display currentDisplay;
        private int currentDisplayIndex = -1;

        public Display CurrentDisplay
        {
            get => currentDisplay ??= Displays.ElementAtOrDefault(currentDisplayIndex);
            set
            {
                if (value.Index == currentDisplayIndex)
                    return;

                currentDisplayIndex = value.Index;
                currentDisplay = value;

                int x = value.Bounds.Left + value.Bounds.Width / 2 - size.Width / 2;
                int y = value.Bounds.Top + value.Bounds.Height / 2 - size.Height / 2;
                position = new Point(x, y);
                WindowState = WindowState.Normal;
                windowDirty.Invalidate();
            }
        }

        private DisplayMode currentDisplayMode;

        public DisplayMode CurrentDisplayMode
        {
            get => currentDisplayMode;
            set
            {
                currentDisplayMode = value;
                windowDirty.Invalidate();
            }
        }

        private static Display displayFromSDL(int displayIndex)
        {
            var displayModes = Enumerable.Range(0, SDL.SDL_GetNumDisplayModes(displayIndex))
                                         .Select(modeIndex =>
                                         {
                                             SDL.SDL_GetDisplayMode(displayIndex, modeIndex, out var mode);
                                             return displayModeFromSDL(mode, displayIndex);
                                         })
                                         .ToArray();

            SDL.SDL_GetDisplayBounds(displayIndex, out var rect);
            return new Display(displayIndex, SDL.SDL_GetDisplayName(displayIndex), new Rectangle(rect.x, rect.y, rect.w, rect.h), displayModes);
        }

        private static DisplayMode displayModeFromSDL(SDL.SDL_DisplayMode mode, int displayIndex)
        {
            SDL.SDL_PixelFormatEnumToMasks(mode.format, out var bpp, out _, out _, out _, out _);
            return new DisplayMode(SDL.SDL_GetPixelFormatName(mode.format), new Size(mode.w, mode.h), bpp, mode.refresh_rate, displayIndex);
        }

        private int windowDisplayIndex => SdlWindowHandle == IntPtr.Zero ? 0 : SDL.SDL_GetWindowDisplayIndex(SdlWindowHandle);

        private SDL.SDL_WindowFlags windowFlags => SdlWindowHandle == IntPtr.Zero ? 0 : (SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(SdlWindowHandle);

        #endregion

        #region IWindowBackend.Events

        public event Action Update;
        public event Action Resized;
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

        public Sdl2WindowBackend()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
        }

        private void updateWindow()
        {
            if (SdlWindowHandle == IntPtr.Zero || windowDirty.IsValid)
                return;

            windowDirty.Validate();

            SDL.SDL_GetWindowSize(SdlWindowHandle, out var w, out var h);
            SDL.SDL_GetWindowPosition(SdlWindowHandle, out var x, out var y);

            if (w != size.Width || h != size.Height)
            {
                SDL.SDL_SetWindowSize(SdlWindowHandle, size.Width, size.Height);
                scale.Invalidate();
            }

            if (x != position.X || y != position.Y)
            {
                SDL.SDL_SetWindowPosition(SdlWindowHandle, position.X, position.Y);
                scale.Invalidate();
            }

            if (currentDisplayIndex != windowDisplayIndex)
            {
                currentDisplayIndex = windowDisplayIndex;
                scale.Invalidate();
                scheduler.Add(() => OnDisplayChanged(CurrentDisplay));
            }

            SDL.SDL_GetWindowDisplayMode(SdlWindowHandle, out var sdlMode);
            var currentMode = displayModeFromSDL(sdlMode, currentDisplayIndex);

            if (!currentMode.Equals(currentDisplayMode))
            {
                var targetMode = new SDL.SDL_DisplayMode { w = currentDisplayMode.Size.Width, h = currentDisplayMode.Size.Height, refresh_rate = currentDisplayMode.RefreshRate };
                SDL.SDL_GetClosestDisplayMode(currentDisplayIndex, ref targetMode, out var closest);
                SDL.SDL_SetWindowDisplayMode(SdlWindowHandle, ref closest);
                currentDisplayMode = displayModeFromSDL(closest, currentDisplayIndex);
                scale.Invalidate();
            }
        }

        #region Event Invocation

        protected virtual void OnUpdate() => Update?.Invoke();
        protected virtual void OnResized() => Resized?.Invoke();
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

        #region IWindowBackend.Methods

        public void Create()
        {
            SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                                        WindowState.ToFlags();

            SdlWindowHandle = SDL.SDL_CreateWindow(Title, Position.X, Position.Y, Size.Width, Size.Height, flags);
            currentDisplayIndex = windowDisplayIndex;
            scale.Invalidate();
            Exists = true;
        }

        public void Run()
        {
            while (Exists)
            {
                updateWindow();
                processEvents();

                if (!mouseInWindow)
                    pollMouse();

                scheduler.Update();

                OnUpdate();
            }

            if (SdlWindowHandle != IntPtr.Zero)
                SDL.SDL_DestroyWindow(SdlWindowHandle);

            OnClosed();

            SDL.SDL_Quit();
        }

        public void Close() => scheduler.Add(() => Exists = false);

        private void pollMouse()
        {
            SDL.SDL_GetGlobalMouseState(out var x, out var y);
            if (previousPolledPoint.X == x && previousPolledPoint.Y == y)
                return;

            previousPolledPoint = new Point(x, y);

            var pos = windowState == WindowState.Normal ? position : CurrentDisplay.Bounds.Location;
            var rx = x - pos.X;
            var ry = y - pos.Y;

            scheduler.Add(() => OnMouseMove(new MousePositionAbsoluteInput { Position = new Vector2(rx * Scale, ry * Scale) }));
        }

        #endregion

        #region SDL Event Handling

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

        private void handleQuitEvent(SDL.SDL_QuitEvent evtQuit)
        {
            // TODO: handle OnCloseRequested()
            // we currently have a deadlock issue where GameHost blocks
            Exists = false;
        }

        private void handleDropEvent(SDL.SDL_DropEvent evtDrop)
        {
            switch (evtDrop.type)
            {
                case SDL.SDL_EventType.SDL_DROPFILE:
                    var str = SDL.UTF8_ToManaged(evtDrop.file, true);
                    if (str != null)
                        scheduler.Add(() => OnDragDrop(str));

                    break;
            }
        }

        private void handleTouchFingerEvent(SDL.SDL_TouchFingerEvent evtTfinger)
        {
        }

        private void handleControllerDeviceEvent(SDL.SDL_ControllerDeviceEvent evtCdevice)
        {
        }

        private void handleControllerButtonEvent(SDL.SDL_ControllerButtonEvent evtCbutton)
        {
        }

        private void handleControllerAxisEvent(SDL.SDL_ControllerAxisEvent evtCaxis)
        {
        }

        private void handleJoyDeviceEvent(SDL.SDL_JoyDeviceEvent evtJdevice)
        {
        }

        private void handleJoyButtonEvent(SDL.SDL_JoyButtonEvent evtJbutton)
        {
        }

        private void handleJoyHatEvent(SDL.SDL_JoyHatEvent evtJhat)
        {
        }

        private void handleJoyBallEvent(SDL.SDL_JoyBallEvent evtJball)
        {
        }

        private void handleJoyAxisEvent(SDL.SDL_JoyAxisEvent evtJaxis)
        {
        }

        private void handleMouseWheelEvent(SDL.SDL_MouseWheelEvent evtWheel) =>
            scheduler.Add(() => OnMouseWheel(new MouseScrollRelativeInput { Delta = new Vector2(evtWheel.x, evtWheel.y) }));

        private void handleMouseButtonEvent(SDL.SDL_MouseButtonEvent evtButton)
        {
            MouseButton button = mouseButtonFromEvent(evtButton.button);

            switch (evtButton.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    scheduler.Add(() => OnMouseDown(new MouseButtonInput(button, true)));
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    scheduler.Add(() => OnMouseUp(new MouseButtonInput(button, false)));
                    break;
            }
        }

        private void handleMouseMotionEvent(SDL.SDL_MouseMotionEvent evtMotion) =>
            scheduler.Add(() => OnMouseMove(new MousePositionAbsoluteInput { Position = new Vector2(evtMotion.x * Scale, evtMotion.y * Scale) }));

        private unsafe void handleTextInputEvent(SDL.SDL_TextInputEvent evtText)
        {
            var ptr = new IntPtr(evtText.text);
            if (ptr == IntPtr.Zero)
                return;

            string text = Marshal.PtrToStringAnsi(ptr) ?? "";

            foreach (char c in text)
                scheduler.Add(() => OnKeyTyped(c));
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
                    scheduler.Add(() => OnKeyDown(new KeyboardKeyInput(key, true)));
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    scheduler.Add(() => OnKeyUp(new KeyboardKeyInput(key, false)));
                    break;
            }
        }

        private void handleWindowEvent(SDL.SDL_WindowEvent evtWindow)
        {
            var currentState = windowFlags.ToWindowState();

            if (windowState != currentState)
            {
                windowState = currentState;
                scheduler.Add(() => OnWindowStateChanged(currentState));
            }

            switch (evtWindow.windowEvent)
            {
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
                    windowDirty.Invalidate();
                    scheduler.Add(() => OnShown());
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
                    windowDirty.Invalidate();
                    scheduler.Add(() => OnHidden());
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
                    if (windowState != WindowState.Fullscreen && windowState != WindowState.FullscreenBorderless)
                        position = new Point(evtWindow.data1, evtWindow.data2);

                    windowDirty.Invalidate();
                    scale.Invalidate();
                    scheduler.Add(() => OnMoved(position));

                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                    if (windowState == WindowState.Normal)
                    {
                        size = new Size(evtWindow.data1, evtWindow.data2);

                        windowDirty.Invalidate();
                        scale.Invalidate();
                        scheduler.Add(OnResized);
                    }

                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                    windowDirty.Invalidate();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                    mouseInWindow = true;
                    scheduler.Add(() => OnMouseEntered());
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                    mouseInWindow = false;
                    scheduler.Add(() => OnMouseLeft());
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                    scheduler.Add(() => OnFocusGained());
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                    scheduler.Add(() => OnFocusLost());
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                    scheduler.Add(() => OnClosed());
                    break;
            }
        }

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
