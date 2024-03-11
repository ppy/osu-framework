// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct ResizeFrameBufferEvent(RenderEventType Type, ResourceReference FrameBuffer, Vector2I Size) : IRenderEvent
    {
        public static ResizeFrameBufferEvent Create(DeferredRenderer renderer, DeferredFrameBuffer frameBuffer, Vector2I size)
            => new ResizeFrameBufferEvent(RenderEventType.ResizeFrameBuffer, renderer.Context.Reference(frameBuffer), size);
    }
}
