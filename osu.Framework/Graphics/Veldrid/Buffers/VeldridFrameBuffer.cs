// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Textures;
using osuTK;
using Veldrid;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridFrameBuffer : IVeldridFrameBuffer
    {
        public osu.Framework.Graphics.Textures.Texture Texture { get; }

        public Framebuffer Framebuffer { get; private set; }

        private readonly VeldridRenderer renderer;
        private readonly PixelFormat? depthFormat;
        private readonly VeldridTexture colourTarget;
        private Texture? depthTarget;

        private Vector2 size = Vector2.One;

        public Vector2 Size
        {
            get => size;
            set
            {
                if (value == size)
                    return;

                size = value;

                colourTarget.Width = (int)Math.Ceiling(size.X);
                colourTarget.Height = (int)Math.Ceiling(size.Y);
                colourTarget.SetData(new TextureUpload());
                colourTarget.Upload();

                recreateResources();
            }
        }

        public VeldridFrameBuffer(VeldridRenderer renderer, PixelFormat textureFormat = PixelFormat.R8G8B8A8UNorm, PixelFormat[]? formats = null,
                                  SamplerFilter filteringMode = SamplerFilter.MinLinearMagLinearMipLinear)
        {
            // todo: we probably want the arguments separated to "PixelFormat[] colorFormats, PixelFormat depthFormat".
            if (formats?.Length > 1)
                throw new ArgumentException("Veldrid framebuffer cannot contain more than one depth target.");

            this.renderer = renderer;

            depthFormat = formats?[0];

            colourTarget = new FrameBufferTexture(renderer, textureFormat, filteringMode);
            Texture = renderer.CreateTexture(colourTarget);

            recreateResources();
        }

        [MemberNotNull(nameof(Framebuffer))]
        private void recreateResources()
        {
            renderer.ScheduleDisposal(Framebuffer, depthTarget);

            if (depthFormat is PixelFormat depth)
            {
                var depthDescription = TextureDescription.Texture2D((uint)colourTarget.Width, (uint)colourTarget.Height, 1, 1, depth, TextureUsage.DepthStencil);
                depthTarget = renderer.Factory.CreateTexture(ref depthDescription);
            }

            FramebufferDescription description = new FramebufferDescription
            {
                ColorTargets = new[] { new FramebufferAttachmentDescription(colourTarget.GetVeldridTexture(0), 0, 0) },
                DepthTarget = depthTarget == null ? null : new FramebufferAttachmentDescription(depthTarget, 0)
            };

            Framebuffer = renderer.Factory.CreateFramebuffer(ref description);

            // Check if we need to rebind this framebuffer as a result of recreating it.
            if (renderer.IsFrameBufferBound(this))
            {
                Unbind();
                Bind();
            }
        }

        public void Bind() => renderer.BindFrameBuffer(this);
        public void Unbind() => renderer.UnbindFrameBuffer(this);

        #region Disposal

        private bool isDisposed;

        ~VeldridFrameBuffer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (disposing)
                colourTarget.Dispose();

            renderer.ScheduleDisposal(Framebuffer, depthTarget);
        }

        #endregion

        private class FrameBufferTexture : VeldridTexture
        {
            protected override TextureUsage Usages => base.Usages | TextureUsage.RenderTarget;

            public FrameBufferTexture(VeldridRenderer renderer, PixelFormat textureFormat, SamplerFilter filteringMode = SamplerFilter.MinLinearMagLinearMipLinear)
                : base(renderer, 1, 1, textureFormat, true, filteringMode)
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
