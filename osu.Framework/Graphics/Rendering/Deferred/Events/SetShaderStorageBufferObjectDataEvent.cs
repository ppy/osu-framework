// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetShaderStorageBufferObjectDataEvent(ResourceReference Buffer, int Index, MemoryReference Memory)
    {
        public static RenderEvent Create<T>(DeferredRenderer renderer, IDeferredShaderStorageBufferObject buffer, int index, T data)
            where T : unmanaged, IEquatable<T>
            => RenderEvent.Create(new SetShaderStorageBufferObjectDataEvent(renderer.Context.Reference(buffer), index, renderer.Context.AllocateObject(data)));
    }
}
