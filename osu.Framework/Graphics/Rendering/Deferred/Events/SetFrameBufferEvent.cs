// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetFrameBufferEvent(RenderEventType Type, ResourceReference FrameBuffer) : IRenderEvent
    {
        public static SetFrameBufferEvent Create(DeferredRenderer renderer, IFrameBuffer? frameBuffer)
            => new SetFrameBufferEvent(RenderEventType.SetFrameBuffer, renderer.Context.Reference(frameBuffer));
    }
}
