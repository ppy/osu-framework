// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL.Textures;
using OpenTK;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public class FrameBuffer : IDisposable
    {
        private int lastFramebuffer;
        private int frameBuffer = -1;

        public TextureGL Texture { get; private set; }

        private bool isBound => lastFramebuffer != -1;

        private readonly List<RenderBuffer> attachedRenderBuffers = new List<RenderBuffer>();

        #region Disposal

        ~FrameBuffer()
        {
            Dispose(false);
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
            isDisposed = true;

            GLWrapper.ScheduleDisposal(delegate
            {
                Unbind();
                GLWrapper.DeleteFramebuffer(frameBuffer);
                frameBuffer = -1;
            });
        }

        #endregion

        public bool IsInitialized { get; private set; }

        public void Initialize(bool withTexture = true, All filteringMode = All.Linear)
        {
            frameBuffer = GL.GenFramebuffer();

            if (withTexture)
            {
                Texture = new TextureGLSingle(1, 1, true, filteringMode);
                Texture.SetData(new TextureUpload(Array.Empty<byte>()));
                Texture.Upload();

                Bind();

                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, Texture.TextureId, 0);
                GLWrapper.BindTexture(null);

                Unbind();
            }

            IsInitialized = true;
        }

        private Vector2 size = Vector2.One;

        /// <summary>
        /// Sets the size of the texture of this framebuffer.
        /// </summary>
        public Vector2 Size
        {
            get { return size; }
            set
            {
                if (value == size)
                    return;
                size = value;

                Texture.Width = (int)Math.Ceiling(size.X);
                Texture.Height = (int)Math.Ceiling(size.Y);
                Texture.SetData(new TextureUpload(Array.Empty<byte>()));
                Texture.Upload();
            }
        }

        /// <summary>
        /// Attaches a RenderBuffer to this framebuffer.
        /// </summary>
        /// <param name="format">The type of RenderBuffer to attach.</param>
        public void Attach(RenderbufferInternalFormat format)
        {
            if (attachedRenderBuffers.Exists(r => r.Format == format))
                return;

            attachedRenderBuffers.Add(new RenderBuffer(format));
        }

        /// <summary>
        /// Binds the framebuffer.
        /// <para>Does not clear the buffer or reset the viewport/ortho.</para>
        /// </summary>
        public void Bind()
        {
            if (frameBuffer == -1)
                return;

            if (lastFramebuffer == frameBuffer)
                return;

            // Bind framebuffer and all its renderbuffers
            lastFramebuffer = GLWrapper.BindFrameBuffer(frameBuffer);
            foreach (var r in attachedRenderBuffers)
            {
                r.Size = Size;
                r.Bind(frameBuffer);
            }
        }

        /// <summary>
        /// Unbinds the framebuffer.
        /// </summary>
        public void Unbind()
        {
            if (!isBound)
                return;

            GLWrapper.BindFrameBuffer(lastFramebuffer);
            foreach (var r in attachedRenderBuffers)
                r.Unbind();

            lastFramebuffer = -1;
        }
    }
}
