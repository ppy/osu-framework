// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Information for how depth should be handled.
    /// </summary>
    public readonly struct DepthInfo : IEquatable<DepthInfo>
    {
        /// <summary>
        /// The default depth properties, as defined by OpenGL.
        /// </summary>
        public static DepthInfo Default => new DepthInfo(true);

        /// <summary>
        /// Whether depth testing should occur.
        /// </summary>
        public readonly bool DepthTest;

        /// <summary>
        /// Whether to write to the depth buffer if the depth test passed.
        /// </summary>
        public readonly bool WriteDepth;

        /// <summary>
        /// The depth test function.
        /// </summary>
        public readonly DepthStencilFunction Function;

        public DepthInfo(bool depthTest = true, bool writeDepth = true, DepthStencilFunction function = DepthStencilFunction.LessThan)
        {
            DepthTest = depthTest;
            WriteDepth = writeDepth;
            Function = function;
        }

        public bool Equals(DepthInfo other) => DepthTest == other.DepthTest && WriteDepth == other.WriteDepth && Function == other.Function;
    }
}
