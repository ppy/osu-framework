// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Batches
{
    internal class VeldridLinearBatch<T> : VeldridVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        public VeldridLinearBatch(VeldridRenderer renderer, int size, PrimitiveTopology type)
            : base(renderer, size, type, VeldridIndexLayout.Linear)
        {
        }
    }
}
