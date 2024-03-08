// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetUniformBufferDataEvent(ResourceReference Buffer, UniformBufferData Data)
    {
        public static RenderEvent Create<T>(DeferredRenderer renderer, IDeferredUniformBuffer uniformBuffer, T data)
            where T : unmanaged, IEquatable<T>
            => RenderEvent.Init(new SetUniformBufferDataEvent(renderer.Context.Reference(uniformBuffer), new UniformBufferData(renderer.Context.AllocateObject(data))));
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct UniformBufferData : IEquatable<UniformBufferData>
    {
        [field: FieldOffset(0)]
        private readonly bool isRangeReference;

        [FieldOffset(1)]
        private readonly MemoryReference memory;

        [FieldOffset(1)]
        private readonly UniformBufferReference range;

        public UniformBufferData(MemoryReference memory)
        {
            this.memory = memory;
            isRangeReference = false;
        }

        public UniformBufferData(UniformBufferReference range)
        {
            this.range = range;
            isRangeReference = true;
        }

        public MemoryReference Memory
        {
            get
            {
                Debug.Assert(!isRangeReference);
                return memory;
            }
        }

        public UniformBufferReference Range
        {
            get
            {
                Debug.Assert(isRangeReference);
                return range;
            }
        }

        public bool Equals(UniformBufferData other)
        {
            return memory.Equals(other.memory)
                   && range.Equals(other.range);
        }

        public override bool Equals(object? obj)
        {
            return obj is UniformBufferData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(memory, range);
        }
    }
}
