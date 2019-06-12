// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osuTK;
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
        VSyncMode VSync { get; set; }

        /// <summary>
        /// Returns the default <see cref="WindowMode"/> for the implementation.
        /// </summary>
        WindowMode DefaultWindowMode { get; }

        /// <summary>
        /// Gets the <see cref="DisplayDevice"/> that this window is currently on.
        /// </summary>
        DisplayDevice CurrentDisplay { get; }

        /// <summary>
        /// Whether this <see cref="IWindow"/> is active (in the foreground).
        /// </summary>
        IBindable<bool> IsActive { get; }

        /// <summary>
        /// Provides a <see cref="IBindable{MarginPadding}"/> that can be used to keep track of the "safe area" insets on mobile
        /// devices. This usually corresponds to areas of the screen hidden under notches and rounded corners.
        /// The safe area insets are provided by the operating system and dynamically change as the user rotates the device.
        /// </summary>
        IBindable<MarginPadding> SafeAreaPadding { get; }

        /// <summary>
        /// The <see cref="WindowMode"/>s supported by this <see cref="IWindow"/> implementation.
        /// </summary>
        IBindableList<WindowMode> SupportedWindowModes { get; }

        /// <summary>
        /// Available resolutions for full-screen display.
        /// </summary>
        IEnumerable<DisplayResolution> AvailableResolutions { get; }
    }
}
