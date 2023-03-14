// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Platform;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal unsafe class VeldridVideoTexture : VeldridTexture
    {
        private VeldridTextureResources[]? resourceList;

        public VeldridVideoTexture(VeldridRenderer renderer, int width, int height)
            : base(renderer, width, height, true)
        {
        }

        private NativeMemoryTracker.NativeMemoryLease? memoryLease;

        private int textureSize;

        public override int GetByteSize() => textureSize;

        protected override void DoUpload(ITextureUpload upload)
        {
            if (!(upload is VideoTextureUpload videoUpload))
                return;

            // Do we need to generate a new texture?
            if (resourceList == null)
            {
                Debug.Assert(memoryLease == null);
                memoryLease = NativeMemoryTracker.AddMemory(this, Width * Height * 3 / 2);
                resourceList = new VeldridTextureResources[3];

                for (uint i = 0; i < resourceList.Length; i++)
                {
                    int width = videoUpload.GetPlaneWidth(i);
                    int height = videoUpload.GetPlaneHeight(i);
                    int countPixels = width * height;

                    resourceList[i] = new VeldridTextureResources
                    (
                        Renderer.Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_UNorm, Usages)),
                        Renderer.Factory.CreateSampler(new SamplerDescription
                        {
                            AddressModeU = SamplerAddressMode.Clamp,
                            AddressModeV = SamplerAddressMode.Clamp,
                            AddressModeW = SamplerAddressMode.Clamp,
                            Filter = SamplerFilter.MinLinear_MagLinear_MipLinear,
                            MinimumLod = 0,
                            MaximumLod = IRenderer.MAX_MIPMAP_LEVELS,
                            MaximumAnisotropy = 0,
                        })
                    );

                    textureSize += countPixels;
                }
            }

            for (uint i = 0; i < resourceList.Length; i++)
            {
                Renderer.UpdateTexture(
                    resourceList[i].Texture,
                    0,
                    0,
                    videoUpload.GetPlaneWidth(i),
                    videoUpload.GetPlaneHeight(i),
                    0,
                    new IntPtr(videoUpload.Frame->data[i]),
                    videoUpload.Frame->linesize[i]);
            }
        }

        public override IEnumerable<VeldridTextureResources> GetResourceList() => resourceList.AsNonNull();

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            Renderer.ScheduleDisposal(texture =>
            {
                texture.memoryLease?.Dispose();

                if (texture.resourceList != null)
                {
                    foreach (var res in texture.resourceList)
                        res.Dispose();
                }

                texture.resourceList = null;
            }, this);
        }

        #endregion
    }
}
