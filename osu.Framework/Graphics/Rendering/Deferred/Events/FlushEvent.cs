// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct FlushEvent(RenderEventType Type, ResourceReference VertexBatch, int VertexCount) : IRenderEvent
    {
        public static FlushEvent Create(DeferredRenderer renderer, IDeferredVertexBatch vertexBatch, int vertexCount)
            => new FlushEvent(RenderEventType.Flush, renderer.Context.Reference(vertexBatch), vertexCount);
    }
}
