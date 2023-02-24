// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Bindables;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Logging;
using osuTK.Graphics.ES30;

namespace osu.Framework.Platform.Windows
{
    internal class WindowsGLRenderer : GLRenderer, IWindowsRenderer
    {
        public IBindable<FullscreenCapability> FullscreenCapability => fullscreenCapability;
        private readonly Bindable<FullscreenCapability> fullscreenCapability = new Bindable<FullscreenCapability>();

        private readonly WindowsGameHost host;

        public WindowsGLRenderer(WindowsGameHost host)
        {
            this.host = host;
        }

        protected override void Initialise(IGraphicsSurface graphicsSurface)
        {
            base.Initialise(graphicsSurface);

            WindowsWindow windowsWindow = (WindowsWindow)host.Window;

            bool isIntel = GL.GetString(StringName.Vendor).Trim() == "Intel";

            if (isIntel)
            {
                // Exclusive fullscreen is always supported on Intel.
                fullscreenCapability.Value = Windows.FullscreenCapability.Capable;
            }
            else
            {
                // For all other vendors, support depends on the system setup - e.g. NVIDIA Optimus doesn't support exclusive fullscreen with OpenGL.
                windowsWindow.IsActive.BindValueChanged(_ => detectFullscreenCapability(windowsWindow));
                windowsWindow.WindowStateChanged += _ => detectFullscreenCapability(windowsWindow);
                detectFullscreenCapability(windowsWindow);
            }
        }

        private CancellationTokenSource? fullscreenCapabilityDetectionCancellationSource;

        private void detectFullscreenCapability(IWindow window)
        {
            fullscreenCapabilityDetectionCancellationSource?.Cancel();
            fullscreenCapabilityDetectionCancellationSource?.Dispose();
            fullscreenCapabilityDetectionCancellationSource = null;

            if (window.WindowState != WindowState.Fullscreen || !window.IsActive.Value || fullscreenCapability.Value != Windows.FullscreenCapability.Unknown)
                return;

            var cancellationSource = fullscreenCapabilityDetectionCancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;

            // 50 attempts, 100ms apart = run the detection for a total of 5 seconds before yielding an incapable state.
            const int max_attempts = 50;
            const int time_per_attempt = 100;
            int attempts = 0;

            queueNextAttempt();

            void queueNextAttempt() => Task.Delay(time_per_attempt, cancellationToken).ContinueWith(_ =>
            {
                if (cancellationToken.IsCancellationRequested || window.WindowState != WindowState.Fullscreen || !window.IsActive.Value)
                    return;

                attempts++;

                try
                {
                    SHQueryUserNotificationState(out var notificationState);

                    var capability = notificationState == QueryUserNotificationState.QUNS_RUNNING_D3D_FULL_SCREEN
                        ? Windows.FullscreenCapability.Capable
                        : Windows.FullscreenCapability.Incapable;

                    if (capability == Windows.FullscreenCapability.Incapable && attempts < max_attempts)
                    {
                        queueNextAttempt();
                        return;
                    }

                    fullscreenCapability.Value = capability;
                    Logger.Log($"Exclusive fullscreen capability: {fullscreenCapability.Value} ({notificationState})");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to detect fullscreen capabilities.");
                    fullscreenCapability.Value = Windows.FullscreenCapability.Capable;
                }
            }, cancellationToken);
        }

        [DllImport("shell32.dll")]
        private static extern int SHQueryUserNotificationState(out QueryUserNotificationState state);

        private enum QueryUserNotificationState
        {
            QUNS_NOT_PRESENT = 1,
            QUNS_BUSY = 2,
            QUNS_RUNNING_D3D_FULL_SCREEN = 3,
            QUNS_PRESENTATION_MODE = 4,
            QUNS_ACCEPTS_NOTIFICATIONS = 5,
            QUNS_QUIET_TIME = 6
        }
    }
}
