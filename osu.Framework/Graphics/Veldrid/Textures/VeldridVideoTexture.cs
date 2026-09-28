// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Platform;
using Veldrid;
using PixelFormat = Veldrid.PixelFormat;
using VdTexture = Veldrid.Texture;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal unsafe class VeldridVideoTexture : VeldridTexture
    {
        private const int plane_count = 3;

        private readonly Sampler?[] samplers = new Sampler[plane_count];
        private readonly VdTexture?[] textures = new VdTexture[plane_count];
        private readonly ResourceSet?[] resources = new ResourceSet[plane_count];

        public VeldridVideoTexture(IVeldridRenderer renderer, int width, int height)
            : base(renderer, width, height, manualMipmaps: true)
        {
        }

        private NativeMemoryTracker.NativeMemoryLease? memoryLease;

        private int textureSize;

        public override int GetByteSize() => textureSize;

        protected override void DoUpload(ITextureUpload upload)
        {
            if (upload is not VideoTextureUpload videoUpload)
                return;

            memoryLease ??= NativeMemoryTracker.AddMemory(this, Width * Height * 3 / 2);

            for (uint i = 0; i < plane_count; i++)
            {
                if (textures[i] == null)
                {
                    int width = videoUpload.GetPlaneWidth(i);
                    int height = videoUpload.GetPlaneHeight(i);
                    int countPixels = width * height;

                    textures[i] = Renderer.Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8UNorm, Usages));
                    samplers[i] = Renderer.Factory.CreateSampler(new SamplerDescription
                    {
                        AddressModeU = SamplerAddressMode.Clamp,
                        AddressModeV = SamplerAddressMode.Clamp,
                        AddressModeW = SamplerAddressMode.Clamp,
                        Filter = SamplerFilter.MinLinearMagLinearMipLinear,
                        MinimumLod = 0,
                        MaximumLod = IRenderer.MAX_MIPMAP_LEVELS,
                        MaximumAnisotropy = 0,
                    });

                    textureSize += countPixels;
                }

                Debug.Assert(textures[i] != null);

                Renderer.UpdateTexture(
                    textures[i].AsNonNull(),
                    0,
                    0,
                    videoUpload.GetPlaneWidth(i),
                    videoUpload.GetPlaneHeight(i),
                    0,
                    new IntPtr(videoUpload.Frame->data[i]),
                    videoUpload.Frame->linesize[i]);
            }
        }

        public override int ResourceCount => resources.Length;

        public override ResourceSet? GetResourceSet(int index, ResourceLayout layout)
        {
            if (textures[index] == null || samplers[index] == null)
                return null;

            return resources[index] ??= Renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, textures[index], samplers[index]));
        }

        #region Disposal

        private bool isDisposed;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (isDisposed)
                return;

            isDisposed = true;

            memoryLease?.Dispose();

            Renderer.ScheduleDisposal(textures.Cast<IDisposable>().ToArray());
            Renderer.ScheduleDisposal(samplers.Cast<IDisposable>().ToArray());
            Renderer.ScheduleDisposal(resources.Cast<IDisposable>().ToArray());
        }

        #endregion
    }
}
