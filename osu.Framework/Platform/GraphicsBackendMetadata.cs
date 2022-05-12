// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform
{
    /// <summary>
    /// Contains information to identify a specific graphics backend vendor and renderer
    /// </summary>
    public readonly struct GraphicsBackendMetadata
    {
        /// <summary>
        /// Product or software used to render the display
        /// </summary>
        public readonly string RendererName;

        /// <summary>
        /// Vendor of the renderer product or software
        /// </summary>
        public readonly string Vendor;

        /// <summary>
        /// OpenGL version string, including vendor-specific version info
        /// </summary>
        public readonly string VersionString;

        public GraphicsBackendMetadata(string rendererName, string vendor, string versionString)
        {
            RendererName = rendererName;
            Vendor = vendor;
            VersionString = versionString;
        }
    }
}
