// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public class RenderBuffer : IDisposable
    {
        public Vector2 Size = Vector2.One;
        public RenderbufferInternalFormat Format { get; }

        private int renderBuffer = -1;

        public RenderBuffer(RenderbufferInternalFormat format)
        {
            Format = format;
        }

        /// <summary>
        /// Binds the renderbuffer to the specfied framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer this renderbuffer should be bound to.</param>
        internal void Bind(int frameBuffer)
        {
            if (renderBuffer == -1)
                renderBuffer = GL.GenRenderbuffer();

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, Format, (int)Math.Ceiling(Size.X), (int)Math.Ceiling(Size.Y));

            // Make sure the framebuffer we want to attach to is bound
            GLWrapper.BindFrameBuffer(frameBuffer);

            switch (Format)
            {
                case RenderbufferInternalFormat.DepthComponent16:
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, renderBuffer);
                    break;

                case RenderbufferInternalFormat.Rgb565:
                case RenderbufferInternalFormat.Rgb5A1:
                case RenderbufferInternalFormat.Rgba4:
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, renderBuffer);
                    break;

                case RenderbufferInternalFormat.StencilIndex8:
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, renderBuffer);
                    break;
            }

            GLWrapper.UnbindFrameBuffer(frameBuffer);
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
                GL.DeleteRenderbuffer(renderBuffer);

            isDisposed = true;
        }

        #endregion
    }
}
