// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public static class BufferFormatExtensions
    {
        public static FramebufferAttachment GetAttachmentType(this RenderbufferInternalFormat format)
        {
            switch (format)
            {
                case RenderbufferInternalFormat.Rgb8:
                case RenderbufferInternalFormat.Rgba4:
                case RenderbufferInternalFormat.Rgb5A1:
                case RenderbufferInternalFormat.Rgba8:
                case RenderbufferInternalFormat.Rgb10A2:
                case RenderbufferInternalFormat.R8:
                case RenderbufferInternalFormat.Rg8:
                case RenderbufferInternalFormat.R16f:
                case RenderbufferInternalFormat.R32f:
                case RenderbufferInternalFormat.Rg16f:
                case RenderbufferInternalFormat.Rg32f:
                case RenderbufferInternalFormat.R8i:
                case RenderbufferInternalFormat.R8ui:
                case RenderbufferInternalFormat.R16i:
                case RenderbufferInternalFormat.R16ui:
                case RenderbufferInternalFormat.R32i:
                case RenderbufferInternalFormat.R32ui:
                case RenderbufferInternalFormat.Rg8i:
                case RenderbufferInternalFormat.Rg8ui:
                case RenderbufferInternalFormat.Rg16i:
                case RenderbufferInternalFormat.Rg16ui:
                case RenderbufferInternalFormat.Rg32i:
                case RenderbufferInternalFormat.Rg32ui:
                case RenderbufferInternalFormat.Rgba32f:
                case RenderbufferInternalFormat.Rgb32f:
                case RenderbufferInternalFormat.Rgba16f:
                case RenderbufferInternalFormat.Rgb16f:
                case RenderbufferInternalFormat.R11fG11fB10f:
                case RenderbufferInternalFormat.Rgb9E5:
                case RenderbufferInternalFormat.Srgb8:
                case RenderbufferInternalFormat.Srgb8Alpha8:
                case RenderbufferInternalFormat.Rgb565:
                case RenderbufferInternalFormat.Rgba32ui:
                case RenderbufferInternalFormat.Rgb32ui:
                case RenderbufferInternalFormat.Rgba16ui:
                case RenderbufferInternalFormat.Rgb16ui:
                case RenderbufferInternalFormat.Rgba8ui:
                case RenderbufferInternalFormat.Rgb8ui:
                case RenderbufferInternalFormat.Rgba32i:
                case RenderbufferInternalFormat.Rgb32i:
                case RenderbufferInternalFormat.Rgba16i:
                case RenderbufferInternalFormat.Rgb16i:
                case RenderbufferInternalFormat.Rgba8i:
                case RenderbufferInternalFormat.Rgb8i:
                case RenderbufferInternalFormat.R8Snorm:
                case RenderbufferInternalFormat.Rg8Snorm:
                case RenderbufferInternalFormat.Rgb8Snorm:
                case RenderbufferInternalFormat.Rgba8Snorm:
                case RenderbufferInternalFormat.Rgb10A2ui:
                    return FramebufferAttachment.ColorAttachment0;

                case RenderbufferInternalFormat.DepthComponent16:
                case RenderbufferInternalFormat.DepthComponent24:
                case RenderbufferInternalFormat.DepthComponent32f:
                    return FramebufferAttachment.DepthAttachment;

                case RenderbufferInternalFormat.StencilIndex8:
                    return FramebufferAttachment.StencilAttachment;

                case RenderbufferInternalFormat.Depth24Stencil8:
                case RenderbufferInternalFormat.Depth32fStencil8:
                    return FramebufferAttachment.DepthStencilAttachment;

                default:
                    throw new InvalidOperationException($"{format} is not a valid {nameof(RenderbufferInternalFormat)} type.");
            }
        }

        public static int GetBytesPerPixel(this RenderbufferInternalFormat format)
        {
            return format switch
            {
                RenderbufferInternalFormat.Rgb8 => 3,
                RenderbufferInternalFormat.Rgba4 => 2,
                RenderbufferInternalFormat.Rgb5A1 => 2,
                RenderbufferInternalFormat.Rgba8 => 4,
                RenderbufferInternalFormat.Rgb10A2 => 4,
                RenderbufferInternalFormat.DepthComponent16 => 2,
                RenderbufferInternalFormat.DepthComponent24 => 3,
                RenderbufferInternalFormat.R8 => 1,
                RenderbufferInternalFormat.Rg8 => 2,
                RenderbufferInternalFormat.R16f => 2,
                RenderbufferInternalFormat.R32f => 4,
                RenderbufferInternalFormat.Rg16f => 4,
                RenderbufferInternalFormat.Rg32f => 8,
                RenderbufferInternalFormat.R8i => 1,
                RenderbufferInternalFormat.R8ui => 1,
                RenderbufferInternalFormat.R16i => 2,
                RenderbufferInternalFormat.R16ui => 2,
                RenderbufferInternalFormat.R32i => 4,
                RenderbufferInternalFormat.R32ui => 4,
                RenderbufferInternalFormat.Rg8i => 2,
                RenderbufferInternalFormat.Rg8ui => 2,
                RenderbufferInternalFormat.Rg16i => 4,
                RenderbufferInternalFormat.Rg16ui => 4,
                RenderbufferInternalFormat.Rg32i => 8,
                RenderbufferInternalFormat.Rg32ui => 8,
                RenderbufferInternalFormat.Rgba32f => 16,
                RenderbufferInternalFormat.Rgb32f => 12,
                RenderbufferInternalFormat.Rgba16f => 8,
                RenderbufferInternalFormat.Rgb16f => 6,
                RenderbufferInternalFormat.Depth24Stencil8 => 4,
                RenderbufferInternalFormat.R11fG11fB10f => 4,
                RenderbufferInternalFormat.Rgb9E5 => 4,
                RenderbufferInternalFormat.Srgb8 => 3,
                RenderbufferInternalFormat.Srgb8Alpha8 => 4,
                RenderbufferInternalFormat.DepthComponent32f => 4,
                RenderbufferInternalFormat.Depth32fStencil8 => 5,
                RenderbufferInternalFormat.StencilIndex8 => 1,
                RenderbufferInternalFormat.Rgb565 => 2,
                RenderbufferInternalFormat.Rgba32ui => 16,
                RenderbufferInternalFormat.Rgb32ui => 12,
                RenderbufferInternalFormat.Rgba16ui => 8,
                RenderbufferInternalFormat.Rgb16ui => 6,
                RenderbufferInternalFormat.Rgba8ui => 4,
                RenderbufferInternalFormat.Rgb8ui => 3,
                RenderbufferInternalFormat.Rgba32i => 16,
                RenderbufferInternalFormat.Rgb32i => 12,
                RenderbufferInternalFormat.Rgba16i => 8,
                RenderbufferInternalFormat.Rgb16i => 6,
                RenderbufferInternalFormat.Rgba8i => 4,
                RenderbufferInternalFormat.Rgb8i => 3,
                RenderbufferInternalFormat.R8Snorm => 1,
                RenderbufferInternalFormat.Rg8Snorm => 2,
                RenderbufferInternalFormat.Rgb8Snorm => 3,
                RenderbufferInternalFormat.Rgba8Snorm => 4,
                RenderbufferInternalFormat.Rgb10A2ui => 4,

                _ => throw new InvalidOperationException($"{format} is not a valid {nameof(RenderbufferInternalFormat)} type.")
            };
        }
    }
}
