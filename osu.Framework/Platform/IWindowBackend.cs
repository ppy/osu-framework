// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Numerics;
using osu.Framework.Input.StateChanges;
using Veldrid;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Provides an implementation-agnostic interface on the backing windowing API.
    /// </summary>
    public interface IWindowBackend
    {
        #region Properties

        /// <summary>
        /// Gets and sets the window title.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Enables or disables the window visibility.
        /// </summary>
        bool Visible { get; set; }

        /// <summary>
        /// Returns or sets the window's position in screen space.
        /// </summary>
        Vector2 Position { get; set; }

        /// <summary>
        /// Returns or sets the window's internal size, before scaling.
        /// </summary>
        Vector2 Size { get; set; }

        /// <summary>
        /// Returns the scale of window's drawable area.
        /// In high-dpi environments this will be greater than one.
        /// </summary>
        float Scale { get; }

        /// <summary>
        /// Returns or sets the cursor's visibility within the window.
        /// </summary>
        bool CursorVisible { get; set; }

        /// <summary>
        /// Returns or sets whether the cursor is confined to the window's
        /// drawable area.
        /// </summary>
        bool CursorConfined { get; set; }

        /// <summary>
        /// Returns or sets the window's current <see cref="WindowState"/>.
        /// </summary>
        WindowState WindowState { get; set; }

        /// <summary>
        /// Returns true if window has been created.
        /// Returns false if the window has not yet been created, or has been closed.
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Queries the physical displays and their supported resolutions.
        /// </summary>
        IEnumerable<Display> Displays { get; }

        /// <summary>
        /// Gets the <see cref="Display"/> that this window is currently on.
        /// Can be null if running headless.
        /// </summary>
        Display Display { get; }

        /// <summary>
        /// Gets the <see cref="DisplayMode"/> for the display that this window is currently on.
        /// Can be null if running headless.
        /// </summary>
        DisplayMode DisplayMode { get; }

        #endregion

        #region Events

        /// <summary>
        /// Invoked once every window event loop.
        /// </summary>
        event Action Update;

        /// <summary>
        /// Invoked after the window has resized.
        /// </summary>
        event Action Resized;

        /// <summary>
        /// Invoked after the window's state has changed.
        /// </summary>
        event Action WindowStateChanged;

        /// <summary>
        /// Invoked when the user attempts to close the window.
        /// </summary>
        event Func<bool> CloseRequested;

        /// <summary>
        /// Invoked when the window is about to close.
        /// </summary>
        event Action Closed;

        /// <summary>
        /// Invoked when the window loses focus.
        /// </summary>
        event Action FocusLost;

        /// <summary>
        /// Invoked when the window gains focus.
        /// </summary>
        event Action FocusGained;

        /// <summary>
        /// Invoked when the window becomes visible.
        /// </summary>
        event Action Shown;

        /// <summary>
        /// Invoked when the window becomes invisible.
        /// </summary>
        event Action Hidden;

        /// <summary>
        /// Invoked when the mouse cursor enters the window.
        /// </summary>
        event Action MouseEntered;

        /// <summary>
        /// Invoked when the mouse cursor leaves the window.
        /// </summary>
        event Action MouseLeft;

        /// <summary>
        /// Invoked when the window moves.
        /// </summary>
        event Action<Vector2> Moved;

        /// <summary>
        /// Invoked when the user scrolls the mouse wheel over the window.
        /// </summary>
        event Action<MouseScrollRelativeInput> MouseWheel;

        /// <summary>
        /// Invoked when the user moves the mouse cursor within the window.
        /// </summary>
        event Action<MousePositionAbsoluteInput> MouseMove;

        /// <summary>
        /// Invoked when the user presses a mouse button.
        /// </summary>
        event Action<MouseButtonInput> MouseDown;

        /// <summary>
        /// Invoked when the user releases a mouse button.
        /// </summary>
        event Action<MouseButtonInput> MouseUp;

        /// <summary>
        /// Invoked when the user presses a key.
        /// </summary>
        event Action<KeyboardKeyInput> KeyDown;

        /// <summary>
        /// Invoked when the user releases a key.
        /// </summary>
        event Action<KeyboardKeyInput> KeyUp;

        /// <summary>
        /// Invoked when the user types a character.
        /// </summary>
        event Action<char> KeyTyped;

        /// <summary>
        /// Invoked when the user drops a file into the window.
        /// </summary>
        event Action<string> DragDrop;

        #endregion

        #region Methods

        /// <summary>
        /// Creates the concrete window implementation.
        /// </summary>
        void Create();

        /// <summary>
        /// Starts the event loop for the window.
        /// </summary>
        void Run();

        /// <summary>
        /// Requests that the window close.
        /// </summary>
        void Close();

        #endregion
    }
}
