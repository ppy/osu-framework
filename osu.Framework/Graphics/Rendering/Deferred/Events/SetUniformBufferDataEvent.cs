// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetUniformBufferDataEvent(ResourceReference Buffer, UniformBufferData Data) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.SetUniformBufferData;

        public static SetUniformBufferDataEvent Create<T>(DeferredRenderer renderer, IDeferredUniformBuffer uniformBuffer, T data)
            where T : unmanaged, IEquatable<T>
        {
            return new SetUniformBufferDataEvent(renderer.Context.Reference(uniformBuffer), new UniformBufferData(renderer.Context.AllocateObject(data)));
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct UniformBufferData : IEquatable<UniformBufferData>
    {
        [FieldOffset(0)]
        public readonly MemoryReference Memory;

        [FieldOffset(0)]
        public readonly UniformBufferReference Range;

        public UniformBufferData(MemoryReference memory)
        {
            Memory = memory;
        }

        public UniformBufferData(UniformBufferReference range)
        {
            Range = range;
        }

        public bool Equals(UniformBufferData other)
        {
            return Memory.Equals(other.Memory)
                   && Range.Equals(other.Range);
        }

        public override bool Equals(object? obj)
        {
            return obj is UniformBufferData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Memory, Range);
        }
    }
}
