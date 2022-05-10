// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform
{
    public static class PlatformWorkaroundDetector
    {
        public static PlatformWorkaround DetectWorkaround(GraphicsBackendMetadata backendMetadata)
            => DetectWorkaround(backendMetadata, RuntimeInfo.OS);

        public static PlatformWorkaround DetectWorkaround(GraphicsBackendMetadata backendMetadata, RuntimeInfo.Platform platform)
        {
            //HACK: Force macOS to use glFinish for the time being, until it is further investigated (https://github.com/ppy/osu/issues/7447)
            if (platform == RuntimeInfo.Platform.macOS)
                return PlatformWorkaround.FinishAfterSwapAlways;

            if (backendMetadata.Vendor == "Intel")
            {
                if (backendMetadata.RendererName.Contains("UHD Graphics 620") ||
                    backendMetadata.RendererName.Contains("UHD Graphics 630"))
                {
                    // UHD 620/630 needs custom workarounds for Windows
                    //  due to the bug causing excess overload until
                    //  dwm or the driver ends up crashing.
                    if (platform == RuntimeInfo.Platform.Windows)
                        return PlatformWorkaround.WindowsInvalidateRect
                             | PlatformWorkaround.FinishBeforeSwap //TODO: wglSwapLayerBuffers is preferred over an explicit pre-SwapBuffers glFinish
                             | PlatformWorkaround.FinishAfterSwapAlways;

                    // On macOS there is simply just a scheduling bug,
                    //  which can be kept in sync by just a mere glFinish.
                    if (platform == RuntimeInfo.Platform.macOS)
                        return PlatformWorkaround.FinishAfterSwapAlways;

                    // UHD 630 is not broken on Linux due to the Mesa driver being mature
                    //  due to being open-source, and supported by the community.
                    // Perhaps a check should be here for Linux to see if it's not running on Mesa drivers?
                }
            }

            return PlatformWorkaround.Default;
        }
    }

    [Flags]
    public enum PlatformWorkaround
    {
        /// <summary>
        /// Special value to indicate that workarounds need to be automatically detected.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Default workaround configuration used if no workarounds are required.
        /// </summary>
        Default = FinishAfterSwapVSync,

        /// <summary>
        /// Perform glFinish after SwapBuffers if VSync is enabled.
        /// </summary>
        FinishAfterSwapVSync = (1 << 0),

        /// <summary>
        /// Perform glFinish after SwapBuffers if VSync is not enabled.
        /// </summary>
        FinishAfterSwapNoVSync = (1 << 1),

        /// <summary>
        /// Perform glFinish after SwapBuffers, no matter if VSync is enabled or not.
        /// </summary>
        FinishAfterSwapAlways = FinishAfterSwapVSync | FinishAfterSwapNoVSync,

        /// <summary>
        /// Perform glFinish before SwapBuffers.
        /// </summary>
        FinishBeforeSwap = (1 << 2),

        /// <summary>
        /// On Windows, perform InvalidateRect as the first thing to work around certain driver bugs.
        /// </summary>
        WindowsInvalidateRect = (1 << 3)
    }
}
