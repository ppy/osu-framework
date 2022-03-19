// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics
{
    public readonly struct DrawState
    {
        public readonly QuadBatch<TexturedVertex2D> QuadBatch;

        public DrawState(QuadBatch<TexturedVertex2D> quadBatch)
        {
            QuadBatch = quadBatch;
        }

        public DrawState WithQuadBatch(QuadBatch<TexturedVertex2D> quadBatch) => new DrawState(quadBatch);
    }
}
