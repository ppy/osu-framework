// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Textures;
using osuTK;
using Veldrid;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredFrameBuffer : IVeldridFrameBuffer
    {
        public Texture Texture { get; }

        private readonly DeferredFrameBufferTexture nativeTexture;
        private readonly DeferredRenderer renderer;
        private readonly PixelFormat[]? formats;
        private readonly SamplerFilter filteringMode;

        private Vector2I size = Vector2I.One;

        public DeferredFrameBuffer(DeferredRenderer renderer, PixelFormat[]? formats, SamplerFilter filteringMode)
        {
            this.renderer = renderer;
            this.formats = formats;
            this.filteringMode = filteringMode;

            nativeTexture = new DeferredFrameBufferTexture(this);
            Texture = renderer.CreateTexture(nativeTexture);
        }

        public void Resize(Vector2I size)
            => nativeTexture.Resize(size);

        public void DeleteResources()
            => nativeTexture.Dispose();

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

        ~DeferredFrameBuffer()
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

        private sealed class DeferredFrameBufferTexture : IVeldridTexture
        {
            public bool Available { get; private set; } = true;

            private readonly DeferredFrameBuffer deferredFrameBuffer;
            private readonly VeldridTextureResources?[] resourcesArray = new VeldridTextureResources?[1];
            private Framebuffer? framebuffer;
            private Vector2I resourceSize = Vector2I.One;

            public DeferredFrameBufferTexture(DeferredFrameBuffer deferredFrameBuffer)
            {
                this.deferredFrameBuffer = deferredFrameBuffer;
            }

            public void Resize(Vector2I size)
            {
                if (resourceSize == size)
                    return;

                resources?.Dispose();
                resources = null;

                framebuffer?.Dispose();
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

            public IReadOnlyList<VeldridTextureResources> GetResourceList()
            {
                EnsureCreated();
                return resourcesArray!;
            }

            public void EnsureCreated()
            {
                if (framebuffer != null)
                    return;

                resources = new VeldridTextureResources(
                    deferredFrameBuffer.renderer.Factory.CreateTexture(
                        TextureDescription.Texture2D((uint)resourceSize.X,
                            (uint)resourceSize.Y,
                            1,
                            1,
                            PixelFormat.R8G8B8A8UNorm,
                            TextureUsage.Sampled | TextureUsage.RenderTarget)),
                    deferredFrameBuffer.renderer.Factory.CreateSampler(
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
                            SamplerBorderColor.TransparentBlack)));

                global::Veldrid.Texture? depthTexture = null;

                if (deferredFrameBuffer.formats?[0] is PixelFormat depth)
                {
                    depthTexture = deferredFrameBuffer.renderer.Factory.CreateTexture(
                        TextureDescription.Texture2D(
                            resources.Texture.Width,
                            resources.Texture.Height,
                            1,
                            1,
                            depth,
                            TextureUsage.DepthStencil));
                }

                framebuffer = deferredFrameBuffer.renderer.Factory.CreateFramebuffer(new FramebufferDescription
                {
                    ColorTargets = new[] { new FramebufferAttachmentDescription(resources.Texture, 0) },
                    DepthTarget = depthTexture == null ? null : new FramebufferAttachmentDescription(depthTexture, 0)
                });
            }

            private VeldridTextureResources? resources
            {
                get => resourcesArray[0];
                set => resourcesArray[0] = value;
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

            private bool isDisposed;

            public void Dispose()
            {
                if (isDisposed)
                    return;

                resources?.Dispose();
                framebuffer?.Dispose();
                Available = false;
                isDisposed = true;
            }
        }
    }
}
