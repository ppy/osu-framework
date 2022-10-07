// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLFrameBuffer : IFrameBuffer
    {
        public Texture Texture { get; }

        private readonly List<GLRenderBuffer> attachedRenderBuffers = new List<GLRenderBuffer>();
        private readonly GLRenderer renderer;
        private readonly GLTexture glTexture;

        public readonly int FrameBuffer;

        public GLFrameBuffer(GLRenderer renderer, RenderbufferInternalFormat[]? renderBufferFormats = null, All filteringMode = All.Linear)
        {
            this.renderer = renderer;
            FrameBuffer = GL.GenFramebuffer();

            Texture = renderer.CreateTexture(glTexture = new FrameBufferTexture(renderer, filteringMode));

            renderer.BindFrameBuffer(this);

            Debug.Assert(glTexture.TextureId != 0);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, glTexture.TextureId, 0);
            renderer.UnbindTexture();

            if (renderBufferFormats != null)
            {
                foreach (var format in renderBufferFormats)
                    attachedRenderBuffers.Add(new GLRenderBuffer(renderer, format));
            }

            renderer.UnbindFrameBuffer(this);
        }

        private Vector2 size = Vector2.One;

        /// <summary>
        /// Sets the size of the texture of this frame buffer.
        /// </summary>
        public Vector2 Size
        {
            get => size;
            set
            {
                if (value == size)
                    return;

                size = value;

                glTexture.Width = (int)Math.Ceiling(size.X);
                glTexture.Height = (int)Math.Ceiling(size.Y);
                glTexture.SetData(new TextureUpload());
                glTexture.Upload();
            }
        }

        /// <summary>
        /// Binds the framebuffer.
        /// <para>Does not clear the buffer or reset the viewport/ortho.</para>
        /// </summary>
        public void Bind()
        {
            renderer.BindFrameBuffer(this);

            foreach (var buffer in attachedRenderBuffers)
                buffer.Bind(Size);
        }

        /// <summary>
        /// Unbinds the framebuffer.
        /// </summary>
        public void Unbind()
        {
            // See: https://community.arm.com/developer/tools-software/graphics/b/blog/posts/mali-performance-2-how-to-correctly-handle-framebuffers
            // Unbinding renderbuffers causes an invalidation of the relevant attachment of this framebuffer on embedded devices, causing the renderbuffers to remain transient.
            // This must be done _before_ the framebuffer is flushed via the framebuffer unbind process, otherwise the renderbuffer may be copied to system memory.
            foreach (var buffer in attachedRenderBuffers)
                buffer.Unbind();

            renderer.UnbindFrameBuffer(this);
        }

        #region Disposal

        ~GLFrameBuffer()
        {
            renderer.ScheduleDisposal(b => b.Dispose(false), this);
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

            glTexture.Dispose();
            renderer.DeleteFrameBuffer(this);

            foreach (var buffer in attachedRenderBuffers)
                buffer.Dispose();

            isDisposed = true;
        }

        #endregion

        private class FrameBufferTexture : GLTexture
        {
            public FrameBufferTexture(GLRenderer renderer, All filteringMode = All.Linear)
                : base(renderer, 1, 1, true, filteringMode)
            {
                BypassTextureUploadQueueing = true;

                SetData(new TextureUpload());
                Upload();
            }

            public override int Width
            {
                get => base.Width;
                set => base.Width = Math.Clamp(value, 1, Renderer.MaxTextureSize);
            }

            public override int Height
            {
                get => base.Height;
                set => base.Height = Math.Clamp(value, 1, Renderer.MaxTextureSize);
            }
        }
    }
}
