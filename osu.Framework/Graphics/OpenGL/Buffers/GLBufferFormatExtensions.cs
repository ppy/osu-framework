// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public static class GLBufferFormatExtensions
    {
        public static FramebufferAttachment GetAttachmentType(this RenderbufferInternalFormat format)
        {
            switch (format)
            {
                case RenderbufferInternalFormat.R8:
                case RenderbufferInternalFormat.R8Snorm:
                case RenderbufferInternalFormat.R16f:
                case RenderbufferInternalFormat.R32f:
                case RenderbufferInternalFormat.R8ui:
                case RenderbufferInternalFormat.R8i:
                case RenderbufferInternalFormat.R16ui:
                case RenderbufferInternalFormat.R16i:
                case RenderbufferInternalFormat.R32ui:
                case RenderbufferInternalFormat.R32i:
                case RenderbufferInternalFormat.Rg8:
                case RenderbufferInternalFormat.Rg8Snorm:
                case RenderbufferInternalFormat.Rg16f:
                case RenderbufferInternalFormat.Rg32f:
                case RenderbufferInternalFormat.Rg8ui:
                case RenderbufferInternalFormat.Rg8i:
                case RenderbufferInternalFormat.Rg16ui:
                case RenderbufferInternalFormat.Rg16i:
                case RenderbufferInternalFormat.Rg32ui:
                case RenderbufferInternalFormat.Rg32i:
                case RenderbufferInternalFormat.Rgb8:
                case RenderbufferInternalFormat.Srgb8:
                case RenderbufferInternalFormat.Rgb565:
                case RenderbufferInternalFormat.Rgb8Snorm:
                case RenderbufferInternalFormat.R11fG11fB10f:
                case RenderbufferInternalFormat.Rgb9E5:
                case RenderbufferInternalFormat.Rgb16f:
                case RenderbufferInternalFormat.Rgb32f:
                case RenderbufferInternalFormat.Rgb8ui:
                case RenderbufferInternalFormat.Rgb8i:
                case RenderbufferInternalFormat.Rgb16ui:
                case RenderbufferInternalFormat.Rgb16i:
                case RenderbufferInternalFormat.Rgb32ui:
                case RenderbufferInternalFormat.Rgb32i:
                case RenderbufferInternalFormat.Rgba8:
                case RenderbufferInternalFormat.Srgb8Alpha8:
                case RenderbufferInternalFormat.Rgba8Snorm:
                case RenderbufferInternalFormat.Rgb5A1:
                case RenderbufferInternalFormat.Rgba4:
                case RenderbufferInternalFormat.Rgb10A2:
                case RenderbufferInternalFormat.Rgba16f:
                case RenderbufferInternalFormat.Rgba32f:
                case RenderbufferInternalFormat.Rgba8i:
                case RenderbufferInternalFormat.Rgba8ui:
                case RenderbufferInternalFormat.Rgb10A2ui:
                case RenderbufferInternalFormat.Rgba16i:
                case RenderbufferInternalFormat.Rgba16ui:
                case RenderbufferInternalFormat.Rgba32i:
                case RenderbufferInternalFormat.Rgba32ui:
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
            // cross-reference: https://www.khronos.org/registry/OpenGL-Refpages/es3.0/html/glTexImage2D.xhtml
            switch (format)
            {
                // GL_RED
                case RenderbufferInternalFormat.R8:
                case RenderbufferInternalFormat.R8Snorm:
                    return 1;

                case RenderbufferInternalFormat.R16f:
                    return 2;

                case RenderbufferInternalFormat.R32f:
                    return 4;

                // GL_RED_INTEGER
                case RenderbufferInternalFormat.R8ui:
                case RenderbufferInternalFormat.R8i:
                    return 1;

                case RenderbufferInternalFormat.R16ui:
                case RenderbufferInternalFormat.R16i:
                    return 2;

                case RenderbufferInternalFormat.R32ui:
                case RenderbufferInternalFormat.R32i:
                    return 4;

                // GL_RG
                case RenderbufferInternalFormat.Rg8:
                case RenderbufferInternalFormat.Rg8Snorm:
                    return 2;

                case RenderbufferInternalFormat.Rg16f:
                    return 4;

                case RenderbufferInternalFormat.Rg32f:
                    return 8;

                // GL_RG_INTEGER
                case RenderbufferInternalFormat.Rg8ui:
                case RenderbufferInternalFormat.Rg8i:
                    return 2;

                case RenderbufferInternalFormat.Rg16ui:
                case RenderbufferInternalFormat.Rg16i:
                    return 4;

                case RenderbufferInternalFormat.Rg32ui:
                case RenderbufferInternalFormat.Rg32i:
                    return 8;

                // GL_RGB
                case RenderbufferInternalFormat.Rgb8:
                case RenderbufferInternalFormat.Srgb8:
                    return 3;

                case RenderbufferInternalFormat.Rgb565:
                    return 2;

                case RenderbufferInternalFormat.Rgb8Snorm:
                    return 3;

                case RenderbufferInternalFormat.R11fG11fB10f:
                case RenderbufferInternalFormat.Rgb9E5:
                    return 4;

                case RenderbufferInternalFormat.Rgb16f:
                    return 6;

                case RenderbufferInternalFormat.Rgb32f:
                    return 12;

                // GL_RGB_INTEGER
                case RenderbufferInternalFormat.Rgb8ui:
                case RenderbufferInternalFormat.Rgb8i:
                    return 3;

                case RenderbufferInternalFormat.Rgb16ui:
                case RenderbufferInternalFormat.Rgb16i:
                    return 6;

                case RenderbufferInternalFormat.Rgb32ui:
                case RenderbufferInternalFormat.Rgb32i:
                    return 12;

                // GL_RGBA
                case RenderbufferInternalFormat.Rgba8:
                case RenderbufferInternalFormat.Srgb8Alpha8:
                case RenderbufferInternalFormat.Rgba8Snorm:
                    return 4;

                case RenderbufferInternalFormat.Rgb5A1:
                case RenderbufferInternalFormat.Rgba4:
                    return 2;

                case RenderbufferInternalFormat.Rgb10A2:
                    return 4;

                case RenderbufferInternalFormat.Rgba16f:
                    return 8;

                case RenderbufferInternalFormat.Rgba32f:
                    return 16;

                // GL_RGBA_INTEGER
                case RenderbufferInternalFormat.Rgba8i:
                case RenderbufferInternalFormat.Rgba8ui:
                case RenderbufferInternalFormat.Rgb10A2ui:
                    return 4;

                case RenderbufferInternalFormat.Rgba16i:
                case RenderbufferInternalFormat.Rgba16ui:
                    return 8;

                case RenderbufferInternalFormat.Rgba32i:
                case RenderbufferInternalFormat.Rgba32ui:
                    return 16;

                // GL_DEPTH_COMPONENT
                case RenderbufferInternalFormat.DepthComponent16:
                    return 2;

                case RenderbufferInternalFormat.DepthComponent24:
                    return 3;

                case RenderbufferInternalFormat.DepthComponent32f:
                    return 4;

                // GL_DEPTH_STENCIL
                case RenderbufferInternalFormat.Depth24Stencil8:
                    return 4;

                case RenderbufferInternalFormat.Depth32fStencil8:
                    return 5;

                case RenderbufferInternalFormat.StencilIndex8:
                    return 1;

                default:
                    throw new InvalidOperationException($"{format} is not a valid {nameof(RenderbufferInternalFormat)} type.");
            }
        }
    }
}
