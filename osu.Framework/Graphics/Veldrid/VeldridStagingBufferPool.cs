// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridStagingBufferPool : VeldridStagingResourcePool<DeviceBuffer>
    {
        public VeldridStagingBufferPool(VeldridRenderer renderer)
            : base(renderer, nameof(VeldridStagingBufferPool))
        {
        }

        public DeviceBuffer Get(uint sizeInBytes)
        {
            if (TryGet(b => b.SizeInBytes >= sizeInBytes, out DeviceBuffer? buffer))
                return buffer;

            buffer = Renderer.Factory.CreateBuffer(new BufferDescription(sizeInBytes, BufferUsage.Staging));
            AddNewResource(buffer);
            return buffer;
        }
    }
}
