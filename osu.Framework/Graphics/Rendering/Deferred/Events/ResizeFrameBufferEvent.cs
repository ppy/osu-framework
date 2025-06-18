// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct ResizeFrameBufferEvent(ResourceReference FrameBuffer, Vector2I Size)
    {
        public static RenderEvent Create(DeferredRenderer renderer, DeferredFrameBuffer frameBuffer, Vector2I size)
            => RenderEvent.Create(new ResizeFrameBufferEvent(renderer.Context.Reference(frameBuffer), size));
    }
}
