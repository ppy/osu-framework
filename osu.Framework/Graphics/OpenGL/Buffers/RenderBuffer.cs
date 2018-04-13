// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public class RenderBuffer : IDisposable
    {
        private static readonly Dictionary<RenderbufferInternalFormat, Stack<RenderBufferInfo>> render_buffer_cache = new Dictionary<RenderbufferInternalFormat, Stack<RenderBufferInfo>>();

        public Vector2 Size = Vector2.One;
        public RenderbufferInternalFormat Format { get; }

        private RenderBufferInfo info;
        private bool isDisposed;

        public RenderBuffer(RenderbufferInternalFormat format)
        {
            Format = format;
        }

        #region Disposal

        ~RenderBuffer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            isDisposed = true;

            Unbind();
        }

        #endregion

        /// <summary>
        /// Binds the renderbuffer to the specfied framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer this renderbuffer should be bound to.</param>
        internal void Bind(int frameBuffer)
        {
            // Check if we're already bound
            if (info != null)
                return;

            if (!render_buffer_cache.ContainsKey(Format))
                render_buffer_cache[Format] = new Stack<RenderBufferInfo>();

            // Make sure we have renderbuffers available
            if (render_buffer_cache[Format].Count == 0)
                render_buffer_cache[Format].Push(new RenderBufferInfo
                {
                    RenderBufferID = GL.GenRenderbuffer(),
                    FrameBufferID = -1
                });

            // Get a renderbuffer from the cache
            info = render_buffer_cache[Format].Pop();

            // Check if we need to update the size
            if (info.Size != Size)
            {
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, info.RenderBufferID);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, Format, (int)Math.Ceiling(Size.X), (int)Math.Ceiling(Size.Y));

                info.Size = Size;
            }

            // For performance reasons, we only need to re-bind the renderbuffer to
            // the framebuffer if it is not already attached to it
            if (info.FrameBufferID != frameBuffer)
            {
                // Make sure the framebuffer we want to attach to is bound
                int lastFrameBuffer = GLWrapper.BindFrameBuffer(frameBuffer);

                switch (Format)
                {
                    case RenderbufferInternalFormat.DepthComponent16:
                        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, info.RenderBufferID);
                        break;
                    case RenderbufferInternalFormat.Rgb565:
                    case RenderbufferInternalFormat.Rgb5A1:
                    case RenderbufferInternalFormat.Rgba4:
                        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, info.RenderBufferID);
                        break;
                    case RenderbufferInternalFormat.StencilIndex8:
                        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, info.RenderBufferID);
                        break;
                }

                GLWrapper.BindFrameBuffer(lastFrameBuffer);
            }

            info.FrameBufferID = frameBuffer;
        }

        /// <summary>
        /// Unbinds the renderbuffer.
        /// <para>The renderbuffer will remain internally attached to the framebuffer.</para>
        /// </summary>
        internal void Unbind()
        {
            if (info == null)
                return;

            // Return the renderbuffer to the cache
            render_buffer_cache[Format].Push(info);

            info = null;
        }

        private class RenderBufferInfo
        {
            public int RenderBufferID;
            public int FrameBufferID;
            public Vector2 Size;
        }
    }
}
