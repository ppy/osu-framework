// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Batches;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL
{
    public class OpenGLRenderer : IRenderer
    {
        public IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveType primitiveType) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new LinearBatch<TVertex>(size, maxBuffers, primitiveType);

        public IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new QuadBatch<TVertex>(size, maxBuffers);
    }
}
