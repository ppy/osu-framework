// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Textures;
using osuTK;
using Veldrid;
using Texture = osu.Framework.Graphics.Textures.Texture;
using VdTexture = Veldrid.Texture;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredFrameBuffer : IVeldridFrameBuffer
    {
        public Texture Texture { get; }

        private readonly DeferredFrameBufferTexture nativeTexture;
        private readonly DeferredRenderer renderer;
        private readonly PixelFormat[]? formats;
        private readonly PixelFormat textureFormat;
        private readonly SamplerFilter filteringMode;

        private Vector2I size = Vector2I.One;

        public DeferredFrameBuffer(DeferredRenderer renderer, PixelFormat textureFormat, PixelFormat[]? formats, SamplerFilter filteringMode)
        {
            this.renderer = renderer;
            this.formats = formats;
            this.textureFormat = textureFormat;
            this.filteringMode = filteringMode;

            nativeTexture = new DeferredFrameBufferTexture(renderer, this);
            Texture = renderer.CreateTexture(nativeTexture);
        }

        public void Resize(Vector2I size)
            => nativeTexture.Resize(size);

        Framebuffer IVeldridFrameBuffer.Framebuffer
            => nativeTexture.Framebuffer;

        void IFrameBuffer.Bind()
            => renderer.BindFrameBuffer(this);

        void IFrameBuffer.Unbind()
            => renderer.UnbindFrameBuffer(this);

        Vector2 IFrameBuffer.Size
        {
            get => size;
            set
            {
                size = new Vector2I(
                    Math.Clamp((int)Math.Ceiling(value.X), 1, renderer.MaxTextureSize),
                    Math.Clamp((int)Math.Ceiling(value.Y), 1, renderer.MaxTextureSize));

                renderer.Context.EnqueueEvent(ResizeFrameBufferEvent.Create(renderer, this, size));
            }
        }

        #region Disposal

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            nativeTexture.Dispose();
        }

        #endregion

        private sealed class DeferredFrameBufferTexture : IVeldridTexture
        {
            public bool Available { get; private set; } = true;

            private readonly DeferredRenderer renderer;
            private readonly DeferredFrameBuffer deferredFrameBuffer;

            private VdTexture? texture;
            private Sampler? sampler;
            private ResourceSet? resourceSet;

            private VdTexture? depthTexture;
            private Framebuffer? framebuffer;

            private Vector2I resourceSize = Vector2I.One;

            public DeferredFrameBufferTexture(DeferredRenderer renderer, DeferredFrameBuffer deferredFrameBuffer)
            {
                this.renderer = renderer;
                this.deferredFrameBuffer = deferredFrameBuffer;
            }

            public void Resize(Vector2I size)
            {
                if (resourceSize == size)
                    return;

                renderer.ScheduleDisposal(texture, sampler, resourceSet, depthTexture, framebuffer);

                texture = null;
                sampler = null;
                resourceSet = null;
                depthTexture = null;
                framebuffer = null;

                resourceSize = size;
            }

            public Framebuffer Framebuffer
            {
                get
                {
                    EnsureCreated();
                    return framebuffer!;
                }
            }

            public int ResourceCount
                => 1;

            public VdTexture GetVeldridTexture(int index)
            {
                EnsureCreated();
                return texture.AsNonNull();
            }

            public ResourceSet GetResourceSet(int index, ResourceLayout layout)
            {
                EnsureCreated();
                return resourceSet ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, texture, sampler));
            }

            public void EnsureCreated()
            {
                if (framebuffer != null)
                    return;

                texture = deferredFrameBuffer.renderer.Factory.CreateTexture(
                    TextureDescription.Texture2D((uint)resourceSize.X,
                        (uint)resourceSize.Y,
                        1,
                        1,
                        deferredFrameBuffer.textureFormat,
                        TextureUsage.Sampled | TextureUsage.RenderTarget));

                sampler = deferredFrameBuffer.renderer.Factory.CreateSampler(
                    new SamplerDescription(
                        SamplerAddressMode.Clamp,
                        SamplerAddressMode.Clamp,
                        SamplerAddressMode.Clamp,
                        deferredFrameBuffer.filteringMode,
                        null,
                        0,
                        0,
                        uint.MaxValue,
                        0,
                        SamplerBorderColor.TransparentBlack));

                if (deferredFrameBuffer.formats?[0] is PixelFormat depth)
                {
                    depthTexture = deferredFrameBuffer.renderer.Factory.CreateTexture(
                        TextureDescription.Texture2D(
                            texture.Width,
                            texture.Height,
                            1,
                            1,
                            depth,
                            TextureUsage.DepthStencil));
                }

                framebuffer = deferredFrameBuffer.renderer.Factory.CreateFramebuffer(new FramebufferDescription
                {
                    ColorTargets = new[] { new FramebufferAttachmentDescription(texture, 0) },
                    DepthTarget = depthTexture == null ? null : new FramebufferAttachmentDescription(depthTexture, 0)
                });
            }

            IRenderer INativeTexture.Renderer
                => deferredFrameBuffer.renderer;

            string INativeTexture.Identifier
                => string.Empty;

            int INativeTexture.MaxSize
                => deferredFrameBuffer.renderer.MaxTextureSize;

            int? INativeTexture.MipLevel
            {
                get => null;
                set { }
            }

            int INativeTexture.Width
            {
                get => deferredFrameBuffer.size.X;
                set { }
            }

            int INativeTexture.Height
            {
                get => deferredFrameBuffer.size.Y;
                set { }
            }

            ulong INativeTexture.TotalBindCount { get; set; }

            bool INativeTexture.BypassTextureUploadQueueing
            {
                get => true;
                set { }
            }

            bool INativeTexture.UploadComplete
                => true;

            void INativeTexture.FlushUploads()
            {
            }

            void INativeTexture.SetData(ITextureUpload upload)
            {
            }

            bool INativeTexture.Upload()
                => false;

            int INativeTexture.GetByteSize()
                => deferredFrameBuffer.size.X * deferredFrameBuffer.size.Y * 4;

            #region Disposal

            private bool isDisposed;

            ~DeferredFrameBufferTexture()
            {
                dispose(false);
            }

            public void Dispose()
            {
                dispose(true);
                GC.SuppressFinalize(this);
            }

            private void dispose(bool isDisposing)
            {
                if (isDisposed)
                    return;

                isDisposed = true;

                renderer.ScheduleDisposal(texture, sampler, resourceSet, depthTexture, framebuffer);

                Available = false;
            }

            #endregion
        }
    }
}
