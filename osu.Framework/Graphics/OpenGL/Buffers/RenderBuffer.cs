// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class RenderBuffer : IDisposable
    {
        private readonly RenderbufferInternalFormat format;
        private readonly int renderBuffer;

        private readonly int sizePerPixel;

        public RenderBuffer(RenderbufferInternalFormat format)
        {
            this.format = format;

            renderBuffer = GL.GenRenderbuffer();

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);

            // OpenGL docs don't specify that this is required, but seems to be required on some platforms
            // to correctly attach in the GL.FramebufferRenderbuffer() call below
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, 1, 1);

            switch (format)
            {
                case RenderbufferInternalFormat.DepthComponent16:
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, renderBuffer);
                    sizePerPixel = 2;
                    break;

                case RenderbufferInternalFormat.Rgb565:
                case RenderbufferInternalFormat.Rgb5A1:
                case RenderbufferInternalFormat.Rgba4:
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, renderBuffer);
                    sizePerPixel = 2;
                    break;

                case RenderbufferInternalFormat.StencilIndex8:
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, renderBuffer);
                    sizePerPixel = 1;
                    break;
            }
        }

        private Vector2 size;

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        internal Vector2 Size
        {
            get => size;
            set
            {
                if (value.X <= size.X && value.Y <= size.Y)
                    return;

                memoryLease?.Dispose();

                size = value;

                memoryLease = NativeMemoryTracker.AddMemory(this, (int)(size.X * size.Y * sizePerPixel));

                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, (int)Math.Ceiling(Size.X), (int)Math.Ceiling(Size.Y));
            }
        }

        #region Disposal

        ~RenderBuffer()
        {
            GLWrapper.ScheduleDisposal(() => Dispose(false));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (renderBuffer != -1)
            {
                memoryLease?.Dispose();
                GL.DeleteRenderbuffer(renderBuffer);
            }

            isDisposed = true;
        }

        #endregion
    }
}
