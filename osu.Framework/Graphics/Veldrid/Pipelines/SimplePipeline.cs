// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using osu.Framework.Platform;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Pipelines
{
    /// <summary>
    /// A non-graphical pipeline that provides a command list and handles basic tasks like uploading textures.
    /// </summary>
    internal class SimplePipeline
    {
        public GraphicsDevice Device => device.Device;

        public ResourceFactory Factory => device.Factory;

        public readonly CommandList Commands;

        public ulong LatestCompletedExecutionIndex { get; private set; }

        public ulong ExecutionIndex { get; private set; }

        /// <summary>
        /// A list of fences which tracks in-flight frames for the purpose of knowing the last completed frame.
        /// This is tracked for the purpose of exposing <see cref="LatestCompletedExecutionIndex"/>.
        /// </summary>
        private readonly List<FrameCompletionFence> pendingFramesFences = new List<FrameCompletionFence>();

        /// <summary>
        /// We are using fences every frame. Construction can be expensive, so let's pool some.
        /// </summary>
        private readonly Queue<Fence> perFrameFencePool = new Queue<Fence>();

        private readonly VeldridStagingTexturePool stagingTexturePool;
        private readonly VeldridDevice device;

        public SimplePipeline(VeldridDevice device)
        {
            this.device = device;

            stagingTexturePool = new VeldridStagingTexturePool(this);
            Commands = device.Factory.CreateCommandList();
        }

        /// <summary>
        /// Begins the pipeline.
        /// </summary>
        public virtual void Begin()
        {
            updateLastCompletedFrameIndex();
            ExecutionIndex++;

            stagingTexturePool.NewFrame();
            Commands.Begin();
        }

        /// <summary>
        /// Finishes the pipeline and submits it for processing by the device.
        /// </summary>
        public void End()
        {
            // This is returned via the end-of-lifetime tracking in `pendingFrameFences`.
            // See `updateLastCompletedFrameIndex`.
            if (!perFrameFencePool.TryDequeue(out Fence? fence))
                fence = Factory.CreateFence(false);
            pendingFramesFences.Add(new FrameCompletionFence(fence, ExecutionIndex));

            Commands.End();
            device.Device.SubmitCommands(Commands, fence);
        }

        /// <summary>
        /// Updates a <see cref="global::Veldrid.Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="global::Veldrid.Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The texture data.</param>
        /// <typeparam name="T">The pixel type.</typeparam>
        public void UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data)
            where T : unmanaged
        {
            // This code is doing the same as the simpler approach of:
            //
            // Device.UpdateTexture(texture, data, (uint)x, (uint)y, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            //
            // Except we are using a staging texture pool to avoid the alloc overhead of each staging texture.
            var staging = stagingTexturePool.Get(width, height, texture.Format);
            device.Device.UpdateTexture(staging, data, 0, 0, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            Commands.CopyTexture(staging, 0, 0, 0, 0, 0, texture, (uint)x, (uint)y, 0, (uint)level, 0, (uint)width, (uint)height, 1, 1);
        }

        /// <summary>
        /// Updates a <see cref="global::Veldrid.Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="global::Veldrid.Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The texture data.</param>
        /// <param name="rowLengthInBytes">The number of bytes per row of the image to read from <paramref name="data"/>.</param>
        public void UpdateTexture(Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes)
        {
            var staging = stagingTexturePool.Get(width, height, texture.Format);

            unsafe
            {
                MappedResource mappedData = Device.Map(staging, MapMode.Write);

                try
                {
                    void* srcPtr = data.ToPointer();
                    void* dstPtr = mappedData.Data.ToPointer();

                    for (int i = 0; i < height; i++)
                    {
                        Unsafe.CopyBlockUnaligned(dstPtr, srcPtr, mappedData.RowPitch);

                        srcPtr = Unsafe.Add<byte>(srcPtr, rowLengthInBytes);
                        dstPtr = Unsafe.Add<byte>(dstPtr, (int)mappedData.RowPitch);
                    }
                }
                finally
                {
                    Device.Unmap(staging);
                }
            }

            Commands.CopyTexture(
                staging, 0, 0, 0, 0, 0,
                texture, (uint)x, (uint)y, 0, (uint)level, 0, (uint)width, (uint)height, 1, 1);
        }

        private void updateLastCompletedFrameIndex()
        {
            int? lastSignalledFenceIndex = null;

            // We have a sequential list of all fences which are in flight.
            // Frame usages are assumed to be sequential and linear.
            //
            // Iterate backwards to find the last signalled fence, which can be considered the last completed frame index.
            for (int i = pendingFramesFences.Count - 1; i >= 0; i--)
            {
                var fence = pendingFramesFences[i];

                if (!fence.Fence.Signaled)
                {
                    // this rule is broken on metal, if a new command buffer has been submitted while a previous fence wasn't signalled yet,
                    // then the previous fence will be thrown away and will never be signalled. keep iterating regardless of signal on metal.
                    if (device.SurfaceType != GraphicsSurfaceType.Metal)
                        Debug.Assert(lastSignalledFenceIndex == null, "A non-signalled fence was detected before the latest signalled frame.");

                    continue;
                }

                lastSignalledFenceIndex ??= i;

                Device.ResetFence(fence.Fence);
                perFrameFencePool.Enqueue(fence.Fence);
            }

            if (lastSignalledFenceIndex != null)
            {
                ulong completedFrameIndex = pendingFramesFences[lastSignalledFenceIndex.Value].FrameIndex;

                Debug.Assert(completedFrameIndex > LatestCompletedExecutionIndex);
                LatestCompletedExecutionIndex = completedFrameIndex;

                pendingFramesFences.RemoveRange(0, lastSignalledFenceIndex.Value + 1);
            }

            Debug.Assert(pendingFramesFences.Count < 16, "Completion frame fence leak detected");
        }

        private record struct FrameCompletionFence(Fence Fence, ulong FrameIndex);
    }
}
