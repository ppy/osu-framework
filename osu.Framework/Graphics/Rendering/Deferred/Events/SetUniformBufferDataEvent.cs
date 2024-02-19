// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetUniformBufferDataEvent(ResourceReference Buffer, MemoryReference Memory) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.SetUniformBufferData;

        public static SetUniformBufferDataEvent Create<T>(DeferredRenderer renderer, IDeferredUniformBuffer uniformBuffer, T data)
            where T : unmanaged, IEquatable<T>
        {
            return new SetUniformBufferDataEvent(renderer.Context.Reference(uniformBuffer), renderer.Context.AllocateObject(data));
        }
    }
}
