// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Logging;
using osu.Framework.Platform.SDL2;
using osuTK;
using SDL2;

namespace osu.Framework.Platform
{
    public partial class SDL2DesktopWindow
    {
        private void setupWindowing(FrameworkConfigManager config)
        {
            fetchDisplays();

            DisplaysChanged += _ => CurrentDisplayBindable.Default = PrimaryDisplay;
            CurrentDisplayBindable.Default = PrimaryDisplay;
            CurrentDisplayBindable.ValueChanged += evt =>
            {
                windowDisplayIndexBindable.Value = (DisplayIndex)evt.NewValue.Index;
            };

            config.BindWith(FrameworkSetting.LastDisplayDevice, windowDisplayIndexBindable);
            windowDisplayIndexBindable.BindValueChanged(evt =>
            {
                currentDisplay = Displays.ElementAtOrDefault((int)evt.NewValue) ?? PrimaryDisplay;
                invalidateWindowSpecifics();
            }, true);

            sizeFullscreen.ValueChanged += _ =>
            {
                if (storingSizeToConfig) return;
                if (windowState != WindowState.Fullscreen) return;

                invalidateWindowSpecifics();
            };

            sizeWindowed.ValueChanged += _ =>
            {
                if (storingSizeToConfig) return;
                if (windowState != WindowState.Normal) return;

                invalidateWindowSpecifics();
            };

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

            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);
            config.BindWith(FrameworkSetting.WindowedSize, sizeWindowed);

            config.BindWith(FrameworkSetting.WindowedPositionX, windowPositionX);
            config.BindWith(FrameworkSetting.WindowedPositionY, windowPositionY);

