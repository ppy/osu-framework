// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLRenderBuffer : IDisposable
    {
        private readonly GLRenderer renderer;
        private readonly RenderbufferInternalFormat format;
        private readonly int renderBuffer;
        private readonly int sizePerPixel;

        private FramebufferAttachment attachment;

        public GLRenderBuffer(GLRenderer renderer, RenderbufferInternalFormat format)
        {
            this.renderer = renderer;
            this.format = format;

            renderBuffer = GL.GenRenderbuffer();

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);

            // OpenGL docs don't specify that this is required, but seems to be required on some platforms
            // to correctly attach in the GL.FramebufferRenderbuffer() call below
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, 1, 1);

            attachment = format.GetAttachmentType();
            sizePerPixel = format.GetBytesPerPixel();

            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, renderBuffer);
        }

        private Vector2 internalSize;
        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        public void Bind(Vector2 size)
        {
            size = Vector2.Clamp(size, Vector2.One, new Vector2(renderer.MaxRenderBufferSize));

            // See: https://www.khronos.org/registry/OpenGL/extensions/EXT/EXT_multisampled_render_to_texture.txt
            //    + https://developer.apple.com/library/archive/documentation/3DDrawing/Conceptual/OpenGLES_ProgrammingGuide/WorkingwithEAGLContexts/WorkingwithEAGLContexts.html
            // OpenGL ES allows the driver to discard renderbuffer contents after they are presented to the screen, so the storage must always be re-initialised for embedded devices.
            // Such discard does not exist on non-embedded platforms, so they are only re-initialised when required.
            if (renderer.IsEmbedded || internalSize.X < size.X || internalSize.Y < size.Y)
            {
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, (int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y));

                if (!renderer.IsEmbedded)
                {
                    memoryLease?.Dispose();
                    memoryLease = NativeMemoryTracker.AddMemory(this, (long)(size.X * size.Y * sizePerPixel));
                }

                internalSize = size;
            }
        }

        public void Unbind()
        {
            if (renderer.IsEmbedded)
            {
                // Renderbuffers are not automatically discarded on all embedded devices, so invalidation is forced for extra performance and to unify logic between devices.
                GL.InvalidateFramebuffer(FramebufferTarget.Framebuffer, 1, ref attachment);
            }
        }

        #region Disposal

        ~GLRenderBuffer()
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
