// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Veldrid;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    /// <summary>
    /// A pool for arbitrary <see cref="DeviceBuffer"/>s.
    /// </summary>
    internal class DeviceBufferPool : VeldridStagingResourcePool<IPooledDeviceBuffer>
    {
        private readonly VeldridDevice device;
        private readonly uint bufferSize;
        private readonly BufferUsage usage;

        /// <summary>
        /// Creates a new <see cref="DeviceBufferPool"/>.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="bufferSize">The size of each buffer in the pool.</param>
        /// <param name="usage">The buffer usage.</param>
        /// <param name="name">A short description.</param>
        public DeviceBufferPool(VeldridDevice device, uint bufferSize, BufferUsage usage, string name)
            : base(device, name)
        {
            this.device = device;
            this.bufferSize = bufferSize;
            this.usage = usage;
        }

        public IPooledDeviceBuffer Get()
        {
            if (TryGet(_ => true, out IPooledDeviceBuffer? existing))
                return existing;

            existing = new PooledDeviceBuffer(device, bufferSize, usage);
            AddNewResource(existing);
            return existing;
        }

        private class PooledDeviceBuffer : IPooledDeviceBuffer
        {
            public DeviceBuffer Buffer { get; }
            private readonly GlobalStatistic<long> statistic;

            public PooledDeviceBuffer(VeldridDevice device, uint bufferSize, BufferUsage usage)
            {
                Buffer = device.Factory.CreateBuffer(new BufferDescription(bufferSize, usage | BufferUsage.Dynamic));
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
