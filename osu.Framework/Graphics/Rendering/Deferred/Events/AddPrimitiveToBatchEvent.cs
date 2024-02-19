// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct AddPrimitiveToBatchEvent(ResourceReference VertexBatch, MemoryReference Memory) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.AddPrimitiveToBatch;

        public static AddPrimitiveToBatchEvent Create<T>(DeferredRenderer renderer, DeferredVertexBatch<T> batch, ReadOnlySpan<T> vertices)
            where T : unmanaged, IVertex, IEquatable<T>
        {
            return new AddPrimitiveToBatchEvent(renderer.Context.Reference(batch), renderer.Context.AllocateRegion(vertices));
        }
    }
}
