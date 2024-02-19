// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Veldrid.Textures;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Pipelines
{
    /// <summary>
    /// A non-graphical pipeline that provides a command list and handles basic tasks like uploading textures.
    /// </summary>
    internal class BasicPipeline : IBasicPipeline
    {
        /// <summary>
        /// The platform graphics device.
        /// </summary>
        public GraphicsDevice Device
            => device.Device;

        /// <summary>
        /// The platform graphics resource factory.
        /// </summary>
        public ResourceFactory Factory
            => device.Factory;

        /// <summary>
        /// The command list.
        /// </summary>
        public readonly CommandList Commands;

        private readonly VeldridDevice device;

        public BasicPipeline(VeldridDevice device)
        {
            this.device = device;
            Commands = device.Factory.CreateCommandList();
        }

        /// <summary>
        /// Begins the pipeline.
        /// </summary>
        public virtual void Begin()
        {
            Commands.Begin();
        }

        /// <summary>
        /// Finishes the pipeline and submits it for processing by the device.
        /// </summary>
        public void End(Fence? fence = null)
        {
            Commands.End();
            device.Device.SubmitCommands(Commands, fence);
        }

        public void UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data)
            where T : unmanaged
        {
            // This code is doing the same as the simpler approach of:
            //
            // Device.UpdateTexture(texture, data, (uint)x, (uint)y, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            //
            // Except we are using a staging texture pool to avoid the alloc overhead of each staging texture.
            var staging = device.StagingTexturePool.Get(width, height, texture.Format);
            device.Device.UpdateTexture(staging, data, 0, 0, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            Commands.CopyTexture(staging, 0, 0, 0, 0, 0, texture, (uint)x, (uint)y, 0, (uint)level, 0, (uint)width, (uint)height, 1, 1);
        }

        public void UpdateTexture(Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes)
        {
            var staging = device.StagingTexturePool.Get(width, height, texture.Format);

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

        public void GenerateMipmaps(VeldridTexture texture)
        {
            var resources = texture.GetResourceList();
            for (int i = 0; i < resources.Count; i++)
                Commands.GenerateMipmaps(resources[i].Texture);
        }
    }
}
