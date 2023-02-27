// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    /// <summary>
    /// Describes the layout of a uniform set in a Veldrid shader.
    /// </summary>
    /// <param name="Set">The described set.</param>
    /// <param name="Layout">The layout of the set.</param>
    internal record VeldridUniformLayout(int Set, ResourceLayout Layout) : IDisposable
    {
        public void Dispose()
        {
            Layout.Dispose();
        }
    }
}
