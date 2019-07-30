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

        private bool isInitialised;

        private readonly All filteringMode;
        private readonly RenderbufferInternalFormat[] renderBufferFormats;

        public FrameBuffer(RenderbufferInternalFormat[] renderBufferFormats = null, All filteringMode = All.Linear)
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

                if (isInitialised)
                {
                    Texture.Width = (int)Math.Ceiling(size.X);
                    Texture.Height = (int)Math.Ceiling(size.Y);
                    Texture.SetData(new TextureUpload());
                    Texture.Upload();

                    foreach (var buffer in attachedRenderBuffers)
                        buffer.Size = value;
                }
            }
        }

        private void initialise()
        {
            frameBuffer = GL.GenFramebuffer();
            Texture = new FrameBufferTexture(Vector2.ComponentMax(Vector2.One, Size), filteringMode);

            GLWrapper.BindFrameBuffer(frameBuffer);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, Texture.TextureId, 0);
            GLWrapper.BindTexture(null);

            if (renderBufferFormats != null)
            {
                foreach (var format in renderBufferFormats)
                    attachedRenderBuffers.Add(new RenderBuffer(format) { Size = Size });
            }
        }

        /// <summary>
        /// Binds the framebuffer.
        /// <para>Does not clear the buffer or reset the viewport/ortho.</para>
        /// </summary>
        public void Bind()
        {
            if (!isInitialised)
            {
                initialise();
                isInitialised = true;
            }
            else
            {
                // Buffer is bound during initialisation
                GLWrapper.BindFrameBuffer(frameBuffer);
            }
        }

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

            if (isInitialised)
            {
                Texture?.Dispose();
                Texture = null;

                GLWrapper.DeleteFrameBuffer(frameBuffer);

                foreach (var buffer in attachedRenderBuffers)
                    buffer.Dispose();
            }

            isDisposed = true;
        }

        #endregion

        private class FrameBufferTexture : TextureGLSingle
        {
            public FrameBufferTexture(Vector2 size, All filteringMode = All.Linear)
                : base((int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y), true, filteringMode)
            {
                SetData(new TextureUpload());
                Upload();
            }
        }
    }
}
