// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Pipelines;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    /// <summary>
    /// A pool for arbitrary <see cref="DeviceBuffer"/>s.
    /// </summary>
    internal class DeviceBufferPool : VeldridStagingResourcePool<IPooledDeviceBuffer>
    {
        private readonly uint bufferSize;
        private readonly BufferUsage usage;

        /// <summary>
        /// Creates a new <see cref="DeviceBufferPool"/>.
        /// </summary>
        /// <param name="pipeline">The graphics pipeline.</param>
        /// <param name="bufferSize">The size of each buffer in the pool.</param>
        /// <param name="usage">The buffer usage.</param>
        /// <param name="name">A short description.</param>
        public DeviceBufferPool(GraphicsPipeline pipeline, uint bufferSize, BufferUsage usage, string name)
            : base(pipeline, name)
        {
            this.bufferSize = bufferSize;
            this.usage = usage;
        }

        public IPooledDeviceBuffer Get()
        {
            if (TryGet(out IPooledDeviceBuffer? existing))
                return existing;

            existing = new PooledDeviceBuffer(Pipeline, bufferSize, usage);
            AddNewResource(existing);
            return existing;
        }

        private class PooledDeviceBuffer : IPooledDeviceBuffer
        {
            public DeviceBuffer Buffer { get; }
            private readonly GlobalStatistic<long> statistic;

            public PooledDeviceBuffer(GraphicsPipeline pipeline, uint bufferSize, BufferUsage usage)
            {
                Buffer = pipeline.Factory.CreateBuffer(new BufferDescription(bufferSize, usage | BufferUsage.Dynamic));
                statistic = GlobalStatistics.Get<long>("Native", $"PooledBuffer - {usage}");

                statistic.Value += Buffer.SizeInBytes;
            }

            public void Dispose()
            {
                Buffer.Dispose();
                statistic.Value -= Buffer.SizeInBytes;
            }
        }
    }
}
