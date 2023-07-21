// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;
using PrimitiveTopology = Veldrid.PrimitiveTopology;

namespace osu.Framework.Graphics.Veldrid.Batches
{
    internal class VeldridQuadBatch<T> : VeldridVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        public VeldridQuadBatch(VeldridRenderer renderer, int quads)
            : base(renderer, quads * IRenderer.VERTICES_PER_QUAD, PrimitiveTopology.TriangleList, VeldridIndexLayout.Quad)
        {
            if (quads > IRenderer.MAX_QUADS)
                throw new OverflowException($"Attempted to initialise a {nameof(VeldridQuadBatch<T>)} with more than {nameof(IRenderer)}.{nameof(IRenderer.MAX_QUADS)} quads ({IRenderer.MAX_QUADS}).");
        }
    }
}
