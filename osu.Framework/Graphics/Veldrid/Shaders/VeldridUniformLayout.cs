// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    /// <summary>
    /// Describes the layout of a uniform set in a Veldrid shader.
    /// </summary>
    internal class VeldridUniformLayout : IDisposable
    {
        public readonly int Set;
        public readonly ResourceLayout Layout;

        /// <param name="set">The described set.</param>
        /// <param name="layout">The layout of the set.</param>
        public VeldridUniformLayout(int set, ResourceLayout layout)
        {
            Set = set;
            Layout = layout;
        }

        public void Dispose()
        {
            Layout.Dispose();
        }
    }
}
