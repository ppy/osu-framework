// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct AddPrimitiveToBatchEvent(RenderEventType Type, ResourceReference VertexBatch, MemoryReference Memory) : IRenderEvent
    {
        public static AddPrimitiveToBatchEvent Create<T>(DeferredRenderer renderer, DeferredVertexBatch<T> batch, ReadOnlySpan<T> vertices)
            where T : unmanaged, IVertex, IEquatable<T>
            => new AddPrimitiveToBatchEvent(RenderEventType.AddPrimitiveToBatch, renderer.Context.Reference(batch), renderer.Context.AllocateRegion(vertices));
    }
}
