// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public class FrameBuffer : IDisposable
    {
        private int frameBuffer = -1;

        internal TextureGLSingle Texture { get; private set; }

        private readonly List<RenderBuffer> attachedRenderBuffers = new List<RenderBuffer>();

        private RenderbufferInternalFormat[] renderBufferFormats;

        private All filteringMode;

        public void Initialise(All filteringMode = All.Linear, RenderbufferInternalFormat[] renderBufferFormats = null)
        {
            this.renderBufferFormats = renderBufferFormats;
            this.filteringMode = filteringMode;
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

                int requestedWidth = (int)Math.Ceiling(size.X);
                int requestedHeight = (int)Math.Ceiling(size.Y);

                if (!IsInitialized)
                {
                    Debug.Assert(Texture == null);
                    Debug.Assert(frameBuffer == -1);

                    IsInitialized = true;

                    frameBuffer = GL.GenFramebuffer();

                    Bind();

                    Texture = FrameBufferTextureCache.Get(requestedWidth, requestedHeight, filteringMode);
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, Texture.TextureId, 0);
                    GLWrapper.BindTexture(null);

                    if (renderBufferFormats != null)
                    {
                        foreach (var format in renderBufferFormats)
                            attachedRenderBuffers.Add(new RenderBuffer(format));
                    }

                    Unbind();
                }

                if (Texture.Width < requestedWidth || Texture.Height < requestedHeight)
                {
                    Texture.Width = Math.Max(Texture.Width, requestedWidth);
                    Texture.Height = Math.Max(Texture.Height, requestedHeight);
                    Texture.SetData(new TextureUpload());
                    Texture.Upload();
                }

                foreach (var buffer in attachedRenderBuffers)
                    buffer.Size = value;
            }
        }

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Binds the framebuffer.
        /// <para>Does not clear the buffer or reset the viewport/ortho.</para>
        /// </summary>
        public void Bind() => GLWrapper.BindFrameBuffer(frameBuffer);

        /// <summary>
        /// Unbinds the framebuffer.
        /// </summary>
        public void Unbind() => GLWrapper.UnbindFrameBuffer(frameBuffer);

        #region Disposal

        ~FrameBuffer()
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

            Texture?.Dispose();
            Texture = null;

            GLWrapper.DeleteFrameBuffer(frameBuffer);

            foreach (var buffer in attachedRenderBuffers)
                buffer.Dispose();

            if (Texture != null)
            {
                FrameBufferTextureCache.Return(Texture);
                Texture = null;
            }

            isDisposed = true;
        }

        #endregion
    }
}
