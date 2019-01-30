// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public class FrameBuffer : IDisposable
    {
        private int frameBuffer = -1;

        public TextureGL Texture { get; private set; }

        private readonly List<RenderBuffer> attachedRenderBuffers = new List<RenderBuffer>();

        #region Disposal

        ~FrameBuffer() => Dispose(false);

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
                Texture.SetData(new TextureUpload());
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

                Texture.Width = (int)Math.Ceiling(size.X);
                Texture.Height = (int)Math.Ceiling(size.Y);
                Texture.SetData(new TextureUpload());
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
            GLWrapper.BindFrameBuffer(frameBuffer);
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
            GLWrapper.UnbindFrameBuffer(frameBuffer);
            foreach (var r in attachedRenderBuffers)
                r.Unbind();
        }
    }
}
