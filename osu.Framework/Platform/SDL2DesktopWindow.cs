// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input;
using osu.Framework.Input.States;
using osu.Framework.Logging;
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
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using Size = System.Drawing.Size;

// ReSharper disable UnusedParameter.Local
// (Class regularly handles native events where we don't consume all parameters)

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

                if (value && !CursorState.HasFlagFast(CursorState.Hidden))
                    throw new InvalidOperationException($"Cannot set {nameof(RelativeMouseMode)} to true when the cursor is not hidden via {nameof(CursorState)}.");

                relativeMouseMode = value;
                ScheduleCommand(() => SDL.SDL_SetRelativeMouseMode(value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));
                updateCursorConfinement();
            }
        }

        /// <summary>
        /// Controls whether the mouse is automatically captured when buttons are pressed and the cursor is outside the window.
        /// Only works with <see cref="RelativeMouseMode"/> disabled.
        /// </summary>
        /// <remarks>
        /// If the cursor leaves the window while it's captured, <see cref="SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE"/> is not sent until the button(s) are released.
        /// And if the cursor leaves and enters the window while captured, <see cref="SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER"/> is not sent either.
        /// We disable relative mode when the cursor exits window bounds (not on the event), but we only enable it again on <see cref="SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER"/>.
        /// The above culminate in <see cref="RelativeMouseMode"/> staying off when the cursor leaves and enters the window bounds when any buttons are pressed.
        /// This is an invalid state, as the cursor is inside the window, and <see cref="RelativeMouseMode"/> is off.
        /// </remarks>
        internal bool MouseAutoCapture
        {
            set => ScheduleCommand(() => SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_AUTO_CAPTURE, value ? "1" : "0"));
        }

        private Size size = new Size(default_width, default_height);

        /// <summary>
        /// Returns or sets the window's internal size, before scaling.
        /// </summary>
        public virtual Size Size
        {
            get => size;
            protected set
            {
                if (value.Equals(size)) return;

                size = value;
                Resized?.Invoke();
            }
        }

        public Size MinSize
        {
            get => sizeWindowed.MinValue;
            set => sizeWindowed.MinValue = value;
        }

        public Size MaxSize
        {
            get => sizeWindowed.MaxValue;
            set => sizeWindowed.MaxValue = value;
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

        private RectangleF? cursorConfineRect;

        public RectangleF? CursorConfineRect
        {
            get => cursorConfineRect;
            set
            {
                cursorConfineRect = value;
                updateCursorConfinement();
            }
        }

        public Bindable<Display> CurrentDisplayBindable { get; } = new Bindable<Display>();

        public Bindable<WindowMode> WindowMode { get; } = new Bindable<WindowMode>();

        private readonly BindableBool isActive = new BindableBool();

        public IBindable<bool> IsActive => isActive;

        private readonly BindableBool cursorInWindow = new BindableBool();

        public IBindable<bool> CursorInWindow => cursorInWindow;

        public IBindableList<WindowMode> SupportedWindowModes { get; }

        public BindableSafeArea SafeAreaPadding { get; } = new BindableSafeArea();

        public virtual Point PointToClient(Point point) => point;

        public virtual Point PointToScreen(Point point) => point;

        private const int default_width = 1366;
        private const int default_height = 768;

        private const int default_icon_size = 256;

        /// <summary>
        /// Scheduler for actions to run before the next event loop.
        /// </summary>
        private readonly Scheduler commandScheduler = new Scheduler();

        /// <summary>
        /// Scheduler for actions to run at the end of the current event loop.
        /// </summary>
        protected readonly Scheduler EventScheduler = new Scheduler();

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

        private void updateCursorVisibility(bool visible) =>
            ScheduleCommand(() => SDL.SDL_ShowCursor(visible ? SDL.SDL_ENABLE : SDL.SDL_DISABLE));

        /// <summary>
        /// Updates OS cursor confinement based on the current <see cref="CursorState"/>, <see cref="CursorConfineRect"/> and <see cref="RelativeMouseMode"/>.
        /// </summary>
        private void updateCursorConfinement()
        {
            bool confined = CursorState.HasFlagFast(CursorState.Confined);

            ScheduleCommand(() => SDL.SDL_SetWindowGrab(SDLWindowHandle, confined ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));

            // Don't use SDL_SetWindowMouseRect when relative mode is enabled, as relative mode already confines the OS cursor to the window.
            // This is fine for our use case, as UserInputManager will clamp the mouse position.
            if (CursorConfineRect != null && confined && !RelativeMouseMode)
            {
                var rect = ((RectangleI)(CursorConfineRect / Scale)).ToSDLRect();
                ScheduleCommand(() => SDL.SDL_SetWindowMouseRect(SDLWindowHandle, ref rect));
            }
            else
            {
                ScheduleCommand(() => SDL.SDL_SetWindowMouseRect(SDLWindowHandle, IntPtr.Zero));
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

        private readonly Bindable<DisplayMode> currentDisplayMode = new Bindable<DisplayMode>();

        /// <summary>
        /// The <see cref="DisplayMode"/> for the display that this window is currently on.
        /// </summary>
        public IBindable<DisplayMode> CurrentDisplayMode => currentDisplayMode;

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

        // references must be kept to avoid GC, see https://stackoverflow.com/a/6193914

        [UsedImplicitly]
        private SDL.SDL_LogOutputFunction logOutputDelegate;

        [UsedImplicitly]
        private SDL.SDL_EventFilter eventFilterDelegate;

        public SDL2DesktopWindow()
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER) < 0)
            {
                throw new InvalidOperationException($"Failed to initialise SDL: {SDL.SDL_GetError()}");
            }

            SDL.SDL_LogSetPriority((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_ERROR, SDL.SDL_LogPriority.SDL_LOG_PRIORITY_DEBUG);
            SDL.SDL_LogSetOutputFunction(logOutputDelegate = (_, categoryInt, priority, messagePtr) =>
            {
                var category = (SDL.SDL_LogCategory)categoryInt;
                string message = Marshal.PtrToStringUTF8(messagePtr);

                Logger.Log($@"SDL {category.ReadableName()} log [{priority.ReadableName()}]: {message}");
            }, IntPtr.Zero);

            graphicsBackend = CreateGraphicsBackend();

            SupportedWindowModes = new BindableList<WindowMode>(DefaultSupportedWindowModes);

            CursorStateBindable.ValueChanged += evt =>
            {
                updateCursorVisibility(!evt.NewValue.HasFlagFast(CursorState.Hidden));
                updateCursorConfinement();
            };

            populateJoysticks();
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
            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_MINIMIZE_ON_FOCUS_LOSS, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_IME_SHOW_UI, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_RELATIVE_MODE_CENTER, "0");
            SDL.SDL_SetHint(SDL.SDL_HINT_TOUCH_MOUSE_EVENTS, "0");

            // we want text input to only be active when SDL2DesktopWindowTextInput is active.
            // SDL activates it by default on some platforms: https://github.com/libsdl-org/SDL/blob/release-2.0.16/src/video/SDL_video.c#L573-L582
            // so we deactivate it on startup.
            SDL.SDL_StopTextInput();

            graphicsBackend.InitialiseBeforeWindowCreation();
            SDLWindowHandle = SDL.SDL_CreateWindow(title, Position.X, Position.Y, Size.Width, Size.Height, flags);

            Exists = true;

            graphicsBackend.Initialise(this);

            updateWindowSpecifics();
            updateWindowSize();

            sizeWindowed.MinValueChanged += min =>
            {
                if (min.Width < 0 || min.Height < 0)
                    throw new InvalidOperationException($"Expected zero or positive size, got {min}");

                if (min.Width > sizeWindowed.MaxValue.Width || min.Height > sizeWindowed.MaxValue.Height)
                    throw new InvalidOperationException($"Expected a size less than max window size ({sizeWindowed.MaxValue}), got {min}");

                ScheduleCommand(() => SDL.SDL_SetWindowMinimumSize(SDLWindowHandle, min.Width, min.Height));
            };

            sizeWindowed.MaxValueChanged += max =>
            {
                if (max.Width <= 0 || max.Height <= 0)
                    throw new InvalidOperationException($"Expected positive size, got {max}");

                if (max.Width < sizeWindowed.MinValue.Width || max.Height < sizeWindowed.MinValue.Height)
                    throw new InvalidOperationException($"Expected a size greater than min window size ({sizeWindowed.MinValue}), got {max}");

                ScheduleCommand(() => SDL.SDL_SetWindowMaximumSize(SDLWindowHandle, max.Width, max.Height));
            };

            sizeWindowed.TriggerChange();

            WindowMode.TriggerChange();
        }

        /// <summary>
        /// Starts the window's run loop.
        /// </summary>
        public void Run()
        {
            SDL.SDL_SetEventFilter(eventFilterDelegate = (_, eventPtr) =>
            {
                var e = Marshal.PtrToStructure<SDL.SDL_Event>(eventPtr);
                OnSDLEvent?.Invoke(e);

                return 1;
            }, IntPtr.Zero);

            // polling via SDL_PollEvent blocks on resizes (https://stackoverflow.com/a/50858339)
            OnSDLEvent += e =>
            {
                if (e.type == SDL.SDL_EventType.SDL_WINDOWEVENT && e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                {
                    updateWindowSize();
                }
            };

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

                EventScheduler.Update();

                Update?.Invoke();
            }

            Exited?.Invoke();

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
            SDL.SDL_GL_GetDrawableSize(SDLWindowHandle, out int w, out int h);
            SDL.SDL_GetWindowSize(SDLWindowHandle, out int actualW, out int _);

            // When minimised on windows, values may be zero.
            // If we receive zeroes for either of these, it seems safe to completely ignore them.
            if (actualW <= 0 || w <= 0)
                return;

            Scale = (float)w / actualW;
            Size = new Size(w, h);

            // This function may be invoked before the SDL internal states are all changed. (as documented here: https://wiki.libsdl.org/SDL_SetEventFilter)
            // Scheduling the store to config until after the event poll has run will ensure the window is in the correct state.
            EventScheduler.AddOnce(storeWindowSizeToConfig);
        }

        /// <summary>
        /// Forcefully closes the window.
        /// </summary>
        public void Close() => ScheduleCommand(() => Exists = false);

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
            float clamped = Math.Clamp((float)axisValue / short.MaxValue, -1f, 1f);
            JoystickAxisChanged?.Invoke(axisSource, clamped);
        }

        private void enqueueJoystickButtonInput(JoystickButton button, bool isPressed)
        {
            if (isPressed)
                JoystickButtonDown?.Invoke(button);
            else
                JoystickButtonUp?.Invoke(button);
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
            SDL.SDL_GetGlobalMouseState(out int x, out int y);
            if (previousPolledPoint.X == x && previousPolledPoint.Y == y)
                return;

            previousPolledPoint = new Point(x, y);

            var pos = WindowMode.Value == Configuration.WindowMode.Windowed ? Position : windowDisplayBounds.Location;
            int rx = x - pos.X;
            int ry = y - pos.Y;

            MouseMove?.Invoke(new Vector2(rx * Scale, ry * Scale));
        }

        public virtual void StartTextInput(bool allowIme) => ScheduleCommand(SDL.SDL_StartTextInput);

        public void StopTextInput() => ScheduleCommand(SDL.SDL_StopTextInput);

        /// <summary>
        /// Resets internal state of the platform-native IME.
        /// This will clear its composition text and prepare it for new input.
        /// </summary>
        public virtual void ResetIme() => ScheduleCommand(() =>
        {
            SDL.SDL_StopTextInput();
            SDL.SDL_StartTextInput();
        });

        public void SetTextInputRect(RectangleF rect) => ScheduleCommand(() =>
        {
            var sdlRect = ((RectangleI)(rect / Scale)).ToSDLRect();
            SDL.SDL_SetTextInputRect(ref sdlRect);
        });

        #region SDL Event Handling

        /// <summary>
        /// Adds an <see cref="Action"/> to the <see cref="Scheduler"/> expected to handle event callbacks.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to execute.</param>
        protected void ScheduleEvent(Action action) => EventScheduler.Add(action, false);

        protected void ScheduleCommand(Action action) => commandScheduler.Add(action, false);

        private const int events_per_peep = 64;
        private readonly SDL.SDL_Event[] events = new SDL.SDL_Event[events_per_peep];

        /// <summary>
        /// Poll for all pending events.
        /// </summary>
        private void pollSDLEvents()
        {
            SDL.SDL_PumpEvents();

            int eventsRead;

            do
            {
                eventsRead = SDL.SDL_PeepEvents(events, events_per_peep, SDL.SDL_eventaction.SDL_GETEVENT, SDL.SDL_EventType.SDL_FIRSTEVENT, SDL.SDL_EventType.SDL_LASTEVENT);
                for (int i = 0; i < eventsRead; i++)
                    handleSDLEvent(events[i]);
            } while (eventsRead == events_per_peep);
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
                    HandleTextEditingEvent(e.edit);
                    break;

                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    HandleTextInputEvent(e.text);
                    break;

                case SDL.SDL_EventType.SDL_KEYMAPCHANGED:
                    handleKeymapChangedEvent();
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

        private void handleQuitEvent(SDL.SDL_QuitEvent evtQuit) => ExitRequested?.Invoke();

        private void handleDropEvent(SDL.SDL_DropEvent evtDrop)
        {
            switch (evtDrop.type)
            {
                case SDL.SDL_EventType.SDL_DROPFILE:
                    string str = SDL.UTF8_ToManaged(evtDrop.file, true);
                    if (str != null)
                        DragDrop?.Invoke(str);

                    break;
            }
        }

        private readonly long?[] activeTouches = new long?[TouchState.MAX_TOUCH_COUNT];

        private TouchSource? getTouchSource(long fingerId)
        {
            for (int i = 0; i < activeTouches.Length; i++)
            {
                if (fingerId == activeTouches[i])
                    return (TouchSource)i;
            }

            return null;
        }

        private TouchSource? assignNextAvailableTouchSource(long fingerId)
        {
            for (int i = 0; i < activeTouches.Length; i++)
            {
                if (activeTouches[i] != null) continue;

                activeTouches[i] = fingerId;
                return (TouchSource)i;
            }

            // we only handle up to TouchState.MAX_TOUCH_COUNT. Ignore any further touches for now.
            return null;
        }

        private void handleTouchFingerEvent(SDL.SDL_TouchFingerEvent evtTfinger)
        {
            var eventType = (SDL.SDL_EventType)evtTfinger.type;

            var existingSource = getTouchSource(evtTfinger.fingerId);

            if (eventType == SDL.SDL_EventType.SDL_FINGERDOWN)
            {
                Debug.Assert(existingSource == null);
                existingSource = assignNextAvailableTouchSource(evtTfinger.fingerId);
            }

            if (existingSource == null)
                return;

            float x = evtTfinger.x * Size.Width;
            float y = evtTfinger.y * Size.Height;

            var touch = new Touch(existingSource.Value, new Vector2(x, y));

            switch (eventType)
            {
                case SDL.SDL_EventType.SDL_FINGERDOWN:
                case SDL.SDL_EventType.SDL_FINGERMOTION:
                    TouchDown?.Invoke(touch);
                    break;

                case SDL.SDL_EventType.SDL_FINGERUP:
                    TouchUp?.Invoke(touch);
                    activeTouches[(int)existingSource] = null;
                    break;
            }
        }

        private void handleControllerDeviceEvent(SDL.SDL_ControllerDeviceEvent evtCdevice)
        {
            switch (evtCdevice.type)
            {
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    addJoystick(evtCdevice.which);
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

        private void addJoystick(int which)
        {
            int instanceID = SDL.SDL_JoystickGetDeviceInstanceID(which);

            // if the joystick is already opened, ignore it
            if (controllers.ContainsKey(instanceID))
                return;

            var joystick = SDL.SDL_JoystickOpen(which);

            var controller = IntPtr.Zero;
            if (SDL.SDL_IsGameController(which) == SDL.SDL_bool.SDL_TRUE)
                controller = SDL.SDL_GameControllerOpen(which);

            controllers[instanceID] = new SDL2ControllerBindings(joystick, controller);
        }

        /// <summary>
        /// Populates <see cref="controllers"/> with joysticks that are already connected.
        /// </summary>
        private void populateJoysticks()
        {
            for (int i = 0; i < SDL.SDL_NumJoysticks(); i++)
            {
                addJoystick(i);
            }
        }

        private void handleJoyDeviceEvent(SDL.SDL_JoyDeviceEvent evtJdevice)
        {
            switch (evtJdevice.type)
            {
                case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                    addJoystick(evtJdevice.which);
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

        private uint lastPreciseScroll;

        private const uint precise_scroll_debounce = 100;

        private void handleMouseWheelEvent(SDL.SDL_MouseWheelEvent evtWheel)
        {
            bool isPrecise(float f) => f % 1 != 0;

            if (isPrecise(evtWheel.preciseX) || isPrecise(evtWheel.preciseY))
                lastPreciseScroll = evtWheel.timestamp;

            bool precise = evtWheel.timestamp < lastPreciseScroll + precise_scroll_debounce;

            // SDL reports horizontal scroll opposite of what framework expects (in non-"natural" mode, scrolling to the right gives positive deltas while we want negative).
            TriggerMouseWheel(new Vector2(-evtWheel.preciseX, evtWheel.preciseY), precise);
        }

        private void handleMouseButtonEvent(SDL.SDL_MouseButtonEvent evtButton)
        {
            MouseButton button = mouseButtonFromEvent(evtButton.button);

            switch (evtButton.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    MouseDown?.Invoke(button);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    MouseUp?.Invoke(button);
                    break;
            }
        }

        private void handleMouseMotionEvent(SDL.SDL_MouseMotionEvent evtMotion)
        {
            if (SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_FALSE)
                MouseMove?.Invoke(new Vector2(evtMotion.x * Scale, evtMotion.y * Scale));
            else
                MouseMoveRelative?.Invoke(new Vector2(evtMotion.xrel * Scale, evtMotion.yrel * Scale));
        }

        protected virtual unsafe void HandleTextInputEvent(SDL.SDL_TextInputEvent evtText)
        {
            if (!SDL2Extensions.TryGetStringFromBytePointer(evtText.text, out string text))
                return;

            TriggerTextInput(text);
        }

        protected virtual unsafe void HandleTextEditingEvent(SDL.SDL_TextEditingEvent evtEdit)
        {
            if (!SDL2Extensions.TryGetStringFromBytePointer(evtEdit.text, out string text))
                return;

            TriggerTextEditing(text, evtEdit.start, evtEdit.length);
        }

        private void handleKeyboardEvent(SDL.SDL_KeyboardEvent evtKey)
        {
            var key = evtKey.keysym.ToKeyboardKey();

            if (key.Key == Key.Unknown)
            {
                Logger.Log($"Unknown SDL key: {evtKey.keysym.scancode}, {evtKey.keysym.sym}");
                return;
            }

            switch (evtKey.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    KeyDown?.Invoke(key);
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    KeyUp?.Invoke(key);
                    break;
            }
        }

        private void handleKeymapChangedEvent() => KeymapChanged?.Invoke();

        private void handleWindowEvent(SDL.SDL_WindowEvent evtWindow)
        {
            updateWindowSpecifics();

            switch (evtWindow.windowEvent)
            {
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
                    // explicitly requery as there are occasions where what SDL has provided us with is not up-to-date.
                    SDL.SDL_GetWindowPosition(SDLWindowHandle, out int x, out int y);
                    var newPosition = new Point(x, y);

                    if (!newPosition.Equals(Position))
                    {
                        position = newPosition;
                        Moved?.Invoke(newPosition);

                        if (WindowMode.Value == Configuration.WindowMode.Windowed)
                            storeWindowPositionToConfig();
                    }

                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                    updateWindowSize();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                    cursorInWindow.Value = true;
                    MouseEntered?.Invoke();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                    cursorInWindow.Value = false;
                    MouseLeft?.Invoke();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                    Focused = true;
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                    Focused = false;
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
                WindowStateChanged?.Invoke(windowState);
                updateMaximisedState();
            }

            int newDisplayIndex = SDL.SDL_GetWindowDisplayIndex(SDLWindowHandle);

            if (displayIndex != newDisplayIndex)
            {
                displayIndex = newDisplayIndex;
                currentDisplay = Displays.ElementAtOrDefault(displayIndex) ?? PrimaryDisplay;
                CurrentDisplayBindable.Value = currentDisplay;
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
                    var closestMode = getClosestDisplayMode(sizeFullscreen.Value, currentDisplayMode.Value.RefreshRate, currentDisplay.Index);

                    Size = new Size(closestMode.w, closestMode.h);

                    SDL.SDL_SetWindowDisplayMode(SDLWindowHandle, ref closestMode);
                    SDL.SDL_SetWindowFullscreen(SDLWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
                    break;

                case WindowState.FullscreenBorderless:
                    Size = SetBorderless();
                    break;

                case WindowState.Maximised:
                    SDL.SDL_RestoreWindow(SDLWindowHandle);
                    SDL.SDL_MaximizeWindow(SDLWindowHandle);

                    SDL.SDL_GL_GetDrawableSize(SDLWindowHandle, out int w, out int h);
                    Size = new Size(w, h);
                    break;

                case WindowState.Minimised:
                    SDL.SDL_MinimizeWindow(SDLWindowHandle);
                    break;
            }

            updateMaximisedState();

            switch (windowState)
            {
                case WindowState.Fullscreen:
                    if (!updateDisplayMode(true))
                        updateDisplayMode(false);
                    break;

                default:
                    if (!updateDisplayMode(false))
                        updateDisplayMode(true);
                    break;
            }

            bool updateDisplayMode(bool queryFullscreenMode)
            {
                // TODO: displayIndex should be valid here at all times.
                // on startup, the displayIndex will be invalid (-1) due to it being set later in the startup sequence.
                // related to order of operations in `updateWindowSpecifics()`.
                int localIndex = SDL.SDL_GetWindowDisplayIndex(SDLWindowHandle);

                if (localIndex != displayIndex)
                    Logger.Log($"Stored display index ({displayIndex}) doesn't match current index ({localIndex})");

                if (queryFullscreenMode)
                {
                    if (SDL.SDL_GetWindowDisplayMode(SDLWindowHandle, out var mode) >= 0)
                    {
                        currentDisplayMode.Value = mode.ToDisplayMode(localIndex);
                        Logger.Log($"Updated display mode to fullscreen resolution: {mode.w}x{mode.h}@{mode.refresh_rate}, {currentDisplayMode.Value.Format}");
                        return true;
                    }

                    Logger.Log($"Failed to get fullscreen display mode. Display index: {localIndex}. SDL error: {SDL.SDL_GetError()}", level: LogLevel.Error);
                    return false;
                }
                else
                {
                    if (SDL.SDL_GetCurrentDisplayMode(localIndex, out var mode) >= 0)
                    {
                        currentDisplayMode.Value = mode.ToDisplayMode(localIndex);
                        Logger.Log($"Updated display mode to desktop resolution: {mode.w}x{mode.h}@{mode.refresh_rate}, {currentDisplayMode.Value.Format}");
                        return true;
                    }

                    Logger.Log($"Failed to get desktop display mode. Display index: {localIndex}. SDL error: {SDL.SDL_GetError()}", level: LogLevel.Error);
                    return false;
                }
            }
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
            int windowX = (int)Math.Round((displayBounds.Width - windowSize.Width) * configPosition.X);
            int windowY = (int)Math.Round((displayBounds.Height - windowSize.Height) * configPosition.Y);

            Position = new Point(windowX + displayBounds.X, windowY + displayBounds.Y);
        }

        private void storeWindowPositionToConfig()
        {
            if (WindowState != WindowState.Normal)
                return;

            var displayBounds = CurrentDisplay.Bounds;

            int windowX = Position.X - displayBounds.X;
            int windowY = Position.Y - displayBounds.Y;

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
            if (WindowState != WindowState.Normal)
                return;

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
            CurrentDisplayBindable.Default = PrimaryDisplay;
            CurrentDisplayBindable.ValueChanged += evt =>
            {
                windowDisplayIndexBindable.Value = (DisplayIndex)evt.NewValue.Index;
            };

            config.BindWith(FrameworkSetting.LastDisplayDevice, windowDisplayIndexBindable);
            windowDisplayIndexBindable.BindValueChanged(evt =>
            {
                CurrentDisplay = Displays.ElementAtOrDefault((int)evt.NewValue) ?? PrimaryDisplay;
                pendingWindowState = windowState;
            }, true);

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
            byte[] bytes = iconGroup.LoadRawIcon(default_icon_size, default_icon_size);
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
                                             return mode.ToDisplayMode(displayIndex);
                                         })
                                         .ToArray();

            SDL.SDL_GetDisplayBounds(displayIndex, out var rect);
            return new Display(displayIndex, SDL.SDL_GetDisplayName(displayIndex), new Rectangle(rect.x, rect.y, rect.w, rect.h), displayModes);
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
        /// Invoked when the window close (X) button or another platform-native exit action has been pressed.
        /// </summary>
        public event Action ExitRequested;

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
        /// <remarks>
        /// Delta is positive when mouse wheel scrolled to the up or left, in non-"natural" scroll mode (ie. the classic way).
        /// </remarks>
        public event Action<Vector2, bool> MouseWheel;

        protected void TriggerMouseWheel(Vector2 delta, bool precise) => MouseWheel?.Invoke(delta, precise);

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
        public event Action<KeyboardKey> KeyDown;

        /// <summary>
        /// Invoked when the user releases a key.
        /// </summary>
        public event Action<KeyboardKey> KeyUp;

        /// <summary>
        /// Invoked when the user enters text.
        /// </summary>
        public event Action<string> TextInput;

        protected void TriggerTextInput(string text) => TextInput?.Invoke(text);

        /// <summary>
        /// Invoked when an IME text editing event occurs.
        /// </summary>
        public event TextEditingDelegate TextEditing;

        protected void TriggerTextEditing(string text, int start, int length) => TextEditing?.Invoke(text, start, length);

        /// <inheritdoc cref="IWindow.KeymapChanged"/>
        public event Action KeymapChanged;

        /// <summary>
        /// Invoked when a joystick axis changes.
        /// </summary>
        public event Action<JoystickAxisSource, float> JoystickAxisChanged;

        /// <summary>
        /// Invoked when the user presses a button on a joystick.
        /// </summary>
        public event Action<JoystickButton> JoystickButtonDown;

        /// <summary>
        /// Invoked when the user releases a button on a joystick.
        /// </summary>
        public event Action<JoystickButton> JoystickButtonUp;

        /// <summary>
        /// Invoked when a finger moves or touches a touchscreen.
        /// </summary>
        public event Action<Touch> TouchDown;

        /// <summary>
        /// Invoked when a finger leaves the touchscreen.
        /// </summary>
        public event Action<Touch> TouchUp;

        /// <summary>
        /// Invoked when the user drops a file into the window.
        /// </summary>
        public event Action<string> DragDrop;

        /// <summary>
        /// Invoked on every SDL event before it's posted to the event queue.
        /// </summary>
        protected event Action<SDL.SDL_Event> OnSDLEvent;

        #endregion

        public void Dispose()
        {
        }

        /// <summary>
        /// Fired when text is edited, usually via IME composition.
        /// </summary>
        /// <param name="text">The composition text.</param>
        /// <param name="start">The index of the selection start.</param>
        /// <param name="length">The length of the selection.</param>
        public delegate void TextEditingDelegate(string text, int start, int length);
    }
}
