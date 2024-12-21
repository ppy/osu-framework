// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Colour;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Information for how the current frame buffer should be cleared.
    /// </summary>
    public readonly struct ClearInfo
    {
        /// <summary>
        /// The default clear properties, as defined by OpenGL.
        /// </summary>
        public static ClearInfo Default => new ClearInfo(default);

        /// <summary>
        /// The colour to write to the frame buffer.
        /// </summary>
        public readonly PremultipliedColour Colour;

        /// <summary>
        /// The depth to write to the frame buffer.
        /// </summary>
        public readonly double Depth;

        /// <summary>
        /// The stencil value to write to the frame buffer.
        /// </summary>
        public readonly int Stencil;

        public ClearInfo(PremultipliedColour colour = default, double depth = 1f, int stencil = 0)
        {
            Colour = colour;
            Depth = depth;
            Stencil = stencil;
        }
    }
}
