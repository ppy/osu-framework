// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetFrameBufferEvent(ResourceReference FrameBuffer)
    {
        public static RenderEvent Create(DeferredRenderer renderer, IFrameBuffer? frameBuffer)
            => RenderEvent.Create(new SetFrameBufferEvent(renderer.Context.Reference(frameBuffer)));
    }
}
