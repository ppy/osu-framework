// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Input;
using osu.Framework.Platform.SDL2;
using osu.Framework.Platform.Windows.Native;
using osu.Framework.Threading;
using osuTK;
using osuTK.Input;
using SDL2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Default implementation of a desktop window, using SDL for windowing and graphics support.
    /// </summary>
    public class SDL2DesktopWindow : IWindow
    {
        internal IntPtr SDLWindowHandle { get; private set; } = IntPtr.Zero;

        private readonly IGraphicsBackend graphicsBackend;

        private bool focused;

        /// <summary>
        /// Whether the window currently has focus.
        /// </summary>
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

        /// <summary>
        /// Enables or disables vertical sync.
        /// </summary>
        public bool VerticalSync
        {
            get => graphicsBackend.VerticalSync;
            set => graphicsBackend.VerticalSync = value;
        }

        /// <summary>
        /// Returns true if window has been created.
        /// Returns false if the window has not yet been created, or has been closed.
        /// </summary>
        public bool Exists { get; private set; }

        public WindowMode DefaultWindowMode => Configuration.WindowMode.Windowed;

        /// <summary>
        /// Returns the window modes that the platform should support by default.
        /// </summary>
        protected virtual IEnumerable<WindowMode> DefaultSupportedWindowModes => Enum.GetValues(typeof(WindowMode)).OfType<WindowMode>();

        private Point position;

        /// <summary>
        /// Returns or sets the window's position in screen space. Only valid when in <see cref="osu.Framework.Configuration.WindowMode.Windowed"/>
        /// </summary>
        public Point Position
        {
            get => position;
            set
            {
                position = value;
                ScheduleCommand(() => SDL.SDL_SetWindowPosition(SDLWindowHandle, value.X, value.Y));
            }
        }

        private bool resizable = true;

        /// <summary>
        /// Returns or sets whether the window is resizable or not. Only valid when in <see cref="osu.Framework.Platform.WindowState.Normal"/>.
        /// </summary>
        public bool Resizable
        {
            get => resizable;
            set
            {
                if (resizable == value)
                    return;

                resizable = value;
                ScheduleCommand(() => SDL.SDL_SetWindowResizable(SDLWindowHandle, value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));
            }
        }

        private bool relativeMouseMode;

        /// <summary>
        /// Set the state of SDL2's RelativeMouseMode (https://wiki.libsdl.org/SDL_SetRelativeMouseMode).
        /// On all platforms, this will lock the mouse to the window (although escaping by setting <see cref="ConfineMouseMode"/> is still possible via a local implementation).
        /// On windows, this will use raw input if available.
        /// </summary>
        public bool RelativeMouseMode
        {
            get => relativeMouseMode;
            set
            {
                if (relativeMouseMode == value)
                    return;

                relativeMouseMode = value;
                ScheduleCommand(() => SDL.SDL_SetRelativeMouseMode(value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));
            }
        }

        private Size size = new Size(default_width, default_height);

        /// <summary>
        /// Returns or sets the window's internal size, before scaling.
        /// </summary>
        public Size Size
        {
            get => size;
            private set
            {
                if (value.Equals(size)) return;

                size = value;
                ScheduleEvent(() => OnResized());
            }
        }

        /// <summary>
        /// Provides a bindable that controls the window's <see cref="CursorStateBindable"/>.
        /// </summary>
        public Bindable<CursorState> CursorStateBindable { get; } = new Bindable<CursorState>();

        public CursorState CursorState
        {
            get => CursorStateBindable.Value;
            set => CursorStateBindable.Value = value;
        }

        public Bindable<Display> CurrentDisplayBindable { get; } = new Bindable<Display>();

        public Bindable<WindowMode> WindowMode { get; } = new Bindable<WindowMode>();

        private readonly BindableBool isActive = new BindableBool(true);

        public IBindable<bool> IsActive => isActive;

        private readonly BindableBool cursorInWindow = new BindableBool(true);

        public IBindable<bool> CursorInWindow => cursorInWindow;

        public IBindableList<WindowMode> SupportedWindowModes { get; }

        public BindableSafeArea SafeAreaPadding { get; } = new BindableSafeArea();

        public virtual Point PointToClient(Point point) => point;

        public virtual Point PointToScreen(Point point) => point;

        private const int default_width = 1366;
        private const int default_height = 768;

        private const int default_icon_size = 256;

        private readonly Scheduler commandScheduler = new Scheduler();
        private readonly Scheduler eventScheduler = new Scheduler();

        private readonly Dictionary<int, SDL2ControllerBindings> controllers = new Dictionary<int, SDL2ControllerBindings>();

        private string title = string.Empty;

        /// <summary>
        /// Gets and sets the window title.
        /// </summary>
        public string Title
        {
            get => title;
            set
            {
                title = value;
                ScheduleCommand(() => SDL.SDL_SetWindowTitle(SDLWindowHandle, title));
            }
        }

        private bool visible;

        /// <summary>
        /// Enables or disables the window visibility.
        /// </summary>
        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                ScheduleCommand(() =>
                {
                    if (value)
                        SDL.SDL_ShowWindow(SDLWindowHandle);
                    else
                        SDL.SDL_HideWindow(SDLWindowHandle);
                });
            }
        }

        private bool cursorVisible = true;

        /// <summary>
        /// Returns or sets the cursor's visibility within the window.
        /// </summary>
        public virtual bool CursorVisible
        {
            get => cursorVisible;
            set
            {
                cursorVisible = value;
                ScheduleCommand(() => SDL.SDL_ShowCursor(value ? SDL.SDL_ENABLE : SDL.SDL_DISABLE));
            }
        }

        private bool cursorConfined;

        /// <summary>
        /// Returns or sets whether the cursor is confined to the window's
        /// drawable area.
        /// </summary>
        public bool CursorConfined
        {
            get => cursorConfined;
            set
            {
                cursorConfined = value;
                ScheduleCommand(() => SDL.SDL_SetWindowGrab(SDLWindowHandle, value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));
            }
        }

        private WindowState windowState = WindowState.Normal;

        private WindowState? pendingWindowState;

        /// <summary>
        /// Returns or sets the window's current <see cref="WindowState"/>.
        /// </summary>
        public WindowState WindowState
        {
            get => windowState;
            set
            {
                if (pendingWindowState == null && windowState == value)
                    return;

                pendingWindowState = value;
            }
        }

        /// <summary>
        /// Stores whether the window used to be in maximised state or not.
        /// Used to properly decide what window state to pick when switching to windowed mode (see <see cref="WindowMode"/> change event)
        /// </summary>
        private bool windowMaximised;

        /// <summary>
        /// Returns the drawable area, after scaling.
        /// </summary>
        public Size ClientSize => new Size(Size.Width, Size.Height);

        public float Scale = 1;

        /// <summary>
        /// Queries the physical displays and their supported resolutions.
        /// </summary>
        public IEnumerable<Display> Displays => Enumerable.Range(0, SDL.SDL_GetNumVideoDisplays()).Select(displayFromSDL);

        /// <summary>
        /// Gets the <see cref="Display"/> that has been set as "primary" or "default" in the operating system.
        /// </summary>
        public virtual Display PrimaryDisplay => Displays.First();

        private Display currentDisplay;
        private int displayIndex = -1;

        /// <summary>
        /// Gets or sets the <see cref="Display"/> that this window is currently on.
        /// </summary>
        public Display CurrentDisplay { get; private set; }

        public readonly Bindable<ConfineMouseMode> ConfineMouseMode = new Bindable<ConfineMouseMode>();

        private DisplayMode currentDisplayMode;

        /// <summary>
        /// Gets or sets the <see cref="DisplayMode"/> for the display that this window is currently on.
        /// </summary>
        public DisplayMode CurrentDisplayMode
        {
            get => currentDisplayMode;
            set
            {
                currentDisplayMode = value;

                // todo: proper handling of this, if we decide we want it.
                pendingWindowState = windowState;
            }
        }

        /// <summary>
        /// Gets the native window handle as provided by the operating system.
        /// </summary>
        public IntPtr WindowHandle
        {
            get
            {
                if (SDLWindowHandle == IntPtr.Zero)
                    return IntPtr.Zero;

                var wmInfo = getWindowWMInfo();

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

        private SDL.SDL_SysWMinfo getWindowWMInfo()
        {
            if (SDLWindowHandle == IntPtr.Zero)
                return default;

            var wmInfo = new SDL.SDL_SysWMinfo();
            SDL.SDL_GetWindowWMInfo(SDLWindowHandle, ref wmInfo);
            return wmInfo;
        }

        private Rectangle windowDisplayBounds
        {
            get
            {
                SDL.SDL_GetDisplayBounds(displayIndex, out var rect);
                return new Rectangle(rect.x, rect.y, rect.w, rect.h);
            }
        }

        public bool CapsLockPressed => SDL.SDL_GetModState().HasFlagFast(SDL.SDL_Keymod.KMOD_CAPS);

        private bool firstDraw = true;

        private readonly BindableSize sizeFullscreen = new BindableSize();
        private readonly BindableSize sizeWindowed = new BindableSize();
        private readonly BindableDouble windowPositionX = new BindableDouble();
        private readonly BindableDouble windowPositionY = new BindableDouble();
        private readonly Bindable<DisplayIndex> windowDisplayIndexBindable = new Bindable<DisplayIndex>();

        public SDL2DesktopWindow()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER);

            graphicsBackend = CreateGraphicsBackend();

            SupportedWindowModes = new BindableList<WindowMode>(DefaultSupportedWindowModes);

            CursorStateBindable.ValueChanged += evt =>
            {
                CursorVisible = !evt.NewValue.HasFlagFast(CursorState.Hidden);
                CursorConfined = evt.NewValue.HasFlagFast(CursorState.Confined);
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
        /// Creates the window and initialises the graphics backend.
        /// </summary>
        public virtual void Create()
        {
            SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | // shown after first swap to avoid white flash on startup (windows)
                                        WindowState.ToFlags();

            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_NO_CLOSE_ON_ALT_F4, "1");

            SDLWindowHandle = SDL.SDL_CreateWindow(title, Position.X, Position.Y, Size.Width, Size.Height, flags);

            Exists = true;

            MouseEntered += () => cursorInWindow.Value = true;
            MouseLeft += () => cursorInWindow.Value = false;

            graphicsBackend.Initialise(this);

            updateWindowSpecifics();
            updateWindowSize();
            WindowMode.TriggerChange();
        }

        // reference must be kept to avoid GC, see https://stackoverflow.com/a/6193914
        [UsedImplicitly]
        private SDL.SDL_EventFilter eventFilterDelegate;

        /// <summary>
        /// Starts the window's run loop.
        /// </summary>
        public void Run()
        {
            // polling via SDL_PollEvent blocks on resizes (https://stackoverflow.com/a/50858339)
            SDL.SDL_SetEventFilter(eventFilterDelegate = (_, eventPtr) =>
            {
                // ReSharper disable once PossibleNullReferenceException
                var e = (SDL.SDL_Event)Marshal.PtrToStructure(eventPtr, typeof(SDL.SDL_Event));

                if (e.type == SDL.SDL_EventType.SDL_WINDOWEVENT && e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                {
                    // This function will be invoked before the SDL internal states are all changed. (as documented here: https://wiki.libsdl.org/SDL_SetEventFilter)
                    // Therefore we should only update the client size without saving to config, as we don't know what state the window would end up in.
                    updateWindowSize();
                    return 0;
                }

                return 1;
            }, IntPtr.Zero);

            while (Exists)
            {
                commandScheduler.Update();

                if (!Exists)
                    break;

                if (pendingWindowState != null)
                    updateWindowSpecifics();

                pollSDLEvents();

                if (!cursorInWindow.Value)
                    pollMouse();

                eventScheduler.Update();

                OnUpdate();
            }

            OnExited();

            if (SDLWindowHandle != IntPtr.Zero)
                SDL.SDL_DestroyWindow(SDLWindowHandle);

            SDL.SDL_Quit();
        }

        /// <summary>
        /// Updates the client size and the scale according to the window.
        /// </summary>
        /// <returns>Whether the window size has been changed after updating.</returns>
        private void updateWindowSize()
        {
            SDL.SDL_GL_GetDrawableSize(SDLWindowHandle, out var w, out var h);

            SDL.SDL_GetWindowSize(SDLWindowHandle, out var actualW, out var _);
            Scale = (float)w / actualW;

            Size = new Size(w, h);
        }

        /// <summary>
        /// Forcefully closes the window.
        /// </summary>
        public void Close() => ScheduleCommand(() => Exists = false);

        /// <summary>
        /// Attempts to close the window.
        /// </summary>
        public void RequestClose() => ScheduleEvent(() =>
        {
            if (!OnExitRequested())
                Close();
        });

        public void SwapBuffers()
        {
            graphicsBackend.SwapBuffers();

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
        public void MakeCurrent() => graphicsBackend.MakeCurrent();

        /// <summary>
        /// Requests that the current context be cleared.
        /// </summary>
        public void ClearCurrent() => graphicsBackend.ClearCurrent();

        private void enqueueJoystickAxisInput(JoystickAxisSource axisSource, short axisValue)
        {
            // SDL reports axis values in the range short.MinValue to short.MaxValue, so we scale and clamp it to the range of -1f to 1f
            var clamped = Math.Clamp((float)axisValue / short.MaxValue, -1f, 1f);
            ScheduleEvent(() => OnJoystickAxisChanged(new JoystickAxis(axisSource, clamped)));
        }

        private void enqueueJoystickButtonInput(JoystickButton button, bool isPressed)
        {
            if (isPressed)
                ScheduleEvent(() => OnJoystickButtonDown(button));
            else
                ScheduleEvent(() => OnJoystickButtonUp(button));
        }

        /// <summary>
        /// Attempts to set the window's icon to the specified image.
        /// </summary>
        /// <param name="image">An <see cref="Image{Rgba32}"/> to set as the window icon.</param>
        private unsafe void setSDLIcon(Image<Rgba32> image)
        {
            var pixelMemory = image.CreateReadOnlyPixelMemory();
            var imageSize = image.Size();

            ScheduleCommand(() =>
            {
                var pixelSpan = pixelMemory.Span;

                IntPtr surface;
                fixed (Rgba32* ptr = pixelSpan)
                    surface = SDL.SDL_CreateRGBSurfaceFrom(new IntPtr(ptr), imageSize.Width, imageSize.Height, 32, imageSize.Width * 4, 0xff, 0xff00, 0xff0000, 0xff000000);

                SDL.SDL_SetWindowIcon(SDLWindowHandle, surface);
                SDL.SDL_FreeSurface(surface);
            });
        }

        private Point previousPolledPoint = Point.Empty;

        private void pollMouse()
        {
            SDL.SDL_GetGlobalMouseState(out var x, out var y);
            if (previousPolledPoint.X == x && previousPolledPoint.Y == y)
                return;

            previousPolledPoint = new Point(x, y);

            var pos = WindowMode.Value == Configuration.WindowMode.Windowed ? Position : windowDisplayBounds.Location;
            var rx = x - pos.X;
            var ry = y - pos.Y;

            ScheduleEvent(() => OnMouseMove(new Vector2(rx * Scale, ry * Scale)));
        }

        #region SDL Event Handling

        /// <summary>
        /// Adds an <see cref="Action"/> to the <see cref="Scheduler"/> expected to handle event callbacks.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to execute.</param>
        protected void ScheduleEvent(Action action) => eventScheduler.Add(action, false);

        protected void ScheduleCommand(Action action) => commandScheduler.Add(action, false);

        /// <summary>
        /// Poll for all pending events.
        /// </summary>
        private void pollSDLEvents()
        {
            while (SDL.SDL_PollEvent(out var e) > 0)
                handleSDLEvent(e);
        }

        private void handleSDLEvent(SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_QUIT:
                case SDL.SDL_EventType.SDL_APP_TERMINATING:
                    handleQuitEvent(e.quit);
                    break;

                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                    handleWindowEvent(e.window);
                    break;

                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:
                    handleKeyboardEvent(e.key);
                    break;

                case SDL.SDL_EventType.SDL_TEXTEDITING:
                    handleTextEditingEvent(e.edit);
                    break;

                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    handleTextInputEvent(e.text);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    handleMouseMotionEvent(e.motion);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    handleMouseButtonEvent(e.button);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    handleMouseWheelEvent(e.wheel);
                    break;

                case SDL.SDL_EventType.SDL_JOYAXISMOTION:
                    handleJoyAxisEvent(e.jaxis);
                    break;

                case SDL.SDL_EventType.SDL_JOYBALLMOTION:
                    handleJoyBallEvent(e.jball);
                    break;

                case SDL.SDL_EventType.SDL_JOYHATMOTION:
                    handleJoyHatEvent(e.jhat);
                    break;

                case SDL.SDL_EventType.SDL_JOYBUTTONDOWN:
                case SDL.SDL_EventType.SDL_JOYBUTTONUP:
                    handleJoyButtonEvent(e.jbutton);
                    break;

                case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                case SDL.SDL_EventType.SDL_JOYDEVICEREMOVED:
                    handleJoyDeviceEvent(e.jdevice);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                    handleControllerAxisEvent(e.caxis);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    handleControllerButtonEvent(e.cbutton);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                    handleControllerDeviceEvent(e.cdevice);
                    break;

                case SDL.SDL_EventType.SDL_FINGERDOWN:
                case SDL.SDL_EventType.SDL_FINGERUP:
                case SDL.SDL_EventType.SDL_FINGERMOTION:
                    handleTouchFingerEvent(e.tfinger);
                    break;

                case SDL.SDL_EventType.SDL_DROPFILE:
                case SDL.SDL_EventType.SDL_DROPTEXT:
                case SDL.SDL_EventType.SDL_DROPBEGIN:
                case SDL.SDL_EventType.SDL_DROPCOMPLETE:
                    handleDropEvent(e.drop);
                    break;
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
                    controllers[instanceID] = new SDL2ControllerBindings(joystick, controller);
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
                    controllers[instanceID] = new SDL2ControllerBindings(joystick, IntPtr.Zero);
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

        private void handleMouseMotionEvent(SDL.SDL_MouseMotionEvent evtMotion)
        {
            if (SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_FALSE)
                ScheduleEvent(() => OnMouseMove(new Vector2(evtMotion.x * Scale, evtMotion.y * Scale)));
            else
                ScheduleEvent(() => OnMouseMoveRelative(new Vector2(evtMotion.xrel * Scale, evtMotion.yrel * Scale)));
        }

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

            if (key == Key.Unknown)
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
            updateWindowSpecifics();

            switch (evtWindow.windowEvent)
            {
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
                    ScheduleEvent(OnShown);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
                    ScheduleEvent(OnHidden);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
                    // explicitly requery as there are occasions where what SDL has provided us with is not up-to-date.
                    SDL.SDL_GetWindowPosition(SDLWindowHandle, out int x, out int y);
                    var newPosition = new Point(x, y);

                    if (WindowMode.Value == Configuration.WindowMode.Windowed && !newPosition.Equals(Position))
                    {
                        position = newPosition;
                        storeWindowPositionToConfig();
                        ScheduleEvent(() => OnMoved(newPosition));
                    }

                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                    updateWindowSize();
                    if (WindowState == WindowState.Normal)
                        storeWindowSizeToConfig();

                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                    cursorInWindow.Value = true;
                    ScheduleEvent(OnMouseEntered);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                    cursorInWindow.Value = false;
                    ScheduleEvent(OnMouseLeft);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                    ScheduleEvent(OnFocusGained);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                    ScheduleEvent(OnFocusLost);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                    break;
            }
        }

        /// <summary>
        /// Should be run on a regular basis to check for external window state changes.
        /// </summary>
        private void updateWindowSpecifics()
        {
            // don't attempt to run before the window is initialised, as Create() will do so anyway.
            if (SDLWindowHandle == IntPtr.Zero)
                return;

            var stateBefore = windowState;

            // check for a pending user state change and give precedence.
            if (pendingWindowState != null)
            {
                windowState = pendingWindowState.Value;
                pendingWindowState = null;

                updateWindowStateAndSize();
            }
            else
            {
                windowState = ((SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(SDLWindowHandle)).ToWindowState();
            }

            if (windowState != stateBefore)
            {
                ScheduleEvent(() => OnWindowStateChanged(windowState));
                updateMaximisedState();
            }

            int newDisplayIndex = SDL.SDL_GetWindowDisplayIndex(SDLWindowHandle);

            if (displayIndex != newDisplayIndex)
            {
                displayIndex = newDisplayIndex;
                currentDisplay = Displays.ElementAtOrDefault(displayIndex) ?? PrimaryDisplay;
                ScheduleEvent(() => OnDisplayChanged(currentDisplay));
            }
        }

        /// <summary>
        /// Should be run after a local window state change, to propagate the correct SDL actions.
        /// </summary>
        private void updateWindowStateAndSize()
        {
            // this reset is required even on changing from one fullscreen resolution to another.
            // if it is not included, the GL context will not get the correct size.
            // this is mentioned by multiple sources as an SDL issue, which seems to resolve by similar means (see https://discourse.libsdl.org/t/sdl-setwindowsize-does-not-work-in-fullscreen/20711/4).
            SDL.SDL_SetWindowBordered(SDLWindowHandle, SDL.SDL_bool.SDL_TRUE);
            SDL.SDL_SetWindowFullscreen(SDLWindowHandle, (uint)SDL.SDL_bool.SDL_FALSE);

            switch (windowState)
            {
                case WindowState.Normal:
                    Size = (sizeWindowed.Value * Scale).ToSize();

                    SDL.SDL_RestoreWindow(SDLWindowHandle);
                    SDL.SDL_SetWindowSize(SDLWindowHandle, sizeWindowed.Value.Width, sizeWindowed.Value.Height);
                    SDL.SDL_SetWindowResizable(SDLWindowHandle, Resizable ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);

                    readWindowPositionFromConfig();
                    break;

                case WindowState.Fullscreen:
                    var closestMode = getClosestDisplayMode(sizeFullscreen.Value, currentDisplayMode.RefreshRate, currentDisplay.Index);

                    Size = new Size(closestMode.w, closestMode.h);

                    SDL.SDL_SetWindowDisplayMode(SDLWindowHandle, ref closestMode);
                    SDL.SDL_SetWindowFullscreen(SDLWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
                    break;

                case WindowState.FullscreenBorderless:
                    Size = SetBorderless();
                    break;

                case WindowState.Maximised:
                    SDL.SDL_MaximizeWindow(SDLWindowHandle);

                    SDL.SDL_GL_GetDrawableSize(SDLWindowHandle, out int w, out int h);
                    Size = new Size(w, h);
                    break;

                case WindowState.Minimised:
                    SDL.SDL_MinimizeWindow(SDLWindowHandle);
                    break;
            }

            updateMaximisedState();

            if (SDL.SDL_GetWindowDisplayMode(SDLWindowHandle, out var mode) >= 0)
                currentDisplayMode = new DisplayMode(mode.format.ToString(), new Size(mode.w, mode.h), 32, mode.refresh_rate, displayIndex, displayIndex);
        }

        private void updateMaximisedState()
        {
            if (windowState == WindowState.Normal || windowState == WindowState.Maximised)
                windowMaximised = windowState == WindowState.Maximised;
        }

        private void readWindowPositionFromConfig()
        {
            if (WindowState != WindowState.Normal)
                return;

            var configPosition = new Vector2((float)windowPositionX.Value, (float)windowPositionY.Value);

            var displayBounds = CurrentDisplay.Bounds;
            var windowSize = sizeWindowed.Value;
            var windowX = (int)Math.Round((displayBounds.Width - windowSize.Width) * configPosition.X);
            var windowY = (int)Math.Round((displayBounds.Height - windowSize.Height) * configPosition.Y);

            Position = new Point(windowX + displayBounds.X, windowY + displayBounds.Y);
        }

        private void storeWindowPositionToConfig()
        {
            if (WindowState != WindowState.Normal)
                return;

            var displayBounds = CurrentDisplay.Bounds;

            var windowX = Position.X - displayBounds.X;
            var windowY = Position.Y - displayBounds.Y;

            var windowSize = sizeWindowed.Value;

            windowPositionX.Value = displayBounds.Width > windowSize.Width ? (float)windowX / (displayBounds.Width - windowSize.Width) : 0;
            windowPositionY.Value = displayBounds.Height > windowSize.Height ? (float)windowY / (displayBounds.Height - windowSize.Height) : 0;
        }

        /// <summary>
        /// Set to <c>true</c> while the window size is being stored to config to avoid bindable feedback.
        /// </summary>
        private bool storingSizeToConfig;

        private void storeWindowSizeToConfig()
        {
            storingSizeToConfig = true;
            sizeWindowed.Value = (Size / Scale).ToSize();
            storingSizeToConfig = false;
        }

        /// <summary>
        /// Prepare display of a borderless window.
        /// </summary>
        /// <returns>
        /// The size of the borderless window's draw area.
        /// </returns>
        protected virtual Size SetBorderless()
        {
            // this is a generally sane method of handling borderless, and works well on macOS and linux.
            SDL.SDL_SetWindowFullscreen(SDLWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);

            return currentDisplay.Bounds.Size;
        }

        protected void OnHidden() { }

        protected void OnShown() { }

        protected void OnWindowStateChanged(WindowState state) => WindowStateChanged?.Invoke(state);

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

        protected virtual IGraphicsBackend CreateGraphicsBackend() => new SDL2GraphicsBackend();

        public void SetupWindow(FrameworkConfigManager config)
        {
            CurrentDisplayBindable.ValueChanged += evt =>
            {
                windowDisplayIndexBindable.Value = (DisplayIndex)evt.NewValue.Index;
                windowPositionX.Value = 0.5;
                windowPositionY.Value = 0.5;
            };

            config.BindWith(FrameworkSetting.LastDisplayDevice, windowDisplayIndexBindable);
            windowDisplayIndexBindable.BindValueChanged(evt => CurrentDisplay = Displays.ElementAtOrDefault((int)evt.NewValue) ?? PrimaryDisplay, true);

            sizeFullscreen.ValueChanged += evt =>
            {
                if (storingSizeToConfig) return;
                if (windowState != WindowState.Fullscreen) return;

                pendingWindowState = windowState;
            };

            sizeWindowed.ValueChanged += evt =>
            {
                if (storingSizeToConfig) return;
                if (windowState != WindowState.Normal) return;

                pendingWindowState = windowState;
            };

            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);
            config.BindWith(FrameworkSetting.WindowedSize, sizeWindowed);

            config.BindWith(FrameworkSetting.WindowedPositionX, windowPositionX);
            config.BindWith(FrameworkSetting.WindowedPositionY, windowPositionY);

            config.BindWith(FrameworkSetting.WindowMode, WindowMode);
            config.BindWith(FrameworkSetting.ConfineMouseMode, ConfineMouseMode);

            WindowMode.BindValueChanged(evt =>
            {
                switch (evt.NewValue)
                {
                    case Configuration.WindowMode.Fullscreen:
                        WindowState = WindowState.Fullscreen;
                        break;

                    case Configuration.WindowMode.Borderless:
                        WindowState = WindowState.FullscreenBorderless;
                        break;

                    case Configuration.WindowMode.Windowed:
                        WindowState = windowMaximised ? WindowState.Maximised : WindowState.Normal;
                        break;
                }

                updateConfineMode();
            });

            ConfineMouseMode.BindValueChanged(_ => updateConfineMode());
        }

        public void CycleMode()
        {
            var currentValue = WindowMode.Value;

            do
            {
                switch (currentValue)
                {
                    case Configuration.WindowMode.Windowed:
                        currentValue = Configuration.WindowMode.Borderless;
                        break;

                    case Configuration.WindowMode.Borderless:
                        currentValue = Configuration.WindowMode.Fullscreen;
                        break;

                    case Configuration.WindowMode.Fullscreen:
                        currentValue = Configuration.WindowMode.Windowed;
                        break;
                }
            } while (!SupportedWindowModes.Contains(currentValue) && currentValue != WindowMode.Value);

            WindowMode.Value = currentValue;
        }

        /// <summary>
        /// Update the host window manager's cursor position based on a location relative to window coordinates.
        /// </summary>
        /// <param name="position">A position inside the window.</param>
        public void UpdateMousePosition(Vector2 position) => ScheduleCommand(() =>
            SDL.SDL_WarpMouseInWindow(SDLWindowHandle, (int)(position.X / Scale), (int)(position.Y / Scale)));

        public void SetIconFromStream(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                ms.Position = 0;

                var imageInfo = Image.Identify(ms);

                if (imageInfo != null)
                    SetIconFromImage(Image.Load<Rgba32>(ms.GetBuffer()));
                else if (IconGroup.TryParse(ms.GetBuffer(), out var iconGroup))
                    SetIconFromGroup(iconGroup);
            }
        }

        internal virtual void SetIconFromGroup(IconGroup iconGroup)
        {
            // LoadRawIcon returns raw PNG data if available, which avoids any Windows-specific pinvokes
            var bytes = iconGroup.LoadRawIcon(default_icon_size, default_icon_size);
            if (bytes == null)
                return;

            SetIconFromImage(Image.Load<Rgba32>(bytes));
        }

        internal virtual void SetIconFromImage(Image<Rgba32> iconImage) => setSDLIcon(iconImage);

        private void updateConfineMode()
        {
            bool confine = false;

            switch (ConfineMouseMode.Value)
            {
                case Input.ConfineMouseMode.Fullscreen:
                    confine = WindowMode.Value != Configuration.WindowMode.Windowed;
                    break;

                case Input.ConfineMouseMode.Always:
                    confine = true;
                    break;
            }

            if (confine)
                CursorStateBindable.Value |= CursorState.Confined;
            else
                CursorStateBindable.Value &= ~CursorState.Confined;
        }

        #region Helper functions

        private SDL.SDL_DisplayMode getClosestDisplayMode(Size size, int refreshRate, int displayIndex)
        {
            var targetMode = new SDL.SDL_DisplayMode { w = size.Width, h = size.Height, refresh_rate = refreshRate };

            if (SDL.SDL_GetClosestDisplayMode(displayIndex, ref targetMode, out var mode) != IntPtr.Zero)
                return mode;

            // fallback to current display's native bounds
            targetMode.w = currentDisplay.Bounds.Width;
            targetMode.h = currentDisplay.Bounds.Height;
            targetMode.refresh_rate = 0;

            if (SDL.SDL_GetClosestDisplayMode(displayIndex, ref targetMode, out mode) != IntPtr.Zero)
                return mode;

            // finally return the current mode if everything else fails.
            // not sure this is required.
            if (SDL.SDL_GetWindowDisplayMode(SDLWindowHandle, out mode) >= 0)
                return mode;

            throw new InvalidOperationException("couldn't retrieve valid display mode");
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
        /// Invoked after the window's state has changed.
        /// </summary>
        public event Action<WindowState> WindowStateChanged;

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
        /// Invoked when the user moves the mouse cursor within the window (via relative / raw input).
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

        protected void OnUpdate() => Update?.Invoke();

        protected void OnResized() => Resized?.Invoke();

        protected bool OnExitRequested() => ExitRequested?.Invoke() ?? false;
        protected void OnExited() => Exited?.Invoke();
        protected void OnMouseEntered() => MouseEntered?.Invoke();
        protected void OnMouseLeft() => MouseLeft?.Invoke();
        protected void OnMoved(Point point) => Moved?.Invoke(point);
        protected void OnMouseWheel(Vector2 delta, bool precise) => MouseWheel?.Invoke(delta, precise);
        protected void OnMouseMove(Vector2 position) => MouseMove?.Invoke(position);
        protected void OnMouseMoveRelative(Vector2 position) => MouseMoveRelative?.Invoke(position);
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

        public void Dispose()
        {
        }
    }
}
