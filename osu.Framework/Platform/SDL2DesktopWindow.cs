// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Logging;
using osu.Framework.Platform.SDL2;
using osu.Framework.Threading;
using SDL2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using Point = System.Drawing.Point;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Default implementation of a desktop window, using SDL for windowing and graphics support.
    /// </summary>
    public partial class SDL2DesktopWindow : IWindow
    {
        internal IntPtr SDLWindowHandle { get; private set; } = IntPtr.Zero;

        private readonly SDL2GraphicsSurface graphicsSurface;
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
                ScheduleCommand(() => SDL.SDL_SetWindowTitle(SDLWindowHandle, title));
            }
        }

        /// <summary>
        /// Whether the current display server is Wayland.
        /// </summary>
        internal bool IsWayland
        {
            get
            {
                if (SDLWindowHandle == IntPtr.Zero)
                    return false;

                return getWindowWMInfo().subsystem == SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WAYLAND;
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

        public IntPtr DisplayHandle
        {
            get
            {
                if (SDLWindowHandle == IntPtr.Zero)
                    return IntPtr.Zero;

                var wmInfo = getWindowWMInfo();

                switch (wmInfo.subsystem)
                {
                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_X11:
                        return wmInfo.info.x11.display;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WAYLAND:
                        return wmInfo.info.wl.display;

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
            SDL.SDL_GetVersion(out wmInfo.version);
            SDL.SDL_GetWindowWMInfo(SDLWindowHandle, ref wmInfo);
            return wmInfo;
        }

        public bool CapsLockPressed => SDL.SDL_GetModState().HasFlagFast(SDL.SDL_Keymod.KMOD_CAPS);

        // references must be kept to avoid GC, see https://stackoverflow.com/a/6193914

        [UsedImplicitly]
        private SDL.SDL_LogOutputFunction logOutputDelegate;

        [UsedImplicitly]
        private SDL.SDL_EventFilter? eventFilterDelegate;

        public SDL2DesktopWindow(GraphicsSurfaceType surfaceType)
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER) < 0)
            {
                throw new InvalidOperationException($"Failed to initialise SDL: {SDL.SDL_GetError()}");
            }

            SDL.SDL_LogSetPriority((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_ERROR, SDL.SDL_LogPriority.SDL_LOG_PRIORITY_DEBUG);
            SDL.SDL_LogSetOutputFunction(logOutputDelegate = (_, categoryInt, priority, messagePtr) =>
            {
                var category = (SDL.SDL_LogCategory)categoryInt;
                string? message = Marshal.PtrToStringUTF8(messagePtr);

                Logger.Log($@"SDL {category.ReadableName()} log [{priority.ReadableName()}]: {message}");
            }, IntPtr.Zero);

            graphicsSurface = new SDL2GraphicsSurface(this, surfaceType);
            SupportedWindowModes = new BindableList<WindowMode>(DefaultSupportedWindowModes);

            CursorStateBindable.ValueChanged += evt =>
            {
                updateCursorVisibility(!evt.NewValue.HasFlagFast(CursorState.Hidden));
                updateCursorConfinement();
            };

            populateJoysticks();
        }

        public void SetupWindow(FrameworkConfigManager config)
        {
            setupWindowing(config);
            setupInput(config);
        }

        public virtual void Create()
        {
            SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN; // shown after first swap to avoid white flash on startup (windows)

            flags |= WindowState.ToFlags();
            flags |= graphicsSurface.Type.ToFlags();

            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_NO_CLOSE_ON_ALT_F4, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_MINIMIZE_ON_FOCUS_LOSS, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_IME_SHOW_UI, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_RELATIVE_MODE_CENTER, "0");
            SDL.SDL_SetHint(SDL.SDL_HINT_TOUCH_MOUSE_EVENTS, "0");

            // we want text input to only be active when SDL2DesktopWindowTextInput is active.
            // SDL activates it by default on some platforms: https://github.com/libsdl-org/SDL/blob/release-2.0.16/src/video/SDL_video.c#L573-L582
            // so we deactivate it on startup.
            SDL.SDL_StopTextInput();

            SDLWindowHandle = SDL.SDL_CreateWindow(title, Position.X, Position.Y, Size.Width, Size.Height, flags);

            if (SDLWindowHandle == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to create SDL window. SDL Error: {SDL.SDL_GetError()}");

            Exists = true;

            graphicsSurface.Initialise();

            initialiseWindowingAfterCreation();
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
                    fetchWindowSize();
                }
            };

            while (Exists)
            {
                commandScheduler.Update();

                if (!Exists)
                    break;

                if (pendingWindowState != null)
                    updateAndFetchWindowSpecifics();

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
        public void Close() => ScheduleCommand(() => Exists = false);

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

                case SDL.SDL_EventType.SDL_DISPLAYEVENT:
                    handleDisplayEvent(e.display);
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

        // ReSharper disable once UnusedParameter.Local
        private void handleQuitEvent(SDL.SDL_QuitEvent evtQuit) => ExitRequested?.Invoke();

        #endregion

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

        /// <summary>
        /// Invoked on every SDL event before it's posted to the event queue.
        /// </summary>
        protected event Action<SDL.SDL_Event>? OnSDLEvent;

        #endregion

        public void Dispose()
        {
        }
    }
}
