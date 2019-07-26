// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class RenderBuffer : IDisposable
    {
        private readonly RenderbufferInternalFormat format;
        private readonly int renderBuffer;

        private static readonly GlobalStatistic<long> loaded_bytes = GlobalStatistics.Get<long>("Native", nameof(RenderBuffer));

        private readonly int sizePerPixel;

        public RenderBuffer(RenderbufferInternalFormat format)
        {
            this.format = format;

            renderBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);

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

        private int sizeInMemory => (int)(size.X * size.Y * sizePerPixel);

        internal Vector2 Size
        {
            get => size;
            set
            {
                if (value.X <= size.X && value.Y <= size.Y)
                    return;

                loaded_bytes.Value -= sizeInMemory;

                size = value;

                loaded_bytes.Value += sizeInMemory;

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
                loaded_bytes.Value -= sizeInMemory;
                GL.DeleteRenderbuffer(renderBuffer);
            }

            isDisposed = true;
        }

        #endregion
    }
}
