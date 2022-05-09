// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Framework.Platform
{
    public static class PlatformWorkaroundDetector
    {
        public static PlatformWorkaroundMode DetectWorkaround(GraphicsBackendMetadata backendMetadata)
            => DetectWorkaround(backendMetadata, RuntimeInfo.OS);

        public static PlatformWorkaroundMode DetectWorkaround(GraphicsBackendMetadata backendMetadata, RuntimeInfo.Platform platform)
        {
            //HACK: Force macOS to use glFinish for the time being, until it is further investigated (https://github.com/ppy/osu/issues/7447)
            if (platform == RuntimeInfo.Platform.macOS)
                return PlatformWorkaroundMode.ForceFinish;

            if (backendMetadata.Vendor == "Intel")
            {
                if (backendMetadata.RendererName.Contains("UHD Graphics 620") ||
                    backendMetadata.RendererName.Contains("UHD Graphics 630"))
                {
                    // UHD 620/630 needs custom workarounds for Windows
                    //  due to the bug causing excess overload until
                    //  dwm or the driver ends up crashing.
                    if (platform == RuntimeInfo.Platform.Windows)
                        return PlatformWorkaroundMode.WorkaroundIntelUHD630_Windows;

                    // On macOS there is simply just a scheduling bug,
                    //  which can be kept in sync by just a mere glFinish.
                    if (platform == RuntimeInfo.Platform.macOS)
                        return PlatformWorkaroundMode.ForceFinish;
                    
                    // UHD 630 is not broken on Linux due to the Mesa driver being mature
                    //  due to being open-source, and supported by the community.
                    // Perhaps a check should be here for Linux to see if it's not running on Mesa drivers?
                }
            }

            return PlatformWorkaroundMode.Default;
        }
    }

    public enum PlatformWorkaroundMode
    {
        /// <summary>
        /// Default is to glFinish on VSync, but don't otherwise.
        /// </summary>
        [Description("No workaround")]
        Default,

        /// <summary>
        /// Acts just like <see cref="Default"/>, but should indicate
        ///  forced workaround detection.
        /// </summary>
        [Description("Automatic workaround detection")]
        Auto,

        /// <summary>
        /// Always call glFinish after SwapBuffers.
        /// </summary>
        [Description("Force drawing synchronization")]
        ForceFinish,

        /// <summary>
        /// Never call glFinish after SwapBuffers.
        /// </summary>
        [Description("Disable drawing synchronization")]
        ForceNoFinish,

        /// <summary>
        /// Use InvalidateRect + glFinish twice to
        ///  synchronize the UHD 620/630 driver
        /// </summary>
        [Description("Intel UHD 620/630 stutter workaround (Windows)")]
        WorkaroundIntelUHD630_Windows
    }
}
