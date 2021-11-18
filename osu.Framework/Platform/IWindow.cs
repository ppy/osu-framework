// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Configuration;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Interface representation of the game window, intended to hide any implementation-specific code.
    /// </summary>
    public interface IWindow : IDisposable
    {
        /// <summary>
        /// Cycles through the available <see cref="WindowMode"/>s as determined by <see cref="SupportedWindowModes"/>.
        /// </summary>
        void CycleMode();

        /// <summary>
        /// Configures the <see cref="IWindow"/> based on the provided <see cref="FrameworkConfigManager"/>.
        /// </summary>
        /// <param name="config">The configuration manager to use.</param>
        void SetupWindow(FrameworkConfigManager config);

        /// <summary>
        /// Creates the concrete window implementation.
        /// </summary>
        void Create();

        /// <summary>
        /// Return value decides whether we should intercept and cancel this exit (if possible).
        /// </summary>
        [CanBeNull]
        event Func<bool> ExitRequested;

        /// <summary>
        /// Invoked when the <see cref="IWindow"/> has closed.
        /// </summary>
        [CanBeNull]
        event Action Exited;

        /// <summary>
        /// Invoked when the <see cref="IWindow"/> client size has changed.
        /// </summary>
        [CanBeNull]
        event Action Resized;

        /// <summary>
        /// Invoked when the system keyboard layout has changed.
        /// </summary>
        event Action KeymapChanged;

        /// <summary>
        /// Whether the OS cursor is currently contained within the game window.
        /// </summary>
        IBindable<bool> CursorInWindow { get; }

        /// <summary>
        /// Controls the state of the OS cursor.
        /// </summary>
        CursorState CursorState { get; set; }

        /// <summary>
        /// Controls the state of the window.
        /// </summary>
        WindowState WindowState { get; set; }

        /// <summary>
        /// Controls the vertical sync mode of the screen.
        /// </summary>
        bool VerticalSync { get; set; }

        /// <summary>
        /// Returns the default <see cref="WindowMode"/> for the implementation.
        /// </summary>
        WindowMode DefaultWindowMode { get; }

        /// <summary>
        /// Whether this <see cref="IWindow"/> is active (in the foreground).
        /// </summary>
        IBindable<bool> IsActive { get; }

        /// <summary>
        /// Provides a <see cref="BindableSafeArea"/> that can be used to keep track of the "safe area" insets on mobile
        /// devices. This usually corresponds to areas of the screen hidden under notches and rounded corners.
        /// The safe area insets are provided by the operating system and dynamically change as the user rotates the device.
        /// </summary>
        BindableSafeArea SafeAreaPadding { get; }

        /// <summary>
        /// The <see cref="WindowMode"/>s supported by this <see cref="IWindow"/> implementation.
        /// </summary>
        IBindableList<WindowMode> SupportedWindowModes { get; }

        /// <summary>
        /// Provides a <see cref="Bindable{WindowMode}"/> that manages the current window mode.
        /// Supported window modes for the current platform can be retrieved via <see cref="SupportedWindowModes"/>.
        /// </summary>
        Bindable<WindowMode> WindowMode { get; }

        /// <summary>
        /// Exposes the physical displays as an <see cref="IEnumerable{Display}"/>.
        /// </summary>
        IEnumerable<Display> Displays { get; }

        /// <summary>
        /// Gets the <see cref="Display"/> that has been set as "primary" or "default" in the operating system.
        /// </summary>
        Display PrimaryDisplay { get; }

        /// <summary>
        /// Exposes the <see cref="Display"/> that this window is currently on as a <see cref="Bindable{Display}"/>.
        /// </summary>
        Bindable<Display> CurrentDisplayBindable { get; }

        /// <summary>
        /// The <see cref="DisplayMode"/> for the display that this window is currently on.
        /// </summary>
        IBindable<DisplayMode> CurrentDisplayMode { get; }

        /// <summary>
        /// Makes this window the current graphics context, if appropriate for the driver.
        /// </summary>
        void MakeCurrent();

        /// <summary>
        /// Clears the current graphics context, if appropriate for the driver.
        /// </summary>
        void ClearCurrent();

        /// <summary>
        /// Request close.
        /// </summary>
        void Close();

        /// <summary>
        /// Start the window's run loop.
        /// Is a blocking call on desktop platforms, and a non-blocking call on mobile platforms.
        /// </summary>
        void Run();

        /// <summary>
        /// Requests that the graphics backend perform a buffer swap.
        /// </summary>
        void SwapBuffers();

        /// <summary>
        /// Whether the window currently has focus.
        /// </summary>
        bool Focused { get; }

        /// <summary>
        /// Convert a screen based coordinate to local window space.
        /// </summary>
        /// <param name="point"></param>
        Point PointToClient(Point point);

        /// <summary>
        /// Convert a window based coordinate to global screen space.
        /// </summary>
        /// <param name="point"></param>
        Point PointToScreen(Point point);

        /// <summary>
        /// The client size of the window (excluding any window decoration/border).
        /// </summary>
        Size ClientSize { get; }

        /// <summary>
        /// The window title.
        /// </summary>
        string Title { get; set; }
    }
}
