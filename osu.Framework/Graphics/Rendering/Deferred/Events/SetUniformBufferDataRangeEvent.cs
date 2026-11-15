// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetUniformBufferDataRangeEvent(ResourceReference Buffer, UniformBufferReference Range)
    {
        public static RenderEvent Create<T>(DeferredRenderer renderer, IDeferredUniformBuffer uniformBuffer, UniformBufferReference range)
            where T : unmanaged, IEquatable<T>
            => RenderEvent.Create(new SetUniformBufferDataRangeEvent(renderer.Context.Reference(uniformBuffer), range));
    }
}
