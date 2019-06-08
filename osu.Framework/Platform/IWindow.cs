// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        void CycleMode();

        void SetupWindow(FrameworkConfigManager config);

        event Func<bool> ExitRequested;

        event Action Exited;

        bool CursorInWindow { get; }

        CursorState CursorState { get; set; }

        VSyncMode VSync { get; set; }

        WindowMode DefaultWindowMode { get; }

        DisplayDevice CurrentDisplay { get; }

        IBindable<bool> IsActive { get; }

        IBindable<MarginPadding> SafeAreaPadding { get; }

        IBindableList<WindowMode> SupportedWindowModes { get; }
    }
}
