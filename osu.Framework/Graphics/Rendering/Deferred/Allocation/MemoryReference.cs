// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Development;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    /// <summary>
    /// Represents a reference to a block of memory allocated via a <see cref="ResourceAllocator"/>.
    /// </summary>
    /// <param name="BufferId">The buffer which the memory exists in.</param>
    /// <param name="Offset">The offset into the buffer at which the block starts.</param>
    /// <param name="Length">The length of the block.</param>
    internal readonly record struct MemoryReference(int BufferId, int Offset, int Length)
    {
        /// <summary>
        /// Writes the contents of the memory block to a mapped <see cref="DeviceBuffer"/>.
        /// </summary>
        /// <param name="context">The deferred context.</param>
        /// <param name="target">The mapped <see cref="DeviceBuffer"/> to write to.</param>
        /// <param name="offsetInTarget">The offset in <paramref name="target"/> to write at.</param>
        public void WriteTo(DeferredContext context, MappedResource target, int offsetInTarget)
        {
            ThreadSafety.EnsureDrawThread();

            unsafe
            {
                Span<byte> targetSpan = new Span<byte>(target.Data.ToPointer(), (int)target.SizeInBytes);
                context.GetRegion(this).CopyTo(targetSpan[offsetInTarget..]);
            }
        }

        /// <summary>
        /// Writes the contents of the memory block to a <see cref="DeviceBuffer"/>.
        /// </summary>
        /// <param name="context">The deferred context.</param>
        /// <param name="target">The target to write to.</param>
        /// <param name="offsetInTarget">The offset in <paramref name="target"/> to write at.</param>
        public void WriteTo(DeferredContext context, DeviceBuffer target, int offsetInTarget)
        {
            ThreadSafety.EnsureDrawThread();
            context.Device.UpdateBuffer(target, (uint)offsetInTarget, context.GetRegion(this));
        }
    }
}
