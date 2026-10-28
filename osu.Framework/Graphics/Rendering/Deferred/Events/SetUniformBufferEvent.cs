// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetUniformBufferEvent(ResourceReference Name, ResourceReference Buffer)
    {
        public static RenderEvent Create(DeferredRenderer renderer, string name, IUniformBuffer buffer)
            => RenderEvent.Create(new SetUniformBufferEvent(renderer.Context.Reference(name), renderer.Context.Reference(buffer)));
    }
}
