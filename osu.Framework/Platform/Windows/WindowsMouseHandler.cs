// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Versioning;
using osu.Framework.Input.Handlers.Mouse;
using osuTK;

namespace osu.Framework.Platform.Windows
{
    /// <summary>
    /// A windows specific mouse input handler which overrides the SDL3 implementation of raw input.
    /// This is done to better handle quirks of some devices.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal partial class WindowsMouseHandler : MouseHandler
    {
        private IWindowsWindow window = null!;

        public override bool IsActive => Enabled.Value;

        public override bool Initialize(GameHost host)
        {
            if (host.Window is not IWindowsWindow windowsWindow)
                return false;

            window = windowsWindow;

            if (window is SDL2WindowsWindow)
                initialiseSDL2(host);

            return base.Initialize(host);
        }

        public override void FeedbackMousePositionChange(Vector2 position, bool isSelfFeedback)
        {
            window.LastMousePosition = position;
            base.FeedbackMousePositionChange(position, isSelfFeedback);
        }
    }
}