            config.BindWith(FrameworkSetting.WindowMode, WindowMode);

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
            });
        }

        private void initialiseWindowingAfterCreation()
        {
            updateAndFetchWindowSpecifics();
            fetchWindowSize();

            sizeWindowed.TriggerChange();

            WindowMode.TriggerChange();
        }

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

        public Bindable<Display> CurrentDisplayBindable { get; } = new Bindable<Display>();

        /// <summary>
        /// Bound to <see cref="FrameworkSetting.WindowMode"/>.
        /// </summary>
        public Bindable<WindowMode> WindowMode { get; } = new Bindable<WindowMode>();

        private readonly BindableBool isActive = new BindableBool();

        public IBindable<bool> IsActive => isActive;

        private readonly BindableBool cursorInWindow = new BindableBool();

        public IBindable<bool> CursorInWindow => cursorInWindow;

        public IBindableList<WindowMode> SupportedWindowModes { get; }

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
        public Size ClientSize => new Size((int)(Size.Width * Scale), (int)(Size.Height * Scale));

        public float Scale = 1;

        #region Displays (mostly self-contained)

        /// <summary>
        /// Queries the physical displays and their supported resolutions.
        /// </summary>
        public IEnumerable<Display> Displays { get; private set; } = null!;

        public event Action<IEnumerable<Display>>? DisplaysChanged;

        // ReSharper disable once UnusedParameter.Local
        private void handleDisplayEvent(SDL.SDL_DisplayEvent evtDisplay) => fetchDisplays();

        /// <summary>
        /// Updates <see cref="Displays"/> with the latest display information reported by SDL.
        /// </summary>
        /// <remarks>
        /// Has no effect on values of
        /// <see cref="currentDisplay"/> /
        /// <see cref="CurrentDisplayBindable"/>.
        /// </remarks>
        private void fetchDisplays()
        {
            Displays = getSDLDisplays();
            DisplaysChanged?.Invoke(Displays);
        }

        /// <summary>
        /// Asserts that the current <see cref="Displays"/> match the actual displays as reported by SDL.
        /// </summary>
        /// <remarks>
        /// This assert is not fatal, as the <see cref="Displays"/> will get updated sooner or later
        /// in <see cref="handleDisplayEvent"/> or <see cref="handleWindowEvent"/>.
        /// </remarks>
        [Conditional("DEBUG")]
        private void assertDisplaysMatchSDL()
        {
            var actualDisplays = getSDLDisplays();
            Debug.Assert(actualDisplays.SequenceEqual(Displays), $"Stored {nameof(Displays)} don't match actual displays",
                $"Stored displays:\n  {string.Join("\n  ", Displays)}\n\nActual displays:\n  {string.Join("\n  ", actualDisplays)}");
        }

        private static IEnumerable<Display> getSDLDisplays()
        {
            return get().ToArray();

            IEnumerable<Display> get()
            {
                for (int i = 0; i < SDL.SDL_GetNumVideoDisplays(); i++)
                {
                    if (tryGetDisplayFromSDL(i, out Display? display))
                        yield return display;
                    else
                        Debug.Fail($"Failed to retrieve display at index ({i})");
                }
            }
        }

        private static bool tryGetDisplayFromSDL(int displayIndex, [NotNullWhen(true)] out Display? display)
        {
            if (displayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(displayIndex), displayIndex, $"{nameof(displayIndex)} must be non-negative.");

            if (SDL.SDL_GetDisplayBounds(displayIndex, out var rect) < 0)
            {
                display = null;
                return false;
            }

            int numModes = SDL.SDL_GetNumDisplayModes(displayIndex);

            if (numModes <= 0)
            {
                display = null;
                return false;
            }

            var displayModes = Enumerable.Range(0, numModes)
                                         .Select(modeIndex =>
                                         {
                                             SDL.SDL_GetDisplayMode(displayIndex, modeIndex, out var mode);
                                             return mode.ToDisplayMode(displayIndex);
                                         })
                                         .ToArray();

            display = new Display(displayIndex, SDL.SDL_GetDisplayName(displayIndex), new Rectangle(rect.x, rect.y, rect.w, rect.h), displayModes);
            return true;
        }

        #endregion

        /// <summary>
        /// Gets the <see cref="Display"/> that has been set as "primary" or "default" in the operating system.
        /// </summary>
        public virtual Display PrimaryDisplay => Displays.First();

        private Display currentDisplay = null!;
        private int displayIndex = -1;

        private readonly Bindable<DisplayMode> currentDisplayMode = new Bindable<DisplayMode>();

        /// <summary>
        /// The <see cref="DisplayMode"/> for the display that this window is currently on.
        /// </summary>
        public IBindable<DisplayMode> CurrentDisplayMode => currentDisplayMode;

        private Rectangle windowDisplayBounds
        {
            get
            {
                SDL.SDL_GetDisplayBounds(displayIndex, out var rect);
                return new Rectangle(rect.x, rect.y, rect.w, rect.h);
            }
        }

        /// <summary>
        /// Bound to <see cref="FrameworkSetting.SizeFullscreen"/>.
        /// </summary>
        private readonly BindableSize sizeFullscreen = new BindableSize();

        /// <summary>
        /// Bound to <see cref="FrameworkSetting.WindowedSize"/>.
        /// </summary>
        private readonly BindableSize sizeWindowed = new BindableSize();

        /// <summary>
        /// Bound to <see cref="FrameworkSetting.WindowedPositionX"/>.
        /// </summary>
        private readonly BindableDouble windowPositionX = new BindableDouble();

        /// <summary>
        /// Bound to <see cref="FrameworkSetting.WindowedPositionY"/>.
        /// </summary>
        private readonly BindableDouble windowPositionY = new BindableDouble();

        /// <summary>
        /// Bound to <see cref="FrameworkSetting.LastDisplayDevice"/>.
        /// </summary>
        private readonly Bindable<DisplayIndex> windowDisplayIndexBindable = new Bindable<DisplayIndex>();

        /// <summary>
        /// Updates <see cref="Size"/> and <see cref="Scale"/> according to SDL state.
        /// </summary>
        /// <returns>Whether the window size has been changed after updating.</returns>
        private void fetchWindowSize()
        {
            SDL.SDL_GetWindowSize(SDLWindowHandle, out int w, out int h);
            SDL.SDL_GL_GetDrawableSize(SDLWindowHandle, out int drawableW, out int _);

            // When minimised on windows, values may be zero.
            // If we receive zeroes for either of these, it seems safe to completely ignore them.
            if (w <= 0 || drawableW <= 0)
                return;

            Scale = (float)drawableW / w;
            Size = new Size(w, h);

            // This function may be invoked before the SDL internal states are all changed. (as documented here: https://wiki.libsdl.org/SDL_SetEventFilter)
            // Scheduling the store to config until after the event poll has run will ensure the window is in the correct state.
            EventScheduler.AddOnce(storeWindowSizeToConfig);
        }

        #region SDL Event Handling

        private void handleWindowEvent(SDL.SDL_WindowEvent evtWindow)
        {
            updateAndFetchWindowSpecifics();

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

                    // we may get a SDL_WINDOWEVENT_MOVED when the resolution of a display changes.
                    fetchDisplays();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                    fetchWindowSize();
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
                    // displays can change without a SDL_DISPLAYEVENT being sent, eg. changing resolution.
                    // force update displays when gaining keyboard focus to always have up-to-date information.
                    // eg. this covers scenarios when changing resolution outside of the game, and then tabbing in.
                    fetchDisplays();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                    Focused = false;
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                    break;
            }

            assertDisplaysMatchSDL();
        }

        /// <summary>
        /// Invalidates the the state of the window.
        /// This forces <see cref="updateAndFetchWindowSpecifics"/> to run before the next event loop.
        /// </summary>
        private void invalidateWindowSpecifics()
        {
            pendingWindowState = windowState;
        }

        /// <summary>
        /// Should be run on a regular basis to check for external window state changes.
        /// </summary>
        private void updateAndFetchWindowSpecifics()
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

                updateWindowStateAndSize(windowState, currentDisplay, currentDisplayMode.Value);
            }
            else
            {
                windowState = ((SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(SDLWindowHandle)).ToWindowState();
            }

            if (windowState != stateBefore)
            {
                WindowStateChanged?.Invoke(windowState);

                if (tryFetchMaximisedState(windowState, out bool maximized))
                    windowMaximised = maximized;
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
        private void updateWindowStateAndSize(WindowState state, Display display, DisplayMode displayMode)
        {
            // this reset is required even on changing from one fullscreen resolution to another.
            // if it is not included, the GL context will not get the correct size.
            // this is mentioned by multiple sources as an SDL issue, which seems to resolve by similar means (see https://discourse.libsdl.org/t/sdl-setwindowsize-does-not-work-in-fullscreen/20711/4).
            SDL.SDL_SetWindowBordered(SDLWindowHandle, SDL.SDL_bool.SDL_TRUE);
            SDL.SDL_SetWindowFullscreen(SDLWindowHandle, (uint)SDL.SDL_bool.SDL_FALSE);
            SDL.SDL_RestoreWindow(SDLWindowHandle);

            switch (state)
            {
                case WindowState.Normal:
                    Size = sizeWindowed.Value;

                    SDL.SDL_RestoreWindow(SDLWindowHandle);
                    SDL.SDL_SetWindowSize(SDLWindowHandle, Size.Width, Size.Height);
                    SDL.SDL_SetWindowResizable(SDLWindowHandle, Resizable ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);

                    readWindowPositionFromConfig(state, display);
                    break;

                case WindowState.Fullscreen:
                    var closestMode = getClosestDisplayMode(SDLWindowHandle, sizeFullscreen.Value, display, displayMode);

                    Size = new Size(closestMode.w, closestMode.h);

                    ensureWindowOnDisplay(display);

                    SDL.SDL_SetWindowDisplayMode(SDLWindowHandle, ref closestMode);
                    SDL.SDL_SetWindowFullscreen(SDLWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
                    break;

                case WindowState.FullscreenBorderless:
                    Size = SetBorderless(display);
                    break;

                case WindowState.Maximised:
                    SDL.SDL_RestoreWindow(SDLWindowHandle);

                    ensureWindowOnDisplay(display);

                    SDL.SDL_MaximizeWindow(SDLWindowHandle);
                    fetchWindowSize();
                    break;

                case WindowState.Minimised:
                    ensureWindowOnDisplay(display);
                    SDL.SDL_MinimizeWindow(SDLWindowHandle);
                    break;
            }

            if (tryFetchMaximisedState(state, out bool maximized))
                windowMaximised = maximized;

            if (tryFetchDisplayMode(SDLWindowHandle, state, display, out var newMode))
                currentDisplayMode.Value = newMode;
        }

        private static bool tryFetchDisplayMode(IntPtr windowHandle, WindowState windowState, Display display, out DisplayMode displayMode)
        {
            // TODO: displayIndex should be valid here at all times.
            // on startup, the displayIndex will be invalid (-1) due to it being set later in the startup sequence.
            // related to order of operations in `updateWindowSpecifics()`.
            int localIndex = SDL.SDL_GetWindowDisplayIndex(windowHandle);

            if (localIndex != display.Index)
                Logger.Log($"Stored display index ({display.Index}) doesn't match current index ({localIndex})");

            bool success;
            SDL.SDL_DisplayMode mode;

            if (windowState == WindowState.Fullscreen)
                success = SDL.SDL_GetWindowDisplayMode(windowHandle, out mode) >= 0;
            else
                success = SDL.SDL_GetCurrentDisplayMode(localIndex, out mode) >= 0;

            string type = windowState == WindowState.Fullscreen ? "fullscreen" : "desktop";

            if (success)
            {
                displayMode = mode.ToDisplayMode(localIndex);
                Logger.Log($"Updated display mode to {type} resolution: {mode.w}x{mode.h}@{mode.refresh_rate}, {displayMode.Format}");
                return true;
            }
            else
            {
                Logger.Log($"Failed to get {type} display mode. Display index: {localIndex}. SDL error: {SDL.SDL_GetError()}", level: LogLevel.Error);
                displayMode = default;
                return false;
            }
        }

        private static bool tryFetchMaximisedState(WindowState windowState, out bool maximized)
        {
            if (windowState is WindowState.Normal or WindowState.Maximised)
            {
                maximized = windowState == WindowState.Maximised;
                return true;
            }

            maximized = default;
            return false;
        }

        private void readWindowPositionFromConfig(WindowState state, Display display)
        {
            if (state != WindowState.Normal)
                return;

            var configPosition = new Vector2((float)windowPositionX.Value, (float)windowPositionY.Value);

            moveWindowTo(display, configPosition);
        }

        /// <summary>
        /// Ensures that the window is located on the provided <see cref="Display"/>.
        /// </summary>
        /// <param name="display">The <see cref="Display"/> to center the window on.</param>
        private void ensureWindowOnDisplay(Display display)
        {
            if (display.Index == SDL.SDL_GetWindowDisplayIndex(SDLWindowHandle))
                return;

            moveWindowTo(display, new Vector2(0.5f));
        }

        /// <summary>
        /// Moves the window to be centred around the normalised <paramref name="newPosition"/> on a <paramref name="display"/>.
        /// </summary>
        /// <param name="display">The <see cref="Display"/> to move the window to.</param>
        /// <param name="newPosition">Relative position on the display, normalised to <c>[-0.5, 1.5]</c>.</param>
        private void moveWindowTo(Display display, Vector2 newPosition)
        {
            Debug.Assert(newPosition == Vector2.Clamp(newPosition, new Vector2(-0.5f), new Vector2(1.5f)));

            var displayBounds = display.Bounds;
            var windowSize = sizeWindowed.Value;
            int windowX = (int)Math.Round((displayBounds.Width - windowSize.Width) * newPosition.X);
            int windowY = (int)Math.Round((displayBounds.Height - windowSize.Height) * newPosition.Y);

            Position = new Point(windowX + displayBounds.X, windowY + displayBounds.Y);
        }

        private void storeWindowPositionToConfig()
        {
            if (WindowState != WindowState.Normal)
                return;

            var displayBounds = currentDisplay.Bounds;

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
            sizeWindowed.Value = Size;
            storingSizeToConfig = false;
        }

        /// <summary>
        /// Prepare display of a borderless window.
        /// </summary>
        /// <param name="display">The display to make the window fullscreen borderless on.</param>
        /// <returns>
        /// The size of the borderless window's draw area.
        /// </returns>
        protected virtual Size SetBorderless(Display display)
        {
            ensureWindowOnDisplay(display);

            // this is a generally sane method of handling borderless, and works well on macOS and linux.
            SDL.SDL_SetWindowFullscreen(SDLWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);

            return display.Bounds.Size;
        }

        #endregion

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

        #region Helper functions

        private static SDL.SDL_DisplayMode getClosestDisplayMode(IntPtr windowHandle, Size size, Display display, DisplayMode requestedMode)
        {
            SDL.SDL_ClearError(); // clear any stale error.

            // default size means to use the display's native size.
            if (size.Width == 9999 && size.Height == 9999)
                size = display.Bounds.Size;

            var targetMode = new SDL.SDL_DisplayMode { w = size.Width, h = size.Height, refresh_rate = requestedMode.RefreshRate };

            if (SDL.SDL_GetClosestDisplayMode(display.Index, ref targetMode, out var mode) != IntPtr.Zero)
                return mode;
            else
                Logger.Log($"Unable to get preferred display mode (try #1/2). Target display: {display.Index}, mode: {targetMode.ReadableString()}. SDL error: {SDL2Extensions.GetAndClearError()}");

            // fallback to current display's native bounds
            targetMode.w = display.Bounds.Width;
            targetMode.h = display.Bounds.Height;
            targetMode.refresh_rate = 0;

            if (SDL.SDL_GetClosestDisplayMode(display.Index, ref targetMode, out mode) != IntPtr.Zero)
                return mode;
            else
                Logger.Log($"Unable to get preferred display mode (try #2/2). Target display: {display.Index}, mode: {targetMode.ReadableString()}. SDL error: {SDL2Extensions.GetAndClearError()}");

            // try the display's native display mode.
            if (SDL.SDL_GetDesktopDisplayMode(display.Index, out mode) == 0)
                return mode;
            else
                Logger.Log($"Failed to get desktop display mode (try #1/3). Target display: {display.Index}. SDL error: {SDL2Extensions.GetAndClearError()}", level: LogLevel.Error);

            // try the primary display mode.
            if (SDL.SDL_GetDisplayMode(display.Index, 0, out mode) == 0)
                return mode;
            else
                Logger.Log($"Failed to get desktop display mode (try #2/3). Target display: {display.Index}. SDL error: {SDL2Extensions.GetAndClearError()}", level: LogLevel.Error);

            // try the primary display's primary display mode.
            if (SDL.SDL_GetDisplayMode(0, 0, out mode) == 0)
                return mode;
            else
                Logger.Log($"Failed to get desktop display mode (try #3/3). Target display: primary. SDL error: {SDL2Extensions.GetAndClearError()}", level: LogLevel.Error);

            // finally return the current mode if everything else fails.
            if (SDL.SDL_GetWindowDisplayMode(windowHandle, out mode) >= 0)
                return mode;
            else
                Logger.Log($"Failed to get window display mode. SDL error: {SDL2Extensions.GetAndClearError()}", level: LogLevel.Error);

            throw new InvalidOperationException("couldn't retrieve valid display mode");
        }

        #endregion

        #region Events

        /// <summary>
        /// Invoked after the window has resized.
        /// </summary>
        public event Action? Resized;

        /// <summary>
        /// Invoked after the window's state has changed.
        /// </summary>
        public event Action<WindowState>? WindowStateChanged;

        /// <summary>
        /// Invoked when the window moves.
        /// </summary>
        public event Action<Point>? Moved;

        #endregion
    }
}
