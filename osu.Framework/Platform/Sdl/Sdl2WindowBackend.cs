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
    public class Sdl2WindowBackend : IWindowBackend
    {
        private const int default_width = 1366;
        private const int default_height = 768;

        private readonly Scheduler scheduler = new Scheduler();

        private bool mouseInWindow;
        private Point previousPolledPoint = Point.Empty;

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
                scheduler.Add(() => SDL.SDL_SetWindowTitle(SdlWindowHandle, $"{value} (SDL)"));
            }
        }

        private bool visible;

        public bool Visible
        {
            get => SdlWindowHandle == IntPtr.Zero ? visible : ((SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(SdlWindowHandle)).HasFlag(SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN);
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

        private Point position = Point.Empty;

        public Point Position
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return position;

                SDL.SDL_GetWindowPosition(SdlWindowHandle, out int x, out int y);
                return new Point(x, y);
            }
            set
            {
                position = value;
                scheduler.Add(() => SDL.SDL_SetWindowPosition(SdlWindowHandle, value.X, value.Y));
            }
        }

        private Size size = new Size(default_width, default_height);

        public Size Size
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return size;

                SDL.SDL_GetWindowSize(SdlWindowHandle, out int w, out int h);
                return new Size(w, h);
            }
            set
            {
                size = value;
                scheduler.Add(() =>
                {
                    SDL.SDL_SetWindowSize(SdlWindowHandle, value.Width, value.Height);
                    validateScale(true);
                });
            }
        }

        private readonly Cached<float> scale = new Cached<float>();

        public float Scale => validateScale();

        private float validateScale(bool force = false)
        {
            if (!force && scale.IsValid)
                return scale.Value;

            if (SdlWindowHandle == IntPtr.Zero)
                return 1f;

            SDL.SDL_GL_GetDrawableSize(SdlWindowHandle, out int w, out _);

            scale.Value = w / (float)Size.Width;
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

        private WindowState windowState = WindowState.Normal;

        public WindowState WindowState
        {
            get => SdlWindowHandle == IntPtr.Zero ? windowState : windowFlags.ToWindowState();
            set
            {
                windowState = value;
                scheduler.Add(() =>
                {
                    switch (value)
                    {
                        case WindowState.Normal:
                            SDL.SDL_SetWindowFullscreen(SdlWindowHandle, 0);
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

        private int previousDisplayIndex = -1;

        public Display CurrentDisplay
        {
            get => Displays.ElementAtOrDefault(currentDisplayIndex);
            set
            {
                if (value.Index == currentDisplayIndex)
                    return;

                scheduler.Add(() =>
                {
                    var windowSize = Size;
                    int x = value.Bounds.Left + value.Bounds.Width / 2 - windowSize.Width / 2;
                    int y = value.Bounds.Top + value.Bounds.Height / 2 - windowSize.Height / 2;
                    SDL.SDL_SetWindowPosition(SdlWindowHandle, x, y);
                    validateScale(true);
                });
            }
        }

        public DisplayMode CurrentDisplayMode
        {
            get
            {
                SDL.SDL_GetCurrentDisplayMode(currentDisplayIndex, out var mode);
                return displayModeFromSDL(mode);
            }
        }

        private static Display displayFromSDL(int displayIndex)
        {
            var displayModes = Enumerable.Range(0, SDL.SDL_GetNumDisplayModes(displayIndex))
                                         .Select(modeIndex =>
                                         {
                                             SDL.SDL_GetDisplayMode(displayIndex, modeIndex, out var mode);
                                             return displayModeFromSDL(mode);
                                         })
                                         .ToArray();

            SDL.SDL_GetDisplayBounds(displayIndex, out var rect);
            return new Display(displayIndex, SDL.SDL_GetDisplayName(displayIndex), new Rectangle(rect.x, rect.y, rect.w, rect.h), displayModes);
        }

        private static DisplayMode displayModeFromSDL(SDL.SDL_DisplayMode mode)
        {
            SDL.SDL_PixelFormatEnumToMasks(mode.format, out var bpp, out _, out _, out _, out _);
            return new DisplayMode(SDL.SDL_GetPixelFormatName(mode.format), new Size(mode.w, mode.h), bpp, mode.refresh_rate);
        }

        private void checkCurrentDisplay()
        {
            if (previousDisplayIndex == currentDisplayIndex)
                return;

            previousDisplayIndex = currentDisplayIndex;
            OnDisplayChanged(CurrentDisplay);
        }

        private int currentDisplayIndex => SdlWindowHandle == IntPtr.Zero ? 0 : SDL.SDL_GetWindowDisplayIndex(SdlWindowHandle);

        private SDL.SDL_WindowFlags windowFlags => SdlWindowHandle == IntPtr.Zero ? 0 : (SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(SdlWindowHandle);

        #endregion

        #region IWindowBackend.Events

        public event Action Update;
        public event Action Resized;
        public event Action WindowStateChanged;
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

        #region Event Invocation

        protected virtual void OnUpdate() => Update?.Invoke();
        protected virtual void OnResized() => Resized?.Invoke();
        protected virtual void OnWindowStateChanged() => WindowStateChanged?.Invoke();
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

            validateScale(true);

            Exists = true;
        }

        public void Run()
        {
            while (Exists)
            {
                scheduler.Update();

                processEvents();

                if (!mouseInWindow)
                    pollMouse();

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

            var rx = x - Position.X;
            var ry = y - Position.Y;
            OnMouseMove(new MousePositionAbsoluteInput { Position = new Vector2(rx * Scale, ry * Scale) });
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
                        OnDragDrop(str);

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
            OnMouseWheel(new MouseScrollRelativeInput { Delta = new Vector2(evtWheel.x, evtWheel.y) });

        private void handleMouseButtonEvent(SDL.SDL_MouseButtonEvent evtButton)
        {
            MouseButton button = mouseButtonFromEvent(evtButton.button);

            switch (evtButton.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    OnMouseDown(new MouseButtonInput(button, true));
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    OnMouseUp(new MouseButtonInput(button, false));
                    break;
            }
        }

        private void handleMouseMotionEvent(SDL.SDL_MouseMotionEvent evtMotion) =>
            OnMouseMove(new MousePositionAbsoluteInput { Position = new Vector2(evtMotion.x * Scale, evtMotion.y * Scale) });

        private unsafe void handleTextInputEvent(SDL.SDL_TextInputEvent evtText)
        {
            var ptr = new IntPtr(evtText.text);
            if (ptr == IntPtr.Zero)
                return;

            string text = Marshal.PtrToStringAnsi(ptr) ?? "";

            foreach (char c in text)
                OnKeyTyped(c);
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
                    OnKeyDown(new KeyboardKeyInput(key, true));
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    OnKeyUp(new KeyboardKeyInput(key, false));
                    break;
            }
        }

        private void handleWindowEvent(SDL.SDL_WindowEvent evtWindow)
        {
            switch (evtWindow.windowEvent)
            {
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
                    OnShown();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
                    OnHidden();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
                    checkCurrentDisplay();
                    validateScale(true);
                    OnMoved(new Point(evtWindow.data1, evtWindow.data2));
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                    checkCurrentDisplay();
                    validateScale(true);
                    OnResized();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                    OnWindowStateChanged();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                    mouseInWindow = true;
                    OnMouseEntered();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                    mouseInWindow = false;
                    OnMouseLeft();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                    OnFocusGained();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                    OnFocusLost();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                    OnClosed();
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
