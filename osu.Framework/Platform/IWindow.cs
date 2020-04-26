// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osuTK.Platform;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Interface representation of the game window, intended to hide any implementation-specific code.
    /// Currently inherits from osuTK; this will be removed as part of the <see cref="GameWindow"/> refactor.
    /// </summary>
    public interface IWindow : IGameWindow
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
        /// Whether the OS cursor is currently contained within the game window.
        /// </summary>
        bool CursorInWindow { get; }

        /// <summary>
        /// Controls the state of the OS cursor.
        /// </summary>
        CursorState CursorState { get; set; }

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
        Bindable<Display> CurrentDisplay { get; }

        /// <summary>
        /// Gets the <see cref="DisplayMode"/> for the display that this window is currently on.
        /// </summary>
        DisplayMode CurrentDisplayMode { get; }
    }
}
