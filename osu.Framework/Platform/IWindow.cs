// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

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
        /// Invoked when the window close (X) button or another platform-native exit action has been pressed.
        /// </summary>
        [CanBeNull]
        event Action ExitRequested;

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
        /// <remarks>If the cursor is <see cref="Platform.CursorState.Confined"/>, <see cref="CursorConfineRect"/> will be used.</remarks>
        CursorState CursorState { get; set; }

        /// <summary>
        /// Area to which the mouse cursor is confined to when <see cref="CursorState"/> is <see cref="Platform.CursorState.Confined"/>.
        /// </summary>
        /// <remarks>
        /// Will confine to the whole window by default (or when set to <c>null</c>).
        /// Supported fully on desktop platforms, and on Android when relative mode is enabled.
        /// </remarks>
        RectangleF? CursorConfineRect { get; set; }

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
        /// Forcefully closes the window.
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
        /// The minimum size of the window.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when setting a negative size, or a size greater than <see cref="MaxSize"/>.</exception>
        Size MinSize { get; set; }

        /// <summary>
        /// The maximum size of the window.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when setting a negative or zero size, or a size less than <see cref="MinSize"/>.</exception>
        Size MaxSize { get; set; }

        /// <summary>
        /// The window title.
        /// </summary>
        string Title { get; set; }
    }
}
