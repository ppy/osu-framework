// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetUniformBufferEvent(RenderEventType Type, ResourceReference Name, ResourceReference Buffer) : IRenderEvent
    {
        public static SetUniformBufferEvent Create(DeferredRenderer renderer, string name, IUniformBuffer buffer)
            => new SetUniformBufferEvent(RenderEventType.SetUniformBuffer, renderer.Context.Reference(name), renderer.Context.Reference(buffer));
    }
}
