// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Logging;
using osu.Framework.Platform.SDL;
using osu.Framework.Threading;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using Point = System.Drawing.Point;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Default implementation of a window, using SDL for windowing and graphics support.
    /// </summary>
    internal abstract unsafe partial class SDL3Window : IWindow
    {
        internal SDL_Window* SDLWindowHandle { get; private set; } = null;

        private readonly SDL3GraphicsSurface graphicsSurface;
        IGraphicsSurface IWindow.GraphicsSurface => graphicsSurface;

        /// <summary>
        /// Returns true if window has been created.
        /// Returns false if the window has not yet been created, or has been closed.
        /// </summary>
        public bool Exists { get; private set; }

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
                ScheduleCommand(() => SDL3.SDL_SetWindowTitle(SDLWindowHandle, title));
            }
        }

        /// <summary>
        /// Whether the current display server is Wayland.
        /// </summary>
        internal bool IsWayland => SDL3.SDL_GetCurrentVideoDriver() == "wayland";

        /// <summary>
        /// Gets the native window handle as provided by the operating system.
        /// </summary>
        public IntPtr WindowHandle
        {
            get
            {
                if (SDLWindowHandle == null)
                    return IntPtr.Zero;

                var props = SDL3.SDL_GetWindowProperties(SDLWindowHandle);

                switch (RuntimeInfo.OS)
                {
                    case RuntimeInfo.Platform.Windows:
                        return SDL3.SDL_GetProperty(props, SDL3.SDL_PROP_WINDOW_WIN32_HWND_POINTER, IntPtr.Zero);

                    case RuntimeInfo.Platform.Linux:
                        if (IsWayland)
                            return SDL3.SDL_GetProperty(props, SDL3.SDL_PROP_WINDOW_WAYLAND_SURFACE_POINTER, IntPtr.Zero);

                        if (SDL3.SDL_GetCurrentVideoDriver() == "x11")
                            return new IntPtr(SDL3.SDL_GetNumberProperty(props, SDL3.SDL_PROP_WINDOW_X11_WINDOW_NUMBER, 0));

                        return IntPtr.Zero;

                    case RuntimeInfo.Platform.macOS:
                        return SDL3.SDL_GetProperty(props, SDL3.SDL_PROP_WINDOW_COCOA_WINDOW_POINTER, IntPtr.Zero);

                    case RuntimeInfo.Platform.iOS:
                        return SDL3.SDL_GetProperty(props, SDL3.SDL_PROP_WINDOW_UIKIT_WINDOW_POINTER, IntPtr.Zero);

                    case RuntimeInfo.Platform.Android:
                        return SDL3.SDL_GetProperty(props, SDL3.SDL_PROP_WINDOW_ANDROID_WINDOW_POINTER, IntPtr.Zero);

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public IntPtr DisplayHandle
        {
            get
            {
                if (SDLWindowHandle == null)
                    return IntPtr.Zero;

                var props = SDL3.SDL_GetWindowProperties(SDLWindowHandle);

                if (IsWayland)
                    return SDL3.SDL_GetProperty(props, SDL3.SDL_PROP_WINDOW_WAYLAND_DISPLAY_POINTER, IntPtr.Zero);

                if (SDL3.SDL_GetCurrentVideoDriver() == "x11")
                    return SDL3.SDL_GetProperty(props, SDL3.SDL_PROP_WINDOW_X11_DISPLAY_POINTER, IntPtr.Zero);

                return IntPtr.Zero;
            }
        }

        public bool CapsLockPressed => SDL3.SDL_GetModState().HasFlagFast(SDL_Keymod.SDL_KMOD_CAPS);

        /// <summary>
        /// Represents a handle to this <see cref="SDL3Window"/> instance, used for unmanaged callbacks.
        /// </summary>
        protected ObjectHandle<SDL3Window> ObjectHandle { get; private set; }

        protected SDL3Window(GraphicsSurfaceType surfaceType)
        {
            ObjectHandle = new ObjectHandle<SDL3Window>(this, GCHandleType.Normal);

            if (SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD) < 0)
            {
                throw new InvalidOperationException($"Failed to initialise SDL: {SDL3.SDL_GetError()}");
            }

            SDL3.SDL_LogSetPriority(SDL_LogCategory.SDL_LOG_CATEGORY_ERROR, SDL_LogPriority.SDL_LOG_PRIORITY_DEBUG);
            SDL3.SDL_SetLogOutputFunction(&logOutput, IntPtr.Zero);

            graphicsSurface = new SDL3GraphicsSurface(this, surfaceType);

            CursorStateBindable.ValueChanged += evt =>
            {
                updateCursorVisibility(!evt.NewValue.HasFlagFast(CursorState.Hidden));
                updateCursorConfinement();
            };

            populateJoysticks();
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void logOutput(IntPtr _, SDL_LogCategory category, SDL_LogPriority priority, byte* messagePtr)
        {
            string? message = SDL3.PtrToStringUTF8(messagePtr);
            Logger.Log($@"SDL {category.ReadableName()} log [{priority.ReadableName()}]: {message}");
        }

        public void SetupWindow(FrameworkConfigManager config)
        {
            setupWindowing(config);
            setupInput(config);
        }

        public virtual void Create()
        {
            SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                                    SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY |
                                    SDL_WindowFlags.SDL_WINDOW_HIDDEN; // shown after first swap to avoid white flash on startup (windows)

            flags |= WindowState.ToFlags();
            flags |= graphicsSurface.Type.ToFlags();

            SDL3.SDL_SetHint(SDL3.SDL_HINT_WINDOWS_CLOSE_ON_ALT_F4, "0"u8);
            SDL3.SDL_SetHint(SDL3.SDL_HINT_IME_SHOW_UI, "1"u8);
            SDL3.SDL_SetHint(SDL3.SDL_HINT_MOUSE_RELATIVE_MODE_CENTER, "0"u8);
            SDL3.SDL_SetHint(SDL3.SDL_HINT_TOUCH_MOUSE_EVENTS, "0"u8); // disable touch events generating synthetic mouse events on desktop platforms
            SDL3.SDL_SetHint(SDL3.SDL_HINT_MOUSE_TOUCH_EVENTS, "0"u8); // disable mouse events generating synthetic touch events on mobile platforms

            // we want text input to only be active when SDL3DesktopWindowTextInput is active.
            // SDL activates it by default on some platforms: https://github.com/libsdl-org/SDL/blob/release-2.0.16/src/video/SDL_video.c#L573-L582
            // so we deactivate it on startup.
            SDL3.SDL_StopTextInput();

            SDLWindowHandle = SDL3.SDL_CreateWindow(title, Size.Width, Size.Height, flags);

            if (SDLWindowHandle == null)
                throw new InvalidOperationException($"Failed to create SDL window. SDL Error: {SDL3.SDL_GetError()}");

            graphicsSurface.Initialise();

            initialiseWindowingAfterCreation();
            Exists = true;
        }

        /// <summary>
        /// Starts the window's run loop.
        /// </summary>
        public virtual void Run()
        {
            SDL3.SDL_SetEventFilter(&eventFilter, ObjectHandle.Handle);
            SDL3.SDL_AddEventWatch(&eventWatch, ObjectHandle.Handle);

            RunMainLoop();
        }

        /// <summary>
        /// Runs the main window loop.
        /// </summary>
        /// <remarks>
        /// By default this will block and indefinitely call <see cref="RunFrame"/> as long as the window <see cref="Exists"/>.
        /// Once the main loop finished running, cleanup logic will run.
        ///
        /// This may be overridden for special use cases, like mobile platforms which delegate execution of frames to the OS
        /// and don't require any kind of exit logic to exist.
        /// </remarks>
        protected virtual void RunMainLoop()
        {
            while (Exists)
                RunFrame();

            Exited?.Invoke();
            Close();
            SDL3.SDL_Quit();
        }

        /// <summary>
        /// Run a single frame.
        /// </summary>
        protected void RunFrame()
        {
            commandScheduler.Update();

            if (!Exists)
                return;

            if (pendingWindowState != null)
                updateAndFetchWindowSpecifics();

            pollSDLEvents();

            if (!cursorInWindow.Value)
                pollMouse();

            EventScheduler.Update();
            Update?.Invoke();
        }

        /// <summary>
        /// Handles <see cref="SDL_Event"/>s fired from the SDL event filter.
        /// </summary>
        /// <remarks>
        /// As per SDL's recommendation, application events should always be handled via the event filter.
        /// See: https://wiki.libsdl.org/SDL3/SDL_EventType#android_ios_and_winrt_events
        /// </remarks>
        protected virtual void HandleEventFromFilter(SDL_Event evt)
        {
            switch (evt.Type)
            {
                case SDL_EventType.SDL_EVENT_TERMINATING:
                    handleQuitEvent(evt.quit);
                    break;

                case SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND:
                    Suspended?.Invoke();
                    break;

                case SDL_EventType.SDL_EVENT_WILL_ENTER_FOREGROUND:
                    Resumed?.Invoke();
                    break;

                case SDL_EventType.SDL_EVENT_LOW_MEMORY:
                    LowOnMemory?.Invoke();
                    break;
            }
        }

        protected void HandleEventFromWatch(SDL_Event evt)
        {
            switch (evt.Type)
            {
                case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                    // polling via SDL_PollEvent blocks on resizes (https://stackoverflow.com/a/50858339)
                    if (!updatingWindowStateAndSize)
                        fetchWindowSize();

                    break;
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static int eventFilter(IntPtr userdata, SDL_Event* eventPtr)
        {
            var handle = new ObjectHandle<SDL3Window>(userdata);
            if (handle.GetTarget(out SDL3Window window))
                window.HandleEventFromFilter(*eventPtr);

            return 1;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static int eventWatch(IntPtr userdata, SDL_Event* eventPtr)
        {
            var handle = new ObjectHandle<SDL3Window>(userdata);
            if (handle.GetTarget(out SDL3Window window))
                window.HandleEventFromWatch(*eventPtr);

            return 1;
        }

        private bool firstDraw = true;

        public void OnDraw()
        {
            if (!firstDraw)
                return;

            Visible = true;
            firstDraw = false;
        }

        /// <summary>
        /// Forcefully closes the window.
        /// </summary>
        public void Close()
        {
            if (Exists)
            {
                // Close will be called as part of finishing the Run loop.
                ScheduleCommand(() => Exists = false);
            }
            else
            {
                if (SDLWindowHandle != null)
                {
                    SDL3.SDL_DestroyWindow(SDLWindowHandle);
                    SDLWindowHandle = null;
                }
            }
        }

        public void Raise() => ScheduleCommand(() =>
        {
            var flags = SDL3.SDL_GetWindowFlags(SDLWindowHandle);

            if (flags.HasFlagFast(SDL_WindowFlags.SDL_WINDOW_MINIMIZED))
                SDL3.SDL_RestoreWindow(SDLWindowHandle);

            SDL3.SDL_RaiseWindow(SDLWindowHandle);
        });

        public void Hide() => ScheduleCommand(() =>
        {
            SDL3.SDL_HideWindow(SDLWindowHandle);
        });

        public void Show() => ScheduleCommand(() =>
        {
            SDL3.SDL_ShowWindow(SDLWindowHandle);
        });

        public void Flash(bool flashUntilFocused = false) => ScheduleCommand(() =>
        {
            if (isActive.Value)
                return;

            if (!RuntimeInfo.IsDesktop)
                return;

            SDL3.SDL_FlashWindow(SDLWindowHandle, flashUntilFocused
                ? SDL_FlashOperation.SDL_FLASH_UNTIL_FOCUSED
                : SDL_FlashOperation.SDL_FLASH_BRIEFLY);
        });

        public void CancelFlash() => ScheduleCommand(() =>
        {
            if (!RuntimeInfo.IsDesktop)
                return;

            SDL3.SDL_FlashWindow(SDLWindowHandle, SDL_FlashOperation.SDL_FLASH_CANCEL);
        });

        /// <summary>
        /// Attempts to set the window's icon to the specified image.
        /// </summary>
        /// <param name="image">An <see cref="Image{Rgba32}"/> to set as the window icon.</param>
        private void setSDLIcon(Image<Rgba32> image)
        {
            var pixelMemory = image.CreateReadOnlyPixelMemory();
            var imageSize = image.Size;

            ScheduleCommand(() =>
            {
                var pixelSpan = pixelMemory.Span;

                SDL_Surface* surface;

                fixed (Rgba32* ptr = pixelSpan)
                {
                    var pixelFormat = SDL3.SDL_GetPixelFormatEnumForMasks(32, 0xff, 0xff00, 0xff0000, 0xff000000);
                    surface = SDL3.SDL_CreateSurfaceFrom(new IntPtr(ptr), imageSize.Width, imageSize.Height, imageSize.Width * 4, pixelFormat);
                }

                SDL3.SDL_SetWindowIcon(SDLWindowHandle, surface);
                SDL3.SDL_DestroySurface(surface);
            });
        }

        #region SDL Event Handling

        /// <summary>
        /// Adds an <see cref="Action"/> to the <see cref="Scheduler"/> expected to handle event callbacks.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to execute.</param>
        protected void ScheduleEvent(Action action) => EventScheduler.Add(action, false);

        protected void ScheduleCommand(Action action) => commandScheduler.Add(action, false);

        private const int events_per_peep = 64;
        private readonly SDL_Event[] events = new SDL_Event[events_per_peep];

        /// <summary>
        /// Poll for all pending events.
        /// </summary>
        private void pollSDLEvents()
        {
            SDL3.SDL_PumpEvents();

            int eventsRead;

            do
            {
                eventsRead = SDL3.SDL_PeepEvents(events, SDL_eventaction.SDL_GETEVENT, SDL_EventType.SDL_EVENT_FIRST, SDL_EventType.SDL_EVENT_LAST);
                for (int i = 0; i < eventsRead; i++)
                    HandleEvent(events[i]);
            } while (eventsRead == events_per_peep);
        }

        /// <summary>
        /// Handles <see cref="SDL_Event"/>s polled on the main thread.
        /// </summary>
        protected virtual void HandleEvent(SDL_Event e)
        {
            if (e.Type >= SDL_EventType.SDL_EVENT_DISPLAY_FIRST && e.Type <= SDL_EventType.SDL_EVENT_DISPLAY_LAST)
            {
                handleDisplayEvent(e.display);
                return;
            }

            if (e.Type >= SDL_EventType.SDL_EVENT_WINDOW_FIRST && e.Type <= SDL_EventType.SDL_EVENT_WINDOW_LAST)
            {
                handleWindowEvent(e.window);
                return;
            }

            switch (e.Type)
            {
                case SDL_EventType.SDL_EVENT_QUIT:
                    handleQuitEvent(e.quit);
                    break;

                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                case SDL_EventType.SDL_EVENT_KEY_UP:
                    handleKeyboardEvent(e.key);
                    break;

                case SDL_EventType.SDL_EVENT_TEXT_EDITING:
                    HandleTextEditingEvent(e.edit);
                    break;

                case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                    HandleTextInputEvent(e.text);
                    break;

                case SDL_EventType.SDL_EVENT_KEYMAP_CHANGED:
                    handleKeymapChangedEvent();
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    handleMouseMotionEvent(e.motion);
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    handleMouseButtonEvent(e.button);
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    handleMouseWheelEvent(e.wheel);
                    break;

                case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
                    handleJoyAxisEvent(e.jaxis);
                    break;

                case SDL_EventType.SDL_EVENT_JOYSTICK_BALL_MOTION:
                    handleJoyBallEvent(e.jball);
                    break;

                case SDL_EventType.SDL_EVENT_JOYSTICK_HAT_MOTION:
                    handleJoyHatEvent(e.jhat);
                    break;

                case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
                    handleJoyButtonEvent(e.jbutton);
                    break;

                case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
                case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
                    handleJoyDeviceEvent(e.jdevice);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                    handleControllerAxisEvent(e.gaxis);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                    handleControllerButtonEvent(e.gbutton);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                case SDL_EventType.SDL_EVENT_GAMEPAD_REMAPPED:
                    handleControllerDeviceEvent(e.gdevice);
                    break;

                case SDL_EventType.SDL_EVENT_FINGER_DOWN:
                case SDL_EventType.SDL_EVENT_FINGER_UP:
                case SDL_EventType.SDL_EVENT_FINGER_MOTION:
                    HandleTouchFingerEvent(e.tfinger);
                    break;

                case SDL_EventType.SDL_EVENT_DROP_FILE:
                case SDL_EventType.SDL_EVENT_DROP_TEXT:
                case SDL_EventType.SDL_EVENT_DROP_BEGIN:
                case SDL_EventType.SDL_EVENT_DROP_COMPLETE:
                    handleDropEvent(e.drop);
                    break;
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private void handleQuitEvent(SDL_QuitEvent evtQuit) => ExitRequested?.Invoke();

        #endregion

        public void SetIconFromStream(Stream imageStream)
        {
            using (var ms = new MemoryStream())
            {
                imageStream.CopyTo(ms);
                ms.Position = 0;

                try
                {
                    SetIconFromImage(Image.Load<Rgba32>(ms.GetBuffer()));
                }
                catch
                {
                    if (IconGroup.TryParse(ms.GetBuffer(), out var iconGroup))
                        SetIconFromGroup(iconGroup);
                }
            }
        }

        internal virtual void SetIconFromGroup(IconGroup iconGroup)
        {
            // LoadRawIcon returns raw PNG data if available, which avoids any Windows-specific pinvokes
            byte[]? bytes = iconGroup.LoadRawIcon(default_icon_size, default_icon_size);
            if (bytes == null)
                return;

            SetIconFromImage(Image.Load<Rgba32>(bytes));
        }

        internal virtual void SetIconFromImage(Image<Rgba32> iconImage) => setSDLIcon(iconImage);

        #region Events

        /// <summary>
        /// Invoked once every window event loop.
        /// </summary>
        public event Action? Update;

        /// <summary>
        /// Invoked when the application associated with this <see cref="IWindow"/> has been suspended.
        /// </summary>
        public event Action? Suspended;

        /// <summary>
        /// Invoked when the application associated with this <see cref="IWindow"/> has been resumed from suspension.
        /// </summary>
        public event Action? Resumed;

        /// <summary>
        /// Invoked when the operating system is low on memory, in order for the application to free some.
        /// </summary>
        public event Action? LowOnMemory;

        /// <summary>
        /// Invoked when the window close (X) button or another platform-native exit action has been pressed.
        /// </summary>
        public event Action? ExitRequested;

        /// <summary>
        /// Invoked when the window is about to close.
        /// </summary>
        public event Action? Exited;

        /// <summary>
        /// Invoked when the user drops a file into the window.
        /// </summary>
        public event Action<string>? DragDrop;

        #endregion

        public void Dispose()
        {
            Close();
            SDL3.SDL_Quit();

            ObjectHandle.Dispose();
        }
    }
}
