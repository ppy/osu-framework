// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Textures;
using osuTK;
using Veldrid;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridFrameBuffer : IFrameBuffer
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

        public VeldridFrameBuffer(VeldridRenderer renderer, PixelFormat[]? formats = null, SamplerFilter filteringMode = SamplerFilter.MinLinear_MagLinear_MipLinear)
        {
            // todo: we probably want the arguments separated to "PixelFormat[] colorFormats, PixelFormat depthFormat".
            if (formats?.Length > 1)
                throw new ArgumentException("Veldrid framebuffer cannot contain more than one depth target.");

            this.renderer = renderer;

            depthFormat = formats?[0];

            colourTarget = new FrameBufferTexture(renderer, filteringMode);
            Texture = renderer.CreateTexture(colourTarget);

            recreateResources();
        }

        [MemberNotNull(nameof(Framebuffer))]
        private void recreateResources()
        {
            // The texture is created once and resized internally, so it should not be deleted.
            DeleteResources(false);

            if (depthFormat is PixelFormat depth)
            {
                var depthDescription = TextureDescription.Texture2D((uint)colourTarget.Width, (uint)colourTarget.Height, 1, 1, depth, TextureUsage.DepthStencil);
                depthTarget = renderer.Factory.CreateTexture(ref depthDescription);
            }

            FramebufferDescription description = new FramebufferDescription
            {
                ColorTargets = new[] { new FramebufferAttachmentDescription(colourTarget.GetResourceList().Single().Texture, 0) },
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

        /// <summary>
        /// Deletes the resources of this frame buffer.
        /// </summary>
        /// <param name="deleteTexture">Whether the texture should also be deleted.</param>
        public void DeleteResources(bool deleteTexture)
        {
            if (deleteTexture)
                colourTarget.Dispose();

            if (Framebuffer.IsNotNull())
                Framebuffer.Dispose();

            depthTarget?.Dispose();
        }

        public void Bind() => renderer.BindFrameBuffer(this);
        public void Unbind() => renderer.UnbindFrameBuffer(this);

        ~VeldridFrameBuffer()
        {
            renderer.ScheduleDisposal(b => b.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            renderer.DeleteFrameBuffer(this);
            isDisposed = true;
        }

        private class FrameBufferTexture : VeldridTexture
        {
            protected override TextureUsage Usages => base.Usages | TextureUsage.RenderTarget;

            public FrameBufferTexture(VeldridRenderer renderer, SamplerFilter filteringMode = SamplerFilter.MinLinear_MagLinear_MipLinear)
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
