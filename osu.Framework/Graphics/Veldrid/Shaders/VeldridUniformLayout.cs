// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal record VeldridUniformLayout(int Set, ResourceLayout Layout) : IDisposable
    {
        public void Dispose()
        {
            Layout.Dispose();
        }
    }
}
